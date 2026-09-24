using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

// Explicit qualified inputs, not deployment defaults. Host readiness owns the
// complete network/provider configuration; this type owns only the B inputs.
internal sealed record RawIngressBrokerTransactionSettings(
    Guid EvaluationOwnerId, int EvaluationTokenTtlSeconds,
    int IdempotencyLockTimeoutMilliseconds, CommitmentKeySelector CommitmentSelector,
    SubjectTokenKeySelector SubjectSelector, int RequestTimeoutMilliseconds);

public sealed class RawIngressBrokerTransactionFacade : IRawIngressMetadataBroker
{
    private readonly NpgsqlDataSource dataSource;
    private readonly RetainedSourceClaimPreflight preflight;
    private readonly ICustodyProfileProvider profiles;
    private readonly RawIngressBrokerTransactionSettings settings;

    internal RawIngressBrokerTransactionFacade(NpgsqlDataSource dataSource,
        RetainedSourceClaimPreflight preflight, ICustodyProfileProvider profiles,
        RawIngressBrokerTransactionSettings settings)
    {
        this.dataSource = dataSource;
        this.preflight = preflight;
        this.profiles = profiles;
        this.settings = settings;
        if (settings.EvaluationOwnerId == Guid.Empty
            || settings.EvaluationTokenTtlSeconds is < 1 or > 3600
            || settings.IdempotencyLockTimeoutMilliseconds is < 1 or > 30000
            || settings.RequestTimeoutMilliseconds is < 1 or > 30000
            || settings.IdempotencyLockTimeoutMilliseconds >= settings.RequestTimeoutMilliseconds
            || settings.RequestTimeoutMilliseconds >= settings.EvaluationTokenTtlSeconds * 1000L
            || settings.RequestTimeoutMilliseconds >= profiles.TimeBounds.SafetyMargin.TotalMilliseconds)
            throw Invalid();
    }

    public async Task<RawIngressBrokerResult> AdmitAsync(
        CaptureRuntimeRawIngressAdmissionContext input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        var budget = checked((int)input.PlaintextRetentionBudgetSeconds);
        if (budget < 1 || input.ClaimedPlaintextLength < 1
            || input.ClaimedPlaintextDigest.Length != 64
            || input.ClaimedPlaintextDigest.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            throw Invalid();
        var digest = Convert.FromHexString(input.ClaimedPlaintextDigest);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(settings.RequestTimeoutMilliseconds);
        var ct = deadline.Token;
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        // No helper opens another connection/transaction. In particular do not
        // invoke the legacy independently-transacting comparison broker here.
        if (!await CallerOwnsSession(connection, transaction, input, ct))
        {
            await transaction.CommitAsync(ct);
            return Final("NOT_FOUND_OR_NOT_ALLOWED");
        }
        var bound = await ReadBound(connection, transaction, input, ct);
        RawIngressBrokerResult result;
        if (bound is null)
            result = Final("RAW_EXPORT_SOURCE_BINDING_INVALID");
        else
        {
            await using (var actor = new NpgsqlCommand(
                "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',$1,true)", connection, transaction))
            {
                actor.Parameters.AddWithValue(bound.Principal.ToString("D"));
                await actor.ExecuteNonQueryAsync(ct);
            }
            try
            {
                result = await BeginAndComplete(connection, transaction, input, bound, budget, digest, ct);
            }
            catch (BeginRejectedException rejected)
            {
                // These exact B-B exceptions abort B. Preserve the previous
                // durable residue by rolling it back, never committing an
                // aborted transaction or treating an arbitrary SQL error as business.
                await transaction.RollbackAsync(ct);
                return Final(rejected.Code);
            }
            catch (RawIngressProviderCapabilityUnavailableException)
            {
                // B may already have an Evaluating shell. Roll this attempt
                // back before projecting O05 so its token and original budget
                // remain untouched; a later same-metadata retry may re-enter B.
                await transaction.RollbackAsync(ct);
                return Final(RawExportSourceIngressCodes.CapabilityUnavailable);
            }
        }
        // Typed denials commit their authorized residue. Exceptions dispose B;
        // an uncertain commit is propagated, never reported as a proven rollback.
        await transaction.CommitAsync(ct);
        return result;
    }

    private static async Task<bool> CallerOwnsSession(NpgsqlConnection connection,
        NpgsqlTransaction transaction, CaptureRuntimeRawIngressAdmissionContext c, CancellationToken ct)
    {
        await using var sql = new NpgsqlCommand("""
            SELECT tagekyc.capture_runtime_raw_ingress_caller_owns_session(
              $1,$2,$3,$4,$5,$6,$7,$8,$9,pg_catalog.clock_timestamp())
            """, connection, transaction);
        Add(sql, c.CaptureAgentId, c.DeviceInstallationId, c.CredentialId, c.CredentialGeneration,
            c.RolePolicyId, c.RolePolicyRevision, c.VerificationSessionId, c.AgentConfigurationRevision,
            c.RawClass);
        var result = await sql.ExecuteScalarAsync(ct);
        return result is bool authorized && authorized;
    }

    private static async Task<Bound?> ReadBound(NpgsqlConnection connection, NpgsqlTransaction transaction,
        CaptureRuntimeRawIngressAdmissionContext c, CancellationToken ct)
    {
        await using var sql = new NpgsqlCommand("""
            SELECT * FROM tagekyc.capture_runtime_read_bound_raw_ingress(
              $1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,pg_catalog.clock_timestamp())
            """, connection, transaction);
        Add(sql, c.CaptureAgentId, c.DeviceInstallationId, c.CredentialId, c.CredentialGeneration,
            c.RolePolicyId, c.RolePolicyRevision, c.VerificationSessionId, c.CaptureArtifactId,
            c.CaptureRevision, c.RawClass, c.AgentConfigurationRevision);
        await using var row = await sql.ExecuteReaderAsync(ct);
        if (row.FieldCount != 15) throw Invalid();
        if (!await row.ReadAsync(ct)) return null;
        for (var n = 0; n < 15; n++) if (row.IsDBNull(n)) throw Invalid();
        foreach (var n in new[] { 0, 1, 3, 4, 5, 6, 9 }) if (row.GetGuid(n) == Guid.Empty) throw Invalid();
        foreach (var n in new[] { 2, 7, 10 }) if (row.GetInt64(n) < 1) throw Invalid();
        foreach (var n in new[] { 8, 12, 13, 14 }) if (string.IsNullOrEmpty(row.GetString(n))) throw Invalid();
        if (row.GetGuid(5) != c.VerificationSessionId || row.GetInt64(7) != c.CaptureRevision) throw Invalid();
        var result = new Bound(row.GetGuid(0), row.GetGuid(3), row.GetGuid(4), row.GetGuid(6),
            row.GetString(8), row.GetGuid(9), row.GetInt64(10), row.GetFieldValue<DateTimeOffset>(11),
            row.GetString(12), row.GetString(13), row.GetString(14));
        if (await row.ReadAsync(ct)) throw Invalid();
        return result;
    }

    private async Task<RawIngressBrokerResult> BeginAndComplete(NpgsqlConnection connection,
        NpgsqlTransaction transaction, CaptureRuntimeRawIngressAdmissionContext c, Bound b,
        int budget, byte[] digest, CancellationToken ct)
    {
        Token token;
        await using (var sql = new NpgsqlCommand("""
            SELECT * FROM tagekyc.raw_export_begin_retained_source_ingress_with_authority(
              $1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14,$15,$16,$17,$18,$19,$20,$21,$22,$23,$24)
            """, connection, transaction))
        {
            Add(sql, b.Principal, b.Client, c.CaptureAgentId.ToString("N"), c.DeviceInstallationId.ToString("N"),
                c.IngressIdempotencyKey.ToString("N"), c.VerificationSessionId, b.Acceptance, c.CaptureArtifactId,
                c.CaptureRevision, c.RawClass, b.Challenge, c.ClaimedPlaintextLength, c.MediaType,
                c.CapturedAtUtc, c.PlaintextRetentionStartedAtUtc, c.PlaintextRetentionExpiresAtUtc, budget,
                settings.CommitmentSelector.KeyId, settings.CommitmentSelector.KeyVersion,
                settings.EvaluationOwnerId, settings.EvaluationTokenTtlSeconds,
                settings.IdempotencyLockTimeoutMilliseconds, b.Permit, b.PermitRevision);
            await using var row = await ExecuteBeginReader(sql, ct);
            if (row.FieldCount != 13 || !await row.ReadAsync(ct)) throw Invalid();
            if (!row.IsDBNull(0))
            {
                var code = row.GetString(0);
                foreach (var n in new[] { 1, 2, 3, 4, 5, 6, 8, 9, 10, 11, 12 })
                    if (!row.IsDBNull(n)) throw Invalid();
                var retry = row.IsDBNull(7) ? (DateTimeOffset?)null : row.GetFieldValue<DateTimeOffset>(7);
                if (code == "RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS" ? retry is null : retry is not null)
                    throw Invalid();
                if (code is not ("SOURCE_RETENTION_NOT_AUTHORIZED" or "RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS"))
                    throw Invalid();
                if (await row.ReadAsync(ct)) throw Invalid();
                return new RawIngressBrokerResult.Final(new(code, RetryNotBeforeUtc: retry));
            }
            for (var n = 1; n < 13; n++) if (n != 7 && row.IsDBNull(n)) throw Invalid();
            if (!row.IsDBNull(7) || row.GetGuid(4) == Guid.Empty || row.GetInt64(5) < 1 || row.GetInt64(6) < 1
                || row.GetInt32(12) < 1 || string.IsNullOrEmpty(row.GetString(11))) throw Invalid();
            foreach (var n in new[] { 8, 9, 10 }) if (((byte[])row[n]).Length != 32) throw Invalid();
            token = new(row.GetString(1), row.GetString(2), row.GetFieldValue<DateTimeOffset>(3),
                row.GetGuid(4), row.GetInt64(5), row.GetInt64(6), (byte[])row[8], (byte[])row[9],
                row.GetString(11), row.GetInt32(12));
            if (await row.ReadAsync(ct)) throw Invalid();
        }
        var computed = await preflight.ComputeAsync(new(token.Identity, token.Envelope,
            c.VerificationSessionId, c.CaptureArtifactId, c.CaptureRevision, c.RawClass,
            b.Scope, b.Controller, b.Subject, token.Selector, token.SelectorVersion, token.Variant,
            digest, c.ClaimedPlaintextLength, c.MediaType, c.CapturedAtUtc,
            c.PlaintextRetentionStartedAtUtc, c.PlaintextRetentionExpiresAtUtc, budget), settings.SubjectSelector, ct)
            ?? throw Invalid();
        var p = profiles.ActiveSourceEncryptionProfile;
        var k = profiles.ActiveKekReference;
        var bounds = profiles.TimeBounds;
        var profileDigest = RetainedSourceClaimPreflight.ComputeProfileDigests(p);
        await using var complete = new NpgsqlCommand("""
            SELECT * FROM tagekyc.complete_raw_export_source_ingress_claim_with_r2_handoff(
              $1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14,$15,$16,$17,$18,$19,$20,$21,
              $22,$23,$24,$25,$26,$27,$28,$29,$30,$31,$32,$33,$34,$35,$36,$37,$38,$39,$40,$41,$42)
            """, connection, transaction);
        Add(complete, b.Client, c.CaptureAgentId.ToString("N"), c.DeviceInstallationId.ToString("N"),
            c.IngressIdempotencyKey.ToString("N"), token.Id, token.Revision, token.Fence, token.Variant,
            token.Expires, token.Value, computed.ProducerEnvelopeFingerprint, 1, token.Selector, token.SelectorVersion);
        complete.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bytea,
            Value = computed.ContentCommitment is null ? DBNull.Value : computed.ContentCommitment });
        Add(complete, 1, settings.SubjectSelector.KeyId, settings.SubjectSelector.KeyVersion, computed.SubjectToken,
            c.ClaimedPlaintextLength, c.MediaType, c.CapturedAtUtc, c.PlaintextRetentionStartedAtUtc,
            c.PlaintextRetentionExpiresAtUtc, budget, p.StorageProfileId, p.SourceEncryptionProfileId,
            p.SourceEncryptionProfileVersion, p.EncryptionSuiteId, p.EncryptionFramingVersion, p.NonceStrategyId,
            profileDigest.NonceSeedCommitment, p.ChunkSize, profileDigest.FramingParametersDigest,
            k.KeyProviderId, k.KekId, k.KekVersion, k.KekFingerprint,
            checked((int)bounds.MaxRemainingContinuationWindow.TotalSeconds),
            checked((int)bounds.EncryptionAttemptDeadline.TotalSeconds),
            checked((int)bounds.SafetyMargin.TotalMilliseconds), checked((int)bounds.OwnershipLeaseDuration.TotalSeconds));
        await using var completed = await complete.ExecuteReaderAsync(ct);
        if (completed.FieldCount != 6 || !await completed.ReadAsync(ct) || completed.IsDBNull(0)) throw Invalid();
        var result = ProjectComplete(completed, b);
        if (await completed.ReadAsync(ct)) throw Invalid();
        return result;
    }

    private static RawIngressBrokerResult ProjectComplete(NpgsqlDataReader row, Bound b)
    {
        var code = row.GetString(0);
        if (code is "NewReservation" or "SameOwnerReentry")
        {
            for (var n = 1; n < 6; n++) if (row.IsDBNull(n)) throw Invalid();
            for (var n = 1; n < 4; n++) if (row.GetGuid(n) == Guid.Empty) throw Invalid();
            if (row.GetInt64(4) < 1 || row.GetInt64(5) < 1) throw Invalid();
            return new RawIngressBrokerResult.Handoff(new(row.GetGuid(1), row.GetGuid(2), row.GetGuid(3),
                row.GetInt64(4), row.GetInt64(5), b.Principal, b.Client, b.Binding,
                b.Permit, b.PermitRevision, b.Expires));
        }
        for (var n = 2; n < 6; n++) if (!row.IsDBNull(n)) throw Invalid();
        if (code == "RAW_EXPORT_SOURCE_ALREADY_AVAILABLE")
        {
            if (row.IsDBNull(1) || row.GetGuid(1) == Guid.Empty) throw Invalid();
            return new RawIngressBrokerResult.Final(new(code, row.GetGuid(1), "Available", "Available"),
                RawIngressBrokerFinalOrigin.PublishedReplay);
        }
        // Uppercase terminal codes arrive only from the committed-attempt branch
        // of B-C. They are not allowed in the preflight/begin phase.
        code = code switch
        {
            "CLAIM_TOKEN_INVALID" => "RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID",
            "RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT"
                or "RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE"
                or "SOURCE_RETENTION_NOT_AUTHORIZED"
                or "RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID" => code,
            "RAW_EXPORT_SOURCE_RESERVATION_BUSY" or "RAW_EXPORT_SOURCE_RESUME_PENDING"
                or "RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED" or "CONTENT_COMMITMENT_MISMATCH"
                or "RECAPTURE_REQUIRED" => code,
            _ => throw Invalid()
        };
        if (code is "RAW_EXPORT_SOURCE_RESUME_PENDING" or "RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED"
            or "CONTENT_COMMITMENT_MISMATCH" or "RECAPTURE_REQUIRED")
        {
            if (row.IsDBNull(1) || row.GetGuid(1) == Guid.Empty) throw Invalid();
            return new RawIngressBrokerResult.Final(new(code), code == "RAW_EXPORT_SOURCE_RESUME_PENDING"
                ? RawIngressBrokerFinalOrigin.PreservedCiphertext : RawIngressBrokerFinalOrigin.PersistedTerminal);
        }
        return Final(code);
    }

    private static RawIngressBrokerResult Final(string code) => new RawIngressBrokerResult.Final(new(code));
    private static async Task<NpgsqlDataReader> ExecuteBeginReader(NpgsqlCommand sql, CancellationToken ct)
    {
        try { return await sql.ExecuteReaderAsync(ct); }
        catch (PostgresException error) when (
            error.SqlState == "55P03" && error.MessageText == "RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY"
            || error.SqlState == "P0001" && error.MessageText is
                "RAW_EXPORT_SOURCE_BINDING_INVALID" or "RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT"
                or "RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID" or "RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED")
        {
            throw new BeginRejectedException(error.MessageText);
        }
    }
    private sealed class BeginRejectedException(string code) : Exception(code)
    {
        internal string Code { get; } = code;
    }
    private static InvalidOperationException Invalid() => new("RAW_INGRESS_BROKER_STATE_INVALID");
    private static void Add(NpgsqlCommand command, params object[] values)
    {
        foreach (var value in values) command.Parameters.Add(new NpgsqlParameter { Value = value });
    }
    private sealed record Bound(Guid Binding, Guid Principal, Guid Client, Guid Acceptance,
        string Challenge, Guid Permit, long PermitRevision, DateTimeOffset Expires,
        string Scope, string Controller, string Subject);
    private sealed record Token(string Value, string Variant, DateTimeOffset Expires,
        Guid Id, long Revision, long Fence, byte[] Identity, byte[] Envelope,
        string Selector, int SelectorVersion);
}
