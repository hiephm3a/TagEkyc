using System.Data;
using Npgsql;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RecipientPackageRepository(IRecipientPackageConnectionFactory connections)
{
    internal async Task<RecipientKeyCandidate> SelectActiveKeyAsync(Guid recipientId, CancellationToken cancellationToken)
    {
        await using var owned = await CommandAsync(RecipientPackageDatabaseCapability.Preparer,
            "SELECT * FROM tagekyc.raw_export_select_active_recipient_key(@recipient)", cancellationToken);
        var command = owned.Command;
        command.Parameters.AddWithValue("recipient", recipientId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return new("Unavailable", null, null, null, null, null, null, null, null);
        return new(S(reader, "outcome"), NG(reader, "recipient_client_application_id"), NS(reader, "recipient_key_id"),
            NI(reader, "recipient_key_version"), NB(reader, "recipient_key_fingerprint"), NB(reader, "recipient_public_key_spki"),
            NL(reader, "recipient_key_revision"), NT(reader, "recipient_key_valid_from_utc"), NT(reader, "recipient_key_valid_until_utc"));
    }

    internal async Task<RecipientPackageReserveResult> ReserveAsync(RecipientPackageReserveRequest value, CancellationToken cancellationToken)
    {
        await using var owned = await CommandAsync(RecipientPackageDatabaseCapability.Preparer,
            "SELECT * FROM tagekyc.raw_export_reserve_recipient_package(@p,@package,@assembly,@job,@attempt,@fence,@assembly_fp,@manifest,@assembly_digest,@auth,@complete,@recipient,@key_id,@key_version,@key_fp,@key_revision,@equality,@token_digest,@provider_kind,@provider_config,@endpoint_fp,@bucket,@object_key,@object_binding,@profile)", cancellationToken);
        var command = owned.Command;
        Add(command, "p", value.C2PreparationId); Add(command, "package", value.PackageId); Add(command, "assembly", value.AssemblyId);
        Add(command, "job", value.JobId); Add(command, "attempt", value.AttemptId); Add(command, "fence", value.FencingToken);
        Add(command, "assembly_fp", value.AssemblyFingerprint); Add(command, "manifest", value.ManifestDigest);
        Add(command, "assembly_digest", value.AssemblyDigest); Add(command, "auth", value.AssemblyAuthenticationValue);
        Add(command, "complete", value.CompleteAssemblyLength); Add(command, "recipient", value.RecipientClientApplicationId);
        Add(command, "key_id", value.RecipientKeyId); Add(command, "key_version", value.RecipientKeyVersion);
        Add(command, "key_fp", value.RecipientKeyFingerprint); Add(command, "key_revision", value.RecipientKeyRevision);
        Add(command, "equality", value.PackageEqualityFingerprint); Add(command, "token_digest", value.ProviderOperationTokenDigest);
        Add(command, "provider_kind", value.ProviderKind); Add(command, "provider_config", value.ProviderConfigurationId);
        Add(command, "endpoint_fp", value.ProviderEndpointFingerprint); Add(command, "bucket", value.BucketName);
        Add(command, "object_key", value.ObjectKey); Add(command, "object_binding", value.ObjectBindingDigest); Add(command, "profile", value.PackageProfile);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return EmptyReserve("Unavailable");
        return new(S(reader, "outcome"), NL(reader, "row_revision"), NS(reader, "state"), NG(reader, "package_id"),
            NB(reader, "package_equality_fingerprint"), NS(reader, "recipient_key_id"), NI(reader, "recipient_key_version"),
            NB(reader, "recipient_key_fingerprint"), NB(reader, "recipient_public_key_spki"), NL(reader, "recipient_key_revision"),
            NT(reader, "recipient_key_valid_from_utc"), NT(reader, "recipient_key_valid_until_utc"),
            NB(reader, "provider_operation_token_digest"), NB(reader, "object_binding_digest"), NB(reader, "provider_receipt_digest"));
    }

    internal Task<RecipientPackageMutation> BeginPutAsync(Guid id, long revision, byte[] envelope, long length, byte[] digest, CancellationToken token) =>
        MutationAsync(RecipientPackageDatabaseCapability.Preparer, "raw_export_begin_recipient_package_put", token,
            ("p", id), ("revision", revision), ("envelope", envelope), ("length", length), ("digest", digest));

    internal Task<RecipientPackageMutation> RecordUnknownAsync(Guid id, long revision, byte[] evidence, CancellationToken token) =>
        MutationAsync(RecipientPackageDatabaseCapability.Preparer, "raw_export_record_recipient_package_put_unknown", token,
            ("p", id), ("revision", revision), ("evidence", evidence));

    internal Task<RecipientPackageMutation> RecordPreparedAsync(
        RecipientPackageDatabaseCapability capability, Guid id, long revision, byte[] entityTagDigest,
        long length, byte[] digest, byte[] conditional, byte[] receipt, CancellationToken token) =>
        MutationAsync(capability, "raw_export_record_recipient_package_prepared", token,
            ("p", id), ("revision", revision), ("etag", entityTagDigest), ("length", length),
            ("digest", digest), ("conditional", conditional), ("receipt", receipt));

    internal Task<RecipientPackageMutation> FinalizeAsync(Guid id, long revision, byte[] assemblyFingerprint, CancellationToken token) =>
        MutationAsync(RecipientPackageDatabaseCapability.Preparer, "raw_export_finalize_recipient_package", token,
            ("p", id), ("revision", revision), ("assembly", assemblyFingerprint));

    internal Task<RecipientPackageMutation> AuthorizeAbortAsync(Guid id, long revision, byte[] digest, CancellationToken token) =>
        MutationAsync(RecipientPackageDatabaseCapability.Lifecycle, "raw_export_authorize_recipient_package_abort", token,
            ("p", id), ("revision", revision), ("digest", digest));

    internal Task<RecipientPackageMutation> RecordAbortResultAsync(Guid id, long revision, string kind, byte[] evidence, CancellationToken token) =>
        MutationAsync(RecipientPackageDatabaseCapability.Lifecycle, "raw_export_record_recipient_package_abort_result", token,
            ("p", id), ("revision", revision), ("kind", kind), ("evidence", evidence));

    internal Task<RecipientPackageMutation> RecordQuarantinedAsync(
        RecipientPackageDatabaseCapability capability, Guid id, long revision, byte[] evidence, string reason, CancellationToken token) =>
        MutationAsync(capability, "raw_export_record_recipient_package_quarantined", token,
            ("p", id), ("revision", revision), ("evidence", evidence), ("reason", reason));

    internal async Task<RecipientPackageRecoveryContext?> ReadAsync(
        RecipientPackageDatabaseCapability capability, Guid id, CancellationToken cancellationToken)
    {
        await using var owned = await CommandAsync(capability,
            "SELECT * FROM tagekyc.raw_export_read_recipient_package_recovery_context(@p)", cancellationToken);
        var command = owned.Command;
        command.Parameters.AddWithValue("p", id);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            || !string.Equals(S(reader, "outcome"), "Found", StringComparison.Ordinal)) return null;
        return new(S(reader, "outcome"), L(reader, "row_revision"), S(reader, "state"), G(reader, "c2_preparation_id"),
            G(reader, "package_id"), B(reader, "package_equality_fingerprint"), G(reader, "assembly_id"), G(reader, "job_id"),
            G(reader, "attempt_id"), L(reader, "fencing_token"), B(reader, "assembly_fingerprint"), B(reader, "manifest_digest"),
            B(reader, "assembly_digest"), B(reader, "assembly_authentication_value"), L(reader, "complete_assembly_length"),
            G(reader, "recipient_client_application_id"), S(reader, "recipient_key_id"), I(reader, "recipient_key_version"),
            B(reader, "recipient_key_fingerprint"), B(reader, "recipient_public_key_spki"), L(reader, "recipient_key_revision"),
            T(reader, "recipient_key_valid_from_utc"), T(reader, "recipient_key_valid_until_utc"), S(reader, "package_profile"),
            B(reader, "provider_operation_token_digest"), S(reader, "provider_kind"), S(reader, "provider_configuration_id"),
            B(reader, "provider_endpoint_fingerprint"), S(reader, "bucket_name"), S(reader, "object_key"), B(reader, "object_binding_digest"),
            NB(reader, "envelope_digest"), NL(reader, "encrypted_package_length"), NB(reader, "package_ciphertext_digest"),
            NB(reader, "conditional_create_evidence_digest"), NB(reader, "provider_receipt_digest"), NB(reader, "abort_authorization_digest"),
            NB(reader, "positive_absence_evidence_digest"), NB(reader, "cleanup_progress_evidence_digest"), NB(reader, "quarantine_evidence_digest"));
    }

    private async Task<RecipientPackageMutation> MutationAsync(
        RecipientPackageDatabaseCapability capability, string function, CancellationToken cancellationToken,
        params (string Name, object Value)[] values)
    {
        var placeholders = string.Join(',', values.Select(item => '@' + item.Name));
        await using var owned = await CommandAsync(capability, $"SELECT * FROM tagekyc.{function}({placeholders})", cancellationToken);
        var command = owned.Command;
        foreach (var value in values) Add(command, value.Name, value.Value);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return new("Unavailable", null, null);
        return new(S(reader, "outcome"), NL(reader, "row_revision"), NS(reader, "state"),
            Has(reader, "provider_receipt_digest") ? NB(reader, "provider_receipt_digest") : null);
    }

    private async Task<OwnedCommand> CommandAsync(
        RecipientPackageDatabaseCapability capability, string sql, CancellationToken cancellationToken)
    {
        var connection = await connections.OpenAsync(capability, cancellationToken).ConfigureAwait(false);
        var command = new NpgsqlCommand(sql, connection)
        {
            CommandTimeout = checked((int)RecipientPackageOptions.OperationTimeout.TotalSeconds),
        };
        return new(connection, command);
    }

    private sealed class OwnedCommand(NpgsqlConnection connection, NpgsqlCommand command) : IAsyncDisposable
    {
        internal NpgsqlCommand Command { get; } = command;

        public async ValueTask DisposeAsync()
        {
            await Command.DisposeAsync().ConfigureAwait(false);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static RecipientPackageReserveResult EmptyReserve(string outcome) =>
        new(outcome, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
    private static void Add(NpgsqlCommand command, string name, object value) => command.Parameters.AddWithValue(name, value);
    private static bool Has(NpgsqlDataReader r, string name) { try { _ = r.GetOrdinal(name); return true; } catch (IndexOutOfRangeException) { return false; } }
    private static Guid G(NpgsqlDataReader r, string n) => r.GetGuid(r.GetOrdinal(n));
    private static Guid? NG(NpgsqlDataReader r, string n) { var i=r.GetOrdinal(n); return r.IsDBNull(i)?null:r.GetGuid(i); }
    private static long L(NpgsqlDataReader r, string n) => r.GetInt64(r.GetOrdinal(n));
    private static long? NL(NpgsqlDataReader r, string n) { var i=r.GetOrdinal(n); return r.IsDBNull(i)?null:r.GetInt64(i); }
    private static int I(NpgsqlDataReader r, string n) => r.GetInt32(r.GetOrdinal(n));
    private static int? NI(NpgsqlDataReader r, string n) { var i=r.GetOrdinal(n); return r.IsDBNull(i)?null:r.GetInt32(i); }
    private static string S(NpgsqlDataReader r, string n) => r.GetString(r.GetOrdinal(n));
    private static string? NS(NpgsqlDataReader r, string n) { var i=r.GetOrdinal(n); return r.IsDBNull(i)?null:r.GetString(i); }
    private static byte[] B(NpgsqlDataReader r, string n) => (byte[])r[r.GetOrdinal(n)];
    private static byte[]? NB(NpgsqlDataReader r, string n) { var i=r.GetOrdinal(n); return r.IsDBNull(i)?null:(byte[])r[i]; }
    private static DateTimeOffset T(NpgsqlDataReader r, string n) => r.GetFieldValue<DateTimeOffset>(r.GetOrdinal(n));
    private static DateTimeOffset? NT(NpgsqlDataReader r, string n) { var i=r.GetOrdinal(n); return r.IsDBNull(i)?null:r.GetFieldValue<DateTimeOffset>(i); }
}
