using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RawExportR2Repository(TagEkycDbContext db)
{
    private readonly ProvisionalObjectCustodyRepository objects = new(db);

    internal async Task SetActorAsync(Guid actorPrincipalId, CancellationToken cancellationToken)
    {
        if (actorPrincipalId == Guid.Empty)
            throw new ArgumentException("RAW_EXPORT_R2_ACTOR_INVALID", nameof(actorPrincipalId));
        await using var command = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
        command.CommandText = "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,false)";
        command.Parameters.AddWithValue("actor", actorPrincipalId.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<RawExportR2EncryptionContext?> ReadEncryptionContextAsync(
        Guid attemptId,
        long revision,
        long fence,
        CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
        command.CommandText = "SELECT * FROM tagekyc.raw_export_read_source_encryption_context(@attempt,@revision,@fence)";
        command.Parameters.AddWithValue("attempt", attemptId);
        command.Parameters.AddWithValue("revision", revision);
        command.Parameters.AddWithValue("fence", fence);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        return new(
            G(reader, "AttemptId"), G(reader, "SourceArtifactId"), L(reader, "EncryptionAttemptRevision"), L(reader, "Fence"),
            G(reader, "AttemptKeyReservationId"), G(reader, "ProvisionalObjectIdentity"), B(reader, "EncryptionAttemptFingerprint"),
            S(reader, "KeyProviderId"), S(reader, "KekId"), I(reader, "KekVersion"), S(reader, "KekFingerprint"),
            S(reader, "EncryptionSuiteId"), I(reader, "EncryptionFramingVersion"), I(reader, "ChunkSize"), S(reader, "NonceStrategyId"),
            S(reader, "NonceDerivationSeedReferenceOrWrappedSeed"), B(reader, "NonceDerivationSeedCommitment"),
            B(reader, "FramingParametersDigest"), B(reader, "WrappedDekMetadataDigest"), G(reader, "VerificationSessionId"),
            G(reader, "CaptureArtifactId"), I(reader, "CaptureRevision"), S(reader, "RawClass"), S(reader, "StableDataScopeId"),
            S(reader, "ControllerIdentity"), L(reader, "ClaimedPlaintextLength"), S(reader, "MediaType"),
            I(reader, "ContentCommitmentSchemaVersion"), S(reader, "ContentCommitmentKeyId"), I(reader, "ContentCommitmentKeyVersion"),
            B(reader, "ContentCommitment"), T(reader, "OwnershipLeaseExpiresAtUtc"),
            T(reader, "EffectivePlaintextRetentionExpiresAtUtc"), T(reader, "ReservationExpiresAtUtc"));
    }

    internal async Task<RawExportR2VerificationContext?> ReadVerificationContextAsync(
        Guid attemptId,
        long revision,
        long fence,
        CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
        command.CommandText = "SELECT * FROM tagekyc.raw_export_read_source_verification_context(@attempt,@revision,@fence)";
        command.Parameters.AddWithValue("attempt", attemptId);
        command.Parameters.AddWithValue("revision", revision);
        command.Parameters.AddWithValue("fence", fence);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        return new(
            G(reader, "AttemptId"), G(reader, "SourceArtifactId"), L(reader, "EncryptionAttemptRevision"), L(reader, "Fence"),
            G(reader, "AttemptKeyReservationId"), G(reader, "ProvisionalObjectIdentity"), B(reader, "EncryptionAttemptFingerprint"),
            S(reader, "KeyProviderId"), S(reader, "KekId"), I(reader, "KekVersion"), S(reader, "KekFingerprint"),
            S(reader, "EncryptionSuiteId"), I(reader, "EncryptionFramingVersion"), I(reader, "ChunkSize"), S(reader, "NonceStrategyId"),
            B(reader, "FramingParametersDigest"), B(reader, "WrappedDekMetadataDigest"), G(reader, "VerificationSessionId"),
            G(reader, "CaptureArtifactId"), I(reader, "CaptureRevision"), S(reader, "RawClass"), S(reader, "StableDataScopeId"),
            S(reader, "ControllerIdentity"), L(reader, "ClaimedPlaintextLength"), S(reader, "MediaType"),
            I(reader, "ContentCommitmentSchemaVersion"), S(reader, "ContentCommitmentKeyId"), I(reader, "ContentCommitmentKeyVersion"),
            B(reader, "ContentCommitment"));
    }

    internal async Task<RawExportR2KeyInspection?> InspectKeyAsync(
        Guid attemptKeyReservationId,
        CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
        command.CommandText = "SELECT * FROM tagekyc.raw_export_inspect_attempt_key_reservation(@id)";
        command.Parameters.AddWithValue("id", attemptKeyReservationId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        return new(
            reader.GetString(0), reader.GetGuid(1),
            reader.IsDBNull(2) ? null : reader.GetGuid(2),
            reader.GetInt64(3), reader.IsDBNull(4) ? null : (byte[])reader[4]);
    }

    internal Task<ProvisionalObjectBeginResult> BeginAsync(
        Guid attemptId,
        long revision,
        long fence,
        CancellationToken cancellationToken) =>
        objects.BeginAsync(attemptId, revision, fence, 64, cancellationToken);

    internal Task<ProvisionalObjectMutationResult> ArmAsync(
        Guid objectCustodyId,
        long revision,
        Guid operationId,
        CancellationToken cancellationToken) =>
        objects.ArmAsync(objectCustodyId, revision, operationId, cancellationToken);

    internal Task<ProvisionalObjectMutationResult> RecordNotArmedAsync(
        Guid objectCustodyId,
        long revision,
        byte[] objectBindingDigest,
        CancellationToken cancellationToken) =>
        objects.RecordNotArmedAsync(
            objectCustodyId,
            revision,
            C1HashCanonical.Compute(
                "tip-88c1-object-not-armed-evidence-v1",
                new C1HashCanonical.Scalar(Convert.ToHexString(objectBindingDigest).ToLowerInvariant()),
                new C1HashCanonical.Scalar(revision.ToString(CultureInfo.InvariantCulture)),
                new C1HashCanonical.Scalar("NotArmed")),
            cancellationToken);

    internal Task<ProvisionalObjectMutationResult> RecordPutResultAsync(
        Guid objectCustodyId,
        long revision,
        Guid operationId,
        ConditionalPutResult result,
        CancellationToken cancellationToken)
    {
        var kind = result.Outcome switch
        {
            ConditionalPutOutcome.Created => "Created",
            ConditionalPutOutcome.ConditionalConflict => "ConditionalConflictObserved",
            _ => "OutcomeUnknown",
        };
        return objects.RecordPutResultAsync(
            objectCustodyId,
            revision,
            operationId,
            kind,
            result.StatusCode,
            result.Outcome == ConditionalPutOutcome.Created ? result.BytesConsumed : null,
            result.CiphertextDigest,
            result.ProviderReceiptDigest,
            cancellationToken);
    }

    internal async Task<RawExportR2ObjectContext?> ReadObjectContextAsync(
        Guid objectCustodyId,
        CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
        command.CommandText = "SELECT * FROM tagekyc.raw_export_read_provisional_object_reconcile_context(@id)";
        command.Parameters.AddWithValue("id", objectCustodyId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        return new(
            G(reader, "ObjectCustodyId"), G(reader, "AttemptId"), G(reader, "AttemptKeyReservationId"),
            G(reader, "SourceArtifactId"), G(reader, "ProvisionalObjectIdentity"), L(reader, "EncryptionAttemptRevision"),
            L(reader, "AttemptFence"), B(reader, "EncryptionAttemptFingerprint"), S(reader, "ObjectKey"),
            B(reader, "ObjectBindingDigest"), S(reader, "State"), L(reader, "StateRevision"),
            NullableGuid(reader, "PutOperationId"), NullableLong(reader, "CiphertextLength"),
            NullableBytes(reader, "CiphertextDigest"), NullableBytes(reader, "ProviderReceiptDigest"),
            NullableBytes(reader, "VerificationEvidenceDigest"));
    }

    internal Task<ProvisionalObjectMutationResult> MarkVerifiedAsync(
        Guid objectCustodyId,
        long revision,
        byte[] verificationEvidenceDigest,
        CancellationToken cancellationToken) =>
        MutateObjectAsync(
            "raw_export_mark_provisional_object_verified",
            cancellationToken,
            ("id", objectCustodyId), ("revision", revision), ("evidence", verificationEvidenceDigest));

    internal Task<ProvisionalObjectMutationResult> MarkVerificationCleanupRequiredAsync(
        RawExportR2ObjectContext objectContext,
        CancellationToken cancellationToken)
    {
        var evidence = C1HashCanonical.Compute(
            "tip-88c1-object-cleanup-evidence-v1",
            new C1HashCanonical.Scalar(Convert.ToHexString(objectContext.ObjectBindingDigest).ToLowerInvariant()),
            new C1HashCanonical.Scalar(objectContext.State),
            new C1HashCanonical.Scalar(objectContext.StateRevision.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar("VerificationFailed"));
        return MutateObjectAsync(
            "raw_export_mark_provisional_object_cleanup_required",
            cancellationToken,
            ("id", objectContext.ObjectCustodyId), ("revision", objectContext.StateRevision),
            ("reason", "VerificationFailed"), ("evidence", evidence));
    }

    internal async Task<string> TerminateAsync(
        Guid attemptId,
        long revision,
        long fence,
        string disposition,
        CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
        command.CommandText = "SELECT tagekyc.raw_export_terminate_source_encryption_attempt(@attempt,@revision,@fence,@disposition)";
        command.Parameters.AddWithValue("attempt", attemptId);
        command.Parameters.AddWithValue("revision", revision);
        command.Parameters.AddWithValue("fence", fence);
        command.Parameters.AddWithValue("disposition", disposition);
        return (string)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? "StateConflict");
    }

    private async Task<ProvisionalObjectMutationResult> MutateObjectAsync(
        string function,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] values)
    {
        await using var command = await CreateCommandAsync(cancellationToken).ConfigureAwait(false);
        var names = new List<string>(values.Length);
        foreach (var value in values)
        {
            names.Add("@" + value.Name);
            command.Parameters.AddWithValue(value.Name, value.Value);
        }
        command.CommandText = $"SELECT * FROM tagekyc.{function}({string.Join(',', names)})";
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("RAW_EXPORT_R2_EMPTY_MUTATION_RESULT");
        return new(reader.GetString(0), reader.GetGuid(1), reader.GetString(2), reader.GetInt64(3));
    }

    private async Task<NpgsqlCommand> CreateCommandAsync(CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection.CreateCommand();
    }

    private static Guid G(NpgsqlDataReader reader, string name) => reader.GetGuid(reader.GetOrdinal(name));
    private static long L(NpgsqlDataReader reader, string name) => reader.GetInt64(reader.GetOrdinal(name));
    private static int I(NpgsqlDataReader reader, string name) => reader.GetInt32(reader.GetOrdinal(name));
    private static string S(NpgsqlDataReader reader, string name) => reader.GetString(reader.GetOrdinal(name));
    private static byte[] B(NpgsqlDataReader reader, string name) => (byte[])reader[reader.GetOrdinal(name)];
    private static DateTimeOffset T(NpgsqlDataReader reader, string name) => reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal(name));
    private static Guid? NullableGuid(NpgsqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : reader.GetGuid(i); }
    private static long? NullableLong(NpgsqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : reader.GetInt64(i); }
    private static byte[]? NullableBytes(NpgsqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : (byte[])reader[i]; }
}
