using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.ProtectedValues;
using TagEkyc.Infrastructure.Secrets;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed record RawExportAssemblyRuntimeOptions(
    string? ResolverConnectionString,
    string? SealerConnectionString,
    string AuthenticationKeyId,
    int AuthenticationKeyVersion,
    string? AuthenticationKeySecretRef,
    bool IsValid)
{
    internal const string SectionPath = "TagEkyc:RawExport:Assembly";
    internal const string ResolverLogin = "tagekyc_raw_export_assembly_resolver_login";
    internal const string SealerLogin = "tagekyc_raw_export_assembly_sealer_login";

    internal static RawExportAssemblyRuntimeOptions Resolve(IConfiguration configuration, bool isProduction)
    {
        var section = configuration.GetSection(SectionPath);
        var resolver = Connection(section["ResolverConnectionString"], section["ResolverConnectionStringSecretRef"], isProduction);
        var sealer = Connection(section["SealerConnectionString"], section["SealerConnectionStringSecretRef"], isProduction);
        var keyId = section["Authentication:KeyId"] ?? string.Empty;
        var keyVersionValid = int.TryParse(section["Authentication:KeyVersion"], out var keyVersion) && keyVersion > 0;
        var keyRef = section["Authentication:KeySecretRef"];
        return new(resolver.Value, sealer.Value, keyId, keyVersion, keyRef,
            resolver.Valid && sealer.Valid && keyVersionValid
            && !string.IsNullOrWhiteSpace(keyId) && !string.IsNullOrWhiteSpace(keyRef));
    }

    private static (string? Value, bool Valid) Connection(string? plain, string? secretRef, bool isProduction)
    {
        if (isProduction && !string.IsNullOrWhiteSpace(plain)) return (null, false);
        if (!string.IsNullOrWhiteSpace(secretRef))
        {
            try
            {
                var value = SecretRefResolver.Resolve(secretRef).Value;
                return (value, !string.IsNullOrWhiteSpace(value));
            }
            catch (SecretRefResolutionException) { return (null, false); }
        }
        return !isProduction && !string.IsNullOrWhiteSpace(plain) ? (plain, true) : (null, false);
    }

    public override string ToString() =>
        $"RawExportAssemblyRuntimeOptions {{ KeyId = {AuthenticationKeyId}, KeyVersion = {AuthenticationKeyVersion}, Connections = [REDACTED] }}";
}

internal sealed class RawExportAssemblyConnectionFactory(RawExportAssemblyRuntimeOptions options)
    : IRawExportAssemblyConnectionFactory
{
    public async Task<NpgsqlConnection> OpenAsync(
        RawExportAssemblyDatabaseCapability capability,
        CancellationToken cancellationToken)
    {
        var (connectionString, expectedUser) = capability switch
        {
            RawExportAssemblyDatabaseCapability.Resolver =>
                (options.ResolverConnectionString, RawExportAssemblyRuntimeOptions.ResolverLogin),
            RawExportAssemblyDatabaseCapability.Sealer =>
                (options.SealerConnectionString, RawExportAssemblyRuntimeOptions.SealerLogin),
            _ => throw new ArgumentOutOfRangeException(nameof(capability)),
        };
        if (!options.IsValid || string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(RawExportAssemblyOptions.RoleTopologyInvalid);
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT current_user";
            var user = (string?)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (!string.Equals(user, expectedUser, StringComparison.Ordinal))
                throw new InvalidOperationException(RawExportAssemblyOptions.RoleTopologyInvalid);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}

internal sealed class RawExportAssemblyAuthenticationCatalog(RawExportAssemblyRuntimeOptions options)
    : IProtectedValueCatalog
{
    internal ProtectedValueRequest Request => new(
        ProtectedValuePurpose.AssemblyAuthenticationHmac,
        new ProtectedValueId($"{options.AuthenticationKeyId}:{options.AuthenticationKeyVersion}"));

    public ValueTask<ProtectedValueDescriptor?> FindAsync(
        ProtectedValueRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!options.IsValid || request != Request || string.IsNullOrWhiteSpace(options.AuthenticationKeySecretRef))
            return ValueTask.FromResult<ProtectedValueDescriptor?>(null);
        return ValueTask.FromResult<ProtectedValueDescriptor?>(new(
            Request,
            new ProtectedValueReference($"secret-ref:{options.AuthenticationKeySecretRef}")));
    }
}

internal sealed class ProtectedValueRawExportAssemblyAuthenticationProvider(
    RawExportAssemblyRuntimeOptions options,
    RawExportAssemblyAuthenticationCatalog catalog,
    ProtectedValueProviderRegistry registry,
    ProtectedValueResolverOptions resolverOptions,
    TimeProvider timeProvider) : IRawExportAssemblyAuthenticationProvider
{
    public string KeyId => options.AuthenticationKeyId;
    public int KeyVersion => options.AuthenticationKeyVersion;

    public async Task<byte[]> AuthenticateManifestAsync(
        ReadOnlyMemory<byte> authenticationPayload,
        CancellationToken cancellationToken)
    {
        var resolver = new ProtectedValueResolver(catalog, registry, resolverOptions, timeProvider);
        var resolution = await resolver.ResolveAsync(catalog.Request, cancellationToken).ConfigureAwait(false);
        if (resolution.State != ProtectedValueResolutionState.Found || resolution.MaterialLease is null)
        {
            resolution.MaterialLease?.Dispose();
            throw new InvalidOperationException(RawExportAssemblyOptions.AuthenticatorUnavailable);
        }
        using var lease = resolution.MaterialLease;
        if (lease.Material.Length != 32)
            throw new InvalidOperationException(RawExportAssemblyOptions.AuthenticatorUnavailable);
        return HMACSHA256.HashData(lease.Material.Span, authenticationPayload.Span);
    }
}

internal sealed class RawExportAssemblyWorkerIdentity(Guid value)
{
    private long schedulingTurn;

    internal Guid Value { get; } = value;

    // The identity is singleton-scoped while work sources are scoped per poll.
    // Keep the bounded mixed-queue schedule here so recovery cannot starve
    // merely because each host iteration creates a fresh source instance.
    internal bool PreferRecoveryThisTurn() =>
        Interlocked.Increment(ref schedulingTurn) % 4 == 0;
}

internal sealed class DurableRawExportAssemblyWorkSource(
    IRawExportAssemblyConnectionFactory connections,
    IRawExportJobRepository jobs,
    RawExportAssemblyWorkerIdentity worker,
    RawExportAssemblyRepository assemblies,
    ILogger<DurableRawExportAssemblyWorkSource>? suppliedLogger = null) : IRawExportAssemblyWorkSource
{
    private const int RecoveryLeaseSeconds = 300;
    private const int RecoveryRetryBaseSeconds = 1;
    private readonly ILogger<DurableRawExportAssemblyWorkSource> logger =
        suppliedLogger ?? NullLogger<DurableRawExportAssemblyWorkSource>.Instance;
    private AuthenticatedRawExportActor? acquiredActor;

    public async ValueTask<RawExportAssemblyExecutionRequest?> TryAcquireAsync(
        CancellationToken cancellationToken = default)
    {
        var recoveryAlreadyProbed = worker.PreferRecoveryThisTurn();
        if (recoveryAlreadyProbed)
        {
            var preferredRecovery = await TryAcquireRecoveryAsync(cancellationToken).ConfigureAwait(false);
            if (preferredRecovery is not null) return preferredRecovery;
        }

        await using var connection = await connections.OpenAsync(
            RawExportAssemblyDatabaseCapability.Resolver, cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM tagekyc.raw_export_next_assembly_candidate()";
        await using var reader = await command.ExecuteReaderAsync(
            System.Data.CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            await reader.CloseAsync().ConfigureAwait(false);
            return recoveryAlreadyProbed
                ? null
                : await TryAcquireRecoveryAsync(cancellationToken).ConfigureAwait(false);
        }

        var jobId = reader.GetGuid(reader.GetOrdinal("JobId"));
        var revision = reader.GetInt64(reader.GetOrdinal("Revision"));
        var fence = reader.GetInt64(reader.GetOrdinal("FencingToken"));
        var actor = new AuthenticatedRawExportActor(
            reader.GetGuid(reader.GetOrdinal("PrincipalId")),
            reader.GetGuid(reader.GetOrdinal("ClientApplicationId")),
            reader.GetGuid(reader.GetOrdinal("CreatedByApiKeyId")));
        RawExportJobLeaseResult acquired;
        try
        {
            acquired = await jobs.AcquireOrReclaimLeaseAsync(
                new AcquireOrReclaimRawExportJobLeaseCommand(
                    actor, jobId, revision, fence, worker.Value),
                cancellationToken).ConfigureAwait(false);
        }
        catch (RawExportJobException exception) when (
            exception.Code == "RAW_EXPORT_JOB_CONCURRENCY_CONFLICT")
        {
            logger.LogInformation(
                "RAW_EXPORT_ASSEMBLY_ACQUIRE_LOST_RACE FaultStage=TryAcquireAsync JobId={JobId}",
                jobId);
            return null;
        }
        if (acquired.Status is not (RawExportJobLeaseStatus.Acquired
            or RawExportJobLeaseStatus.AcquiredAfterRetryableFailure
            or RawExportJobLeaseStatus.Reclaimed)
            || acquired.AttemptId is null || acquired.Revision is null || acquired.FencingToken is null)
            return null;
        acquiredActor = actor;
        return new RawExportAssemblyExecutionRequest(
            jobId, acquired.AttemptId.Value, acquired.Revision.Value,
            acquired.FencingToken.Value, actor.PrincipalId, worker.Value);
    }

    private async Task<RawExportAssemblyExecutionRequest?> TryAcquireRecoveryAsync(
        CancellationToken cancellationToken)
    {
        var recovery = await assemblies.ClaimNextPostSealRecoveryAsync(
            worker.Value, RecoveryLeaseSeconds, cancellationToken).ConfigureAwait(false);
        if (recovery is null) return null;
        if (recovery.Outcome != "Claimed" || recovery.JobId is null || recovery.AttemptId is null
            || recovery.JobRevision is null || recovery.FencingToken is null
            || recovery.ActorPrincipalId is null || recovery.ClaimGeneration is null)
            throw new InvalidOperationException("RAW_EXPORT_POST_SEAL_RECOVERY_CLAIM_SHAPE_INVALID");
        return new RawExportAssemblyExecutionRequest(
            recovery.JobId.Value,
            recovery.AttemptId.Value,
            checked(recovery.JobRevision.Value - 1),
            recovery.FencingToken.Value,
            recovery.ActorPrincipalId.Value,
            worker.Value,
            recovery.ClaimGeneration.Value);
    }

    public async ValueTask RecordAsync(
        RawExportAssemblyExecutionRequest request,
        RawExportAssemblyExecutionResult result,
        CancellationToken cancellationToken = default)
    {
        var actor = acquiredActor;
        acquiredActor = null;
        if (result.RecoveryClaimGeneration is long recoveryGeneration
            && result.C2PreparationId is Guid preparationId)
        {
            if (result.Outcome is RawExportAssemblyExecutionOutcome.Sealed
                or RawExportAssemblyExecutionOutcome.ExistingMatch)
                return;
            var deferred = await assemblies.DeferPostSealRecoveryAsync(
                preparationId,
                request.RecoveryClaimOwnerId == Guid.Empty ? worker.Value : request.RecoveryClaimOwnerId,
                recoveryGeneration,
                result.Outcome.ToString(),
                RecoveryRetryBaseSeconds,
                cancellationToken).ConfigureAwait(false);
            logger.LogWarning(
                "RAW_EXPORT_ASSEMBLY_POST_SEAL_RECOVERY_DEFERRED FaultStage=RecordAsync JobId={JobId} Outcome={Outcome} DeferOutcome={DeferOutcome} Generation={Generation}",
                request.JobId, result.Outcome, deferred.Outcome, recoveryGeneration);
            if (deferred.Outcome is not ("Deferred" or "Completed" or "ClaimLost"))
                throw new InvalidOperationException("RAW_EXPORT_POST_SEAL_RECOVERY_DEFER_INVALID_RESULT");
            return;
        }
        if (actor is null || result.Outcome is RawExportAssemblyExecutionOutcome.Sealed
            or RawExportAssemblyExecutionOutcome.ExistingMatch
            or RawExportAssemblyExecutionOutcome.LeaseLost
            or RawExportAssemblyExecutionOutcome.Expired)
            return;

        var revision = result.JobRevision ?? request.ExpectedJobRevision;
        if (result.Outcome is RawExportAssemblyExecutionOutcome.ProviderUnavailable
            or RawExportAssemblyExecutionOutcome.ProviderOutcomeUnknown
            or RawExportAssemblyExecutionOutcome.SourceUnavailable
            or RawExportAssemblyExecutionOutcome.VerificationIndeterminate)
        {
            await jobs.RecordAttemptFailureAsync(
                new RecordRawExportJobAttemptFailureCommand(
                    actor, request.JobId, revision, request.ExpectedFence,
                    request.AttemptId, worker.Value,
                    RawExportJobAttemptFailureCode.ATTEMPT_EXECUTION_FAILED_RETRYABLE),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var reason = result.Outcome == RawExportAssemblyExecutionOutcome.AuthorityInvalid
            ? RawExportJobTerminalReasonCode.AUTHORITY_REVALIDATION_FAILED
            : result.Outcome is RawExportAssemblyExecutionOutcome.BindingConflict
                or RawExportAssemblyExecutionOutcome.StateConflict
                or RawExportAssemblyExecutionOutcome.AssemblyClassSetMismatch
                ? RawExportJobTerminalReasonCode.JOB_GRAPH_INVARIANT_FAILURE
                : RawExportJobTerminalReasonCode.ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE;
        await jobs.TerminalizeAsync(
            new TerminalizeRawExportJobCommand(
                actor, request.JobId, revision, request.ExpectedFence,
                request.AttemptId, worker.Value, RawExportJobState.TerminalFailed, reason),
            cancellationToken).ConfigureAwait(false);
    }
}
