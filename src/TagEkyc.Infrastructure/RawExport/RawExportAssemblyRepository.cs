using System.Data;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed record RawExportAssemblyMutation(string Outcome, long? RowRevision);

internal sealed record RawExportAssemblySealMutation(
    string Outcome,
    long? JobRevision,
    long? PreparationRevision,
    DateTimeOffset? SealedAtUtc);

internal sealed record RawExportAssemblyRecoveryContext(
    Guid C2PreparationId,
    Guid AssemblyId,
    Guid JobId,
    Guid AttemptId,
    long FencingToken,
    byte[] AssemblyFingerprint,
    byte[] PreparationFingerprint,
    string Disposition,
    byte[]? ProviderReceiptDigest,
    byte[]? AbortAuthorizationDigest,
    long RowRevision);

internal sealed record RawExportCommittedAssemblyRecoveryContext(
    Guid C2PreparationId,
    Guid AssemblyId,
    Guid JobId,
    Guid AttemptId,
    long FencingToken,
    byte[] AssemblyFingerprint,
    byte[] PreparationFingerprint,
    string Disposition,
    byte[]? ProviderReceiptDigest,
    byte[]? AbortAuthorizationDigest,
    long RowRevision,
    long JobRevision);

internal enum RawExportAssemblyDatabaseCapability
{
    Resolver,
    Sealer,
}

internal interface IRawExportAssemblyConnectionFactory
{
    Task<NpgsqlConnection> OpenAsync(
        RawExportAssemblyDatabaseCapability capability,
        CancellationToken cancellationToken);
}

internal sealed record RawExportAssemblyBindingSnapshot(
    int Ordinal,
    int SubjectRefTokenSchemaVersion,
    string SubjectRefTokenKeyId,
    int SubjectRefTokenKeyVersion,
    byte[] SubjectRefToken);

internal sealed record RawExportAssemblyJobSnapshot(
    Guid JobId,
    Guid PermitId,
    Guid VerificationSessionId,
    Guid ClientApplicationId,
    Guid RecipientClientApplicationId,
    Guid PolicyId,
    int PolicyVersion,
    string PurposeCode,
    string ExportMode,
    DateTimeOffset JobExpiresAtUtc,
    DateTimeOffset CreatedAtUtc,
    Guid AttemptId,
    long Revision,
    long Fence,
    IReadOnlyList<RawExportAssemblyBindingSnapshot> Bindings);

internal sealed record RawExportAssemblyVerificationContext(
    Guid JobSourceBindingId,
    byte[] BindingFingerprint,
    Guid PermitId,
    Guid ClientApplicationId,
    Guid RecipientClientApplicationId,
    Guid PolicyId,
    int PolicyVersion,
    string PurposeCode,
    string ExportMode,
    DateTimeOffset JobExpiresAtUtc,
    DateTimeOffset JobCreatedAtUtc,
    int SubjectRefTokenSchemaVersion,
    string SubjectRefTokenKeyId,
    int SubjectRefTokenKeyVersion,
    byte[] SubjectRefToken,
    RawExportR2VerificationContext Verification,
    RawExportR2ObjectContext Object);

internal sealed class RawExportAssemblyRepository(IRawExportAssemblyConnectionFactory connections)
{
    internal async Task<RawExportAssemblyMutation> FreezeAsync(
        RawExportAssemblyExecutionRequest request,
        CancellationToken cancellationToken)
    {
        await using var scope = await ResolverScope.OpenAsync(connections, request.ActorPrincipalId, cancellationToken).ConfigureAwait(false);
        await using var command = scope.Connection.CreateCommand();
        command.Transaction = scope.Transaction;
        command.CommandText = "SELECT * FROM tagekyc.raw_export_freeze_job_source_bindings(@job,@attempt,@revision,@fence,@actor)";
        command.Parameters.AddWithValue("job", request.JobId);
        command.Parameters.AddWithValue("attempt", request.AttemptId);
        command.Parameters.AddWithValue("revision", request.ExpectedJobRevision);
        command.Parameters.AddWithValue("fence", request.ExpectedFence);
        command.Parameters.AddWithValue("actor", request.ActorPrincipalId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("RAW_EXPORT_ASSEMBLY_EMPTY_FREEZE_RESULT");
        var result = new RawExportAssemblyMutation(reader.GetString(reader.GetOrdinal("Outcome")), reader.GetInt32(reader.GetOrdinal("BindingCount")));
        await reader.CloseAsync().ConfigureAwait(false);
        await scope.Transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    internal async Task<RawExportAssemblyJobSnapshot?> ReadJobAsync(
        RawExportAssemblyExecutionRequest request,
        int bindingCount,
        CancellationToken cancellationToken)
    {
        if (bindingCount < 1) return null;
        var contexts = new List<RawExportAssemblyVerificationContext>(bindingCount);
        for (var ordinal = 0; ordinal < bindingCount; ordinal++)
        {
            var context = await ReadVerificationContextAsync(request, ordinal, cancellationToken).ConfigureAwait(false);
            if (context is null) return null;
            contexts.Add(context);
        }

        var first = contexts[0];
        if (contexts.Any(context =>
                context.PermitId != first.PermitId
                || context.Verification.VerificationSessionId != first.Verification.VerificationSessionId
                || context.ClientApplicationId != first.ClientApplicationId
                || context.RecipientClientApplicationId != first.RecipientClientApplicationId
                || context.PolicyId != first.PolicyId
                || context.PolicyVersion != first.PolicyVersion
                || !string.Equals(context.PurposeCode, first.PurposeCode, StringComparison.Ordinal)
                || !string.Equals(context.ExportMode, first.ExportMode, StringComparison.Ordinal)
                || context.JobExpiresAtUtc != first.JobExpiresAtUtc
                || context.JobCreatedAtUtc != first.JobCreatedAtUtc
                || context.SubjectRefTokenSchemaVersion != first.SubjectRefTokenSchemaVersion
                || !string.Equals(context.SubjectRefTokenKeyId, first.SubjectRefTokenKeyId, StringComparison.Ordinal)
                || context.SubjectRefTokenKeyVersion != first.SubjectRefTokenKeyVersion
                || !Fixed(context.SubjectRefToken, first.SubjectRefToken)))
            return null;

        var bindings = contexts.Select((context, ordinal) => new RawExportAssemblyBindingSnapshot(
            ordinal,
            context.SubjectRefTokenSchemaVersion,
            context.SubjectRefTokenKeyId,
            context.SubjectRefTokenKeyVersion,
            context.SubjectRefToken.ToArray())).ToArray();
        return new(
            request.JobId, first.PermitId, first.Verification.VerificationSessionId, first.ClientApplicationId,
            first.RecipientClientApplicationId, first.PolicyId, first.PolicyVersion,
            first.PurposeCode, first.ExportMode, first.JobExpiresAtUtc, first.JobCreatedAtUtc,
            request.AttemptId, request.ExpectedJobRevision, request.ExpectedFence, bindings);
    }

    internal async Task<RawExportAssemblyVerificationContext?> ReadVerificationContextAsync(
        RawExportAssemblyExecutionRequest request,
        int ordinal,
        CancellationToken cancellationToken)
    {
        await using var scope = await ResolverScope.OpenAsync(connections, request.ActorPrincipalId, cancellationToken).ConfigureAwait(false);
        await using var command = scope.Connection.CreateCommand();
        command.Transaction = scope.Transaction;
        command.CommandText = "SELECT * FROM tagekyc.raw_export_read_job_source_verification_context(@job,@ordinal,@attempt,@revision,@fence,@actor)";
        command.Parameters.AddWithValue("job", request.JobId);
        command.Parameters.AddWithValue("ordinal", ordinal);
        command.Parameters.AddWithValue("attempt", request.AttemptId);
        command.Parameters.AddWithValue("revision", request.ExpectedJobRevision);
        command.Parameters.AddWithValue("fence", request.ExpectedFence);
        command.Parameters.AddWithValue("actor", request.ActorPrincipalId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;

        var verification = new RawExportR2VerificationContext(
            G(reader, "AttemptId"), G(reader, "SourceArtifactId"), L(reader, "EncryptionAttemptRevision"), L(reader, "Fence"),
            G(reader, "AttemptKeyReservationId"), G(reader, "ProvisionalObjectIdentity"), B(reader, "EncryptionAttemptFingerprint"),
            S(reader, "KeyProviderId"), S(reader, "KekId"), I(reader, "KekVersion"), S(reader, "KekFingerprint"),
            S(reader, "EncryptionSuiteId"), I(reader, "EncryptionFramingVersion"), I(reader, "ChunkSize"), S(reader, "NonceStrategyId"),
            B(reader, "FramingParametersDigest"), B(reader, "WrappedDekMetadataDigest"), G(reader, "VerificationSessionId"),
            G(reader, "CaptureArtifactId"), I(reader, "CaptureRevision"), S(reader, "RawClass"), S(reader, "StableDataScopeId"),
            S(reader, "ControllerIdentity"), L(reader, "ClaimedPlaintextLength"), S(reader, "MediaType"),
            I(reader, "ContentCommitmentSchemaVersion"), S(reader, "ContentCommitmentKeyId"), I(reader, "ContentCommitmentKeyVersion"),
            B(reader, "ContentCommitment"));
        var objectContext = new RawExportR2ObjectContext(
            G(reader, "ObjectCustodyId"), verification.AttemptId, verification.AttemptKeyReservationId,
            verification.SourceArtifactId, verification.ProvisionalObjectIdentity, verification.EncryptionAttemptRevision,
            verification.Fence, verification.EncryptionAttemptFingerprint, S(reader, "ObjectKey"),
            B(reader, "ObjectBindingDigest"), S(reader, "ObjectState"), L(reader, "ObjectStateRevision"),
            null, L(reader, "CiphertextLength"), B(reader, "CiphertextDigest"), null, null);
        var result = new RawExportAssemblyVerificationContext(
            G(reader, "JobSourceBindingId"), B(reader, "BindingFingerprint"),
            G(reader, "PermitId"), G(reader, "ClientApplicationId"), G(reader, "RecipientClientApplicationId"),
            G(reader, "PolicyId"), I(reader, "PolicyVersion"), S(reader, "PurposeCode"), S(reader, "ExportMode"),
            T(reader, "JobExpiresAtUtc"), T(reader, "JobCreatedAtUtc"),
            I(reader, "SubjectRefTokenSchemaVersion"), S(reader, "SubjectRefTokenKeyId"),
            I(reader, "SubjectRefTokenKeyVersion"), B(reader, "SubjectRefToken"), verification, objectContext);
        await reader.CloseAsync().ConfigureAwait(false);
        await scope.Transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    internal Task<RawExportAssemblyMutation> RegisterPreparingAsync(
        Guid preparationId, Guid assemblyId, RawExportAssemblyExecutionRequest request,
        byte[] assemblyFingerprint, byte[] preparationFingerprint, CancellationToken cancellationToken) =>
        MutationAsync(
            "raw_export_register_assembly_preparing", cancellationToken,
            ("preparation", preparationId), ("assembly", assemblyId), ("job", request.JobId),
            ("revision", request.ExpectedJobRevision), ("fence", request.ExpectedFence),
            ("assembly_fingerprint", assemblyFingerprint), ("preparation_fingerprint", preparationFingerprint));

    internal Task<RawExportAssemblyMutation> RecordPendingAsync(
        Guid preparationId, long rowRevision, byte[] receiptDigest, CancellationToken cancellationToken) =>
        MutationAsync("raw_export_record_assembly_pending", cancellationToken,
            ("preparation", preparationId), ("revision", rowRevision), ("receipt", receiptDigest));

    internal async Task<RawExportAssemblySealMutation> SealAsync(
        Guid preparationId,
        RawExportAssemblyExecutionRequest request,
        long preparationRevision,
        RawExportAssemblyDerivation derivation,
        string authenticationKeyId,
        int authenticationKeyVersion,
        IReadOnlyList<RawExportAssemblyItemDescriptor> items,
        CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(RawExportAssemblyDatabaseCapability.Sealer, cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM tagekyc.raw_export_seal_authenticated_assembly(@preparation,@job,@prep_revision,@job_revision,@fence,@attempt,@assembly_digest,@manifest_digest,@authentication_value,@key_id,@key_version,@assembly_fingerprint,@length,@count,@items)";
        Add(command, "preparation", preparationId); Add(command, "job", request.JobId);
        Add(command, "prep_revision", preparationRevision); Add(command, "job_revision", request.ExpectedJobRevision);
        Add(command, "fence", request.ExpectedFence); Add(command, "attempt", request.AttemptId);
        Add(command, "assembly_digest", derivation.AssemblyDigest); Add(command, "manifest_digest", derivation.ManifestDigest);
        Add(command, "authentication_value", derivation.AssemblyAuthenticationValue); Add(command, "key_id", authenticationKeyId);
        Add(command, "key_version", authenticationKeyVersion); Add(command, "assembly_fingerprint", derivation.AssemblyFingerprint);
        Add(command, "length", derivation.CompleteAssemblyLength); Add(command, "count", items.Count);
        command.Parameters.AddWithValue("items", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(items));
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("RAW_EXPORT_ASSEMBLY_EMPTY_SEAL_RESULT");
        return new(S(reader, "Outcome"), NL(reader, "JobRevision"), NL(reader, "PreparationRevision"), NT(reader, "SealedAtUtc"));
    }

    internal Task<RawExportAssemblyMutation> RecordFinalizedAsync(
        Guid preparationId, long rowRevision, byte[] fingerprint, CancellationToken cancellationToken) =>
        MutationAsync("raw_export_record_assembly_finalized", cancellationToken,
            ("preparation", preparationId), ("revision", rowRevision), ("fingerprint", fingerprint));

    internal Task<RawExportAssemblyMutation> AuthorizeAbortAsync(
        Guid preparationId, long rowRevision, byte[] authorizationDigest, CancellationToken cancellationToken) =>
        MutationAsync("raw_export_authorize_assembly_abort", cancellationToken,
            ("preparation", preparationId), ("revision", rowRevision), ("abort_authorization_digest", authorizationDigest));

    internal Task<RawExportAssemblyMutation> RecordAbortedAsync(
        Guid preparationId, long rowRevision, byte[] authorizationDigest, CancellationToken cancellationToken) =>
        MutationAsync("raw_export_record_assembly_aborted", cancellationToken,
            ("preparation", preparationId), ("revision", rowRevision), ("abort_authorization_digest", authorizationDigest));

    internal async Task<RawExportAssemblyRecoveryContext?> ReadRecoveryContextAsync(
        Guid preparationId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(RawExportAssemblyDatabaseCapability.Sealer, cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM tagekyc.raw_export_read_assembly_recovery_context(@preparation)";
        Add(command, "preparation", preparationId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        return new(
            G(reader, "C2PreparationId"), G(reader, "AssemblyId"), G(reader, "JobId"), G(reader, "AttemptId"),
            L(reader, "FencingToken"), B(reader, "AssemblyFingerprint"), B(reader, "PreparationFingerprint"),
            S(reader, "Disposition"), NB(reader, "ProviderReceiptDigest"), NB(reader, "AbortAuthorizationDigest"),
            L(reader, "RowRevision"));
    }

    internal async Task<RawExportCommittedAssemblyRecoveryContext?> ReadCommittedRecoveryContextAsync(
        RawExportAssemblyExecutionRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(RawExportAssemblyDatabaseCapability.Sealer, cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM tagekyc.raw_export_read_committed_assembly_recovery_context(@job,@attempt,@revision,@fence)";
        Add(command, "job", request.JobId);
        Add(command, "attempt", request.AttemptId);
        Add(command, "revision", request.ExpectedJobRevision);
        Add(command, "fence", request.ExpectedFence);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        return new(
            G(reader, "C2PreparationId"), G(reader, "AssemblyId"), G(reader, "JobId"), G(reader, "AttemptId"),
            L(reader, "FencingToken"), B(reader, "AssemblyFingerprint"), B(reader, "PreparationFingerprint"),
            S(reader, "Disposition"), NB(reader, "ProviderReceiptDigest"), NB(reader, "AbortAuthorizationDigest"),
            L(reader, "RowRevision"), L(reader, "JobRevision"));
    }

    private async Task<RawExportAssemblyMutation> MutationAsync(
        string function,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] values)
    {
        await using var connection = await connections.OpenAsync(RawExportAssemblyDatabaseCapability.Sealer, cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM tagekyc.{function}({string.Join(',', values.Select(value => '@' + value.Name))})";
        foreach (var value in values) Add(command, value.Name, value.Value);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("RAW_EXPORT_ASSEMBLY_EMPTY_MUTATION_RESULT");
        return new(S(reader, "Outcome"), NL(reader, "RowRevision"));
    }

    private static void Add(NpgsqlCommand command, string name, object value) => command.Parameters.AddWithValue(name, value);
    private static Guid G(NpgsqlDataReader reader, string name) => reader.GetGuid(reader.GetOrdinal(name));
    private static long L(NpgsqlDataReader reader, string name) => reader.GetInt64(reader.GetOrdinal(name));
    private static int I(NpgsqlDataReader reader, string name) => reader.GetInt32(reader.GetOrdinal(name));
    private static string S(NpgsqlDataReader reader, string name) => reader.GetString(reader.GetOrdinal(name));
    private static byte[] B(NpgsqlDataReader reader, string name) => (byte[])reader[reader.GetOrdinal(name)];
    private static DateTimeOffset T(NpgsqlDataReader reader, string name) => reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal(name));
    private static byte[]? NB(NpgsqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : (byte[])reader[i]; }
    private static long? NL(NpgsqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : reader.GetInt64(i); }
    private static DateTimeOffset? NT(NpgsqlDataReader reader, string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : reader.GetFieldValue<DateTimeOffset>(i); }
    private static bool Fixed(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) =>
        left.Length == right.Length && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(left, right);

    private sealed class ResolverScope : IAsyncDisposable
    {
        private ResolverScope(NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        internal NpgsqlConnection Connection { get; }
        internal NpgsqlTransaction Transaction { get; }

        internal static async Task<ResolverScope> OpenAsync(
            IRawExportAssemblyConnectionFactory factory,
            Guid actorPrincipalId,
            CancellationToken cancellationToken)
        {
            if (actorPrincipalId == Guid.Empty)
                throw new ArgumentException("RAW_EXPORT_ASSEMBLY_ACTOR_INVALID", nameof(actorPrincipalId));
            var connection = await factory.OpenAsync(RawExportAssemblyDatabaseCapability.Resolver, cancellationToken).ConfigureAwait(false);
            var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using var actor = connection.CreateCommand();
            actor.Transaction = transaction;
            actor.CommandText = "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true)";
            actor.Parameters.AddWithValue("actor", NpgsqlDbType.Text, actorPrincipalId.ToString("D"));
            await actor.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return new ResolverScope(connection, transaction);
        }

        public async ValueTask DisposeAsync()
        {
            await Transaction.DisposeAsync().ConfigureAwait(false);
            await Connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
