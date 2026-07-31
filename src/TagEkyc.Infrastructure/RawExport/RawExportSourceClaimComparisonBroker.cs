using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.ProtectedValues;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RawExportSourceClaimComparisonBroker(
    TagEkycDbContext db,
    IContentCommitmentService contentCommitments,
    ISubjectRefTokenService subjectTokens,
    ICustodyProfileProvider custodyProfiles) :
    IRawExportSourceClaimComparisonBroker
{
    public async Task<RawExportSourceClaimComparisonResult> CompleteNewCandidateAsync(
        RawExportSourceClaimComparisonCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateCommand(command);

        var preflight = await ReadPreflightAsync(command, cancellationToken);
        if (preflight is null)
        {
            return Failed(RawExportSourceClaimComparisonOutcome.ClaimTokenInvalid);
        }

        var suppliedEnvelope = C1HashCanonical.ComputeProducerClaimEnvelopeFingerprint(
            preflight.IngressIdentityFingerprint,
            command.ClaimedPlaintextLength,
            command.MediaType,
            command.CapturedAtUtc,
            command.PlaintextRetentionStartedAtUtc,
            command.PlaintextRetentionExpiresAtUtc,
            command.PlaintextRetentionBudgetSeconds);
        if (!suppliedEnvelope.AsSpan().SequenceEqual(preflight.ProducerEnvelopeFingerprint))
        {
            return Failed(RawExportSourceClaimComparisonOutcome.ClaimTokenInvalid);
        }

        if (!preflight.AuthorityEffective || !preflight.ConsentEffective)
        {
            return Failed(RawExportSourceClaimComparisonOutcome.SourceRetentionNotAuthorized);
        }

        var commitmentPayload = C1HashCanonical.EncodeLengthPrefixedPayload(
            "TAG-EKYC:RAW-EXPORT:CONTENT-COMMITMENT:C1:V1",
            preflight.StableDataScopeId,
            preflight.ControllerIdentity,
            preflight.VerificationSessionId.ToString("N"),
            preflight.CaptureArtifactId.ToString("N"),
            preflight.CaptureRevision.ToString(CultureInfo.InvariantCulture),
            preflight.RawClass,
            Convert.ToHexString(command.ClaimedPlaintextDigest.Span).ToLowerInvariant(),
            command.ClaimedPlaintextLength.ToString(CultureInfo.InvariantCulture),
            command.MediaType);
        var commitment = await contentCommitments.ComputeAsync(
            new CommitmentKeySelector(
                preflight.CommitmentKeySelectorId,
                preflight.CommitmentKeySelectorVersion),
            commitmentPayload,
            cancellationToken);
        if (!commitment.IsSuccess)
        {
            throw new InvalidOperationException("RAW_EXPORT_CONTENT_COMMITMENT_PROVIDER_FAILURE");
        }

        var subjectPayload = C1HashCanonical.EncodeLengthPrefixedPayload(
            "TAG-EKYC:RAW-EXPORT:SUBJECT-TOKEN:C1:V1",
            preflight.StableDataScopeId,
            preflight.ControllerIdentity,
            preflight.SubjectRef.Normalize());
        var subjectToken = await subjectTokens.ComputeAsync(
            new SubjectTokenKeySelector(
                FixtureSubjectTokenCatalog.FixtureKeyId,
                FixtureSubjectTokenCatalog.FixtureKeyVersion),
            subjectPayload,
            cancellationToken);
        if (!subjectToken.IsSuccess)
        {
            throw new InvalidOperationException("RAW_EXPORT_SUBJECT_TOKEN_PROVIDER_FAILURE");
        }

        var profile = custodyProfiles.ActiveSourceEncryptionProfile;
        var kek = custodyProfiles.ActiveKekReference;
        var bounds = custodyProfiles.TimeBounds;
        var nonceCommitment = C1HashCanonical.ComputeNonceSeedCommitment(
            profile.NonceStrategyId);
        var framingDigest = C1HashCanonical.ComputeFramingParametersDigest(
            profile.EncryptionSuiteId,
            profile.EncryptionFramingVersion,
            profile.ChunkSize,
            profile.NonceStrategyId);

        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);
            await SetActorAsync(
                connection,
                transaction,
                command.ActorPrincipalId,
                cancellationToken);
            await using var sql = new NpgsqlCommand(
                """
                SELECT "OutcomeCode", "SourceArtifactId"
                FROM tagekyc.complete_raw_export_source_ingress_claim(
                    @client,@producer,@agent,@ingress,@evaluation,@revision,@fence,
                    @variant,@expires,@token,@envelope,
                    @commitmentSchema,@commitmentKey,@commitmentVersion,@commitment,
                    @subjectSchema,@subjectKey,@subjectVersion,@subjectToken,
                    @length,@media,@captured,@retentionStart,@retentionExpires,@retentionBudget,
                    @storageProfile,@sourceProfile,@sourceProfileVersion,
                    @suite,@framing,@nonce,@nonceCommitment,@chunk,@framingDigest,
                    @keyProvider,@kekId,@kekVersion,@kekFingerprint,
                    @maxContinuationSeconds,@attemptDeadlineSeconds,@safetyMarginMilliseconds,
                    @ownershipLeaseSeconds);
                """,
                connection,
                transaction);
            Add(sql, "client", command.ClientApplicationId);
            Add(sql, "producer", command.ProducerId);
            Add(sql, "agent", command.CaptureAgentInstanceId);
            Add(sql, "ingress", command.IngressIdempotencyKey.ToString("N"));
            Add(sql, "evaluation", command.Token.EvaluationId);
            Add(sql, "revision", command.Token.Revision);
            Add(sql, "fence", command.Token.Fence);
            Add(sql, "variant", command.Token.Variant);
            Add(sql, "expires", command.Token.ExpiresAtUtc);
            Add(sql, "token", command.Token.Value);
            Add(sql, "envelope", suppliedEnvelope);
            Add(sql, "commitmentSchema", 1);
            Add(sql, "commitmentKey", preflight.CommitmentKeySelectorId);
            Add(sql, "commitmentVersion", preflight.CommitmentKeySelectorVersion);
            Add(sql, "commitment", commitment.Mac.ToArray());
            Add(sql, "subjectSchema", 1);
            Add(sql, "subjectKey", FixtureSubjectTokenCatalog.FixtureKeyId);
            Add(sql, "subjectVersion", FixtureSubjectTokenCatalog.FixtureKeyVersion);
            Add(sql, "subjectToken", subjectToken.Token.ToArray());
            Add(sql, "length", command.ClaimedPlaintextLength);
            Add(sql, "media", command.MediaType);
            Add(sql, "captured", command.CapturedAtUtc);
            Add(sql, "retentionStart", command.PlaintextRetentionStartedAtUtc);
            Add(sql, "retentionExpires", command.PlaintextRetentionExpiresAtUtc);
            Add(sql, "retentionBudget", command.PlaintextRetentionBudgetSeconds);
            Add(sql, "storageProfile", profile.StorageProfileId);
            Add(sql, "sourceProfile", profile.SourceEncryptionProfileId);
            Add(sql, "sourceProfileVersion", profile.SourceEncryptionProfileVersion);
            Add(sql, "suite", profile.EncryptionSuiteId);
            Add(sql, "framing", profile.EncryptionFramingVersion);
            Add(sql, "nonce", profile.NonceStrategyId);
            Add(sql, "nonceCommitment", nonceCommitment);
            Add(sql, "chunk", profile.ChunkSize);
            Add(sql, "framingDigest", framingDigest);
            Add(sql, "keyProvider", kek.KeyProviderId);
            Add(sql, "kekId", kek.KekId);
            Add(sql, "kekVersion", kek.KekVersion);
            Add(sql, "kekFingerprint", kek.KekFingerprint);
            Add(sql, "maxContinuationSeconds", CheckedSeconds(bounds.MaxRemainingContinuationWindow));
            Add(sql, "attemptDeadlineSeconds", CheckedSeconds(bounds.EncryptionAttemptDeadline));
            Add(sql, "safetyMarginMilliseconds", checked((int)bounds.SafetyMargin.TotalMilliseconds));
            Add(sql, "ownershipLeaseSeconds", CheckedSeconds(bounds.OwnershipLeaseDuration));

            await using var reader = await sql.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("RAW_EXPORT_SOURCE_COMPLETE_EMPTY");
            }

            var outcome = reader.GetString(0);
            var sourceId = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1);
            await reader.CloseAsync();
            await transaction.CommitAsync(cancellationToken);
            return outcome switch
            {
                "NewReservation" => new(
                    RawExportSourceClaimComparisonOutcome.NewReservation,
                    sourceId),
                "SOURCE_RETENTION_NOT_AUTHORIZED" => Failed(
                    RawExportSourceClaimComparisonOutcome.SourceRetentionNotAuthorized),
                "CLAIM_TOKEN_INVALID" => Failed(
                    RawExportSourceClaimComparisonOutcome.ClaimTokenInvalid),
                "RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID" => Failed(
                    RawExportSourceClaimComparisonOutcome.PlaintextRetentionInvalid),
                _ => throw new InvalidOperationException(
                    $"Unratified source-complete outcome: {outcome}"),
            };
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private async Task<Preflight?> ReadPreflightAsync(
        RawExportSourceClaimComparisonCommand command,
        CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);
            await SetActorAsync(
                connection,
                transaction,
                command.ActorPrincipalId,
                cancellationToken);
            await using var sql = new NpgsqlCommand(
                """
                WITH token AS (
                    SELECT tagekyc.validate_raw_export_claim_evaluation_token(
                        @client,@producer,@agent,@ingress,@evaluation,@revision,@fence,
                        @variant,@expires,@token) AS valid
                )
                SELECT claim."IngressIdentityFingerprint",
                       alias."ProducerClaimEnvelopeFingerprint",
                       claim."VerificationSessionId",claim."CaptureArtifactId",
                       claim."CaptureRevision",claim."RawClass",
                       claim."CommitmentKeySelectorId",claim."CommitmentKeySelectorVersion",
                       authority."ControllerIdentity",authority."StableDataScopeId",
                       authority."ConsentPolicyId",authority."ConsentPolicyVersion",
                       consent."SubjectRef",authority."AuthoritySnapshotId",
                       authority."AuthoritySnapshotSchemaVersion",
                       authority."AbsoluteSourceExpiresAtUtc",
                       (authority."AuthoritySnapshotId" IS NOT NULL) AS authority_effective,
                       (consent."State" = 'Effective'
                         AND consent."PurposeCode" = 'SubjectRawBiometricExport'
                         AND consent."RawClass" = claim."RawClass") AS consent_effective
                FROM token
                JOIN tagekyc.raw_export_source_ingress_claim_aliases alias
                  ON token.valid
                 AND alias."ClientApplicationId" = @client
                 AND alias."ProducerId" = pg_catalog.normalize(@producer, 'NFC')
                 AND alias."CaptureAgentInstanceId" = pg_catalog.normalize(@agent, 'NFC')
                 AND alias."IngressIdempotencyKey" = @ingress::uuid
                JOIN tagekyc.raw_export_source_ingress_claims claim
                  ON claim."IngressClaimId" = alias."IngressClaimId"
                LEFT JOIN LATERAL tagekyc.raw_export_resolve_current_authority_for_source(
                    @client,claim."VerificationSessionId",claim."CaptureAcceptanceId",
                    claim."RawClass",pg_catalog.statement_timestamp()) authority ON true
                LEFT JOIN LATERAL tagekyc.raw_export_resolve_subject_consent_for_authorization(
                    claim."VerificationSessionId",authority."ConsentPolicyId",
                    authority."ConsentPolicyVersion") consent
                  ON consent."RawClass" = claim."RawClass";
                """,
                connection,
                transaction);
            AddTokenParameters(sql, command);
            await using var reader = await sql.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                await reader.CloseAsync();
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var result = new Preflight(
                (byte[])reader[0],
                (byte[])reader[1],
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.GetInt32(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetInt32(7),
                reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                reader.IsDBNull(10) ? Guid.Empty : reader.GetGuid(10),
                reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                reader.IsDBNull(13) ? Guid.Empty : reader.GetGuid(13),
                reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
                reader.IsDBNull(15) ? default : reader.GetFieldValue<DateTimeOffset>(15),
                reader.GetBoolean(16),
                reader.GetBoolean(17));
            await reader.CloseAsync();
            await transaction.RollbackAsync(cancellationToken);
            return result;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static void ValidateCommand(RawExportSourceClaimComparisonCommand command)
    {
        if (command.ActorPrincipalId == Guid.Empty
            || command.ClientApplicationId == Guid.Empty
            || command.ClaimedPlaintextDigest.Length == 0
            || command.ClaimedPlaintextLength < 0
            || command.PlaintextRetentionBudgetSeconds < 1)
        {
            throw new ArgumentException("Raw-export source comparison command is invalid.");
        }
    }

    private static int CheckedSeconds(TimeSpan value) =>
        checked((int)value.TotalSeconds);

    private static RawExportSourceClaimComparisonResult Failed(
        RawExportSourceClaimComparisonOutcome outcome) => new(outcome, null);

    private static void Add(NpgsqlCommand command, string name, object value) =>
        command.Parameters.AddWithValue(name, value);

    private static void AddTokenParameters(
        NpgsqlCommand sql,
        RawExportSourceClaimComparisonCommand command)
    {
        Add(sql, "client", command.ClientApplicationId);
        Add(sql, "producer", command.ProducerId);
        Add(sql, "agent", command.CaptureAgentInstanceId);
        Add(sql, "ingress", command.IngressIdempotencyKey.ToString("N"));
        Add(sql, "evaluation", command.Token.EvaluationId);
        Add(sql, "revision", command.Token.Revision);
        Add(sql, "fence", command.Token.Fence);
        Add(sql, "variant", command.Token.Variant);
        Add(sql, "expires", command.Token.ExpiresAtUtc);
        Add(sql, "token", command.Token.Value);
    }

    private static async Task SetActorAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid actor,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT pg_catalog.set_config('tagekyc.actor_principal_id', @actor, true);",
            connection,
            transaction);
        command.Parameters.AddWithValue("actor", actor.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed record Preflight(
        byte[] IngressIdentityFingerprint,
        byte[] ProducerEnvelopeFingerprint,
        Guid VerificationSessionId,
        Guid CaptureArtifactId,
        int CaptureRevision,
        string RawClass,
        string CommitmentKeySelectorId,
        int CommitmentKeySelectorVersion,
        string ControllerIdentity,
        string StableDataScopeId,
        Guid ConsentPolicyId,
        int ConsentPolicyVersion,
        string SubjectRef,
        Guid AuthoritySnapshotId,
        int AuthoritySnapshotSchemaVersion,
        DateTimeOffset AbsoluteSourceExpiresAtUtc,
        bool AuthorityEffective,
        bool ConsentEffective);
}
