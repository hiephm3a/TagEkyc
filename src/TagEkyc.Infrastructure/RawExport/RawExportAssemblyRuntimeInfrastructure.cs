using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
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

internal sealed record RawExportAssemblyWorkerIdentity(Guid Value);

internal sealed class DurableRawExportAssemblyWorkSource(
    IRawExportAssemblyConnectionFactory connections,
    IRawExportJobRepository jobs,
    RawExportAssemblyWorkerIdentity worker) : IRawExportAssemblyWorkSource
{
    private AuthenticatedRawExportActor? acquiredActor;

    public async ValueTask<RawExportAssemblyExecutionRequest?> TryAcquireAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(
            RawExportAssemblyDatabaseCapability.Resolver, cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM tagekyc.raw_export_next_assembly_candidate()";
        await using var reader = await command.ExecuteReaderAsync(
            System.Data.CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;

        var jobId = reader.GetGuid(reader.GetOrdinal("JobId"));
        var revision = reader.GetInt64(reader.GetOrdinal("Revision"));
        var fence = reader.GetInt64(reader.GetOrdinal("FencingToken"));
        var actor = new AuthenticatedRawExportActor(
            reader.GetGuid(reader.GetOrdinal("PrincipalId")),
            reader.GetGuid(reader.GetOrdinal("ClientApplicationId")),
            reader.GetGuid(reader.GetOrdinal("CreatedByApiKeyId")));
        var acquired = await jobs.AcquireOrReclaimLeaseAsync(
            new AcquireOrReclaimRawExportJobLeaseCommand(
                actor, jobId, revision, fence, worker.Value),
            cancellationToken).ConfigureAwait(false);
        if (acquired.Status is not (RawExportJobLeaseStatus.Acquired
            or RawExportJobLeaseStatus.AcquiredAfterRetryableFailure
            or RawExportJobLeaseStatus.Reclaimed)
            || acquired.AttemptId is null || acquired.Revision is null || acquired.FencingToken is null)
            return null;
        acquiredActor = actor;
        return new RawExportAssemblyExecutionRequest(
            jobId, acquired.AttemptId.Value, acquired.Revision.Value,
            acquired.FencingToken.Value, actor.PrincipalId);
    }

    public async ValueTask RecordAsync(
        RawExportAssemblyExecutionRequest request,
        RawExportAssemblyExecutionResult result,
        CancellationToken cancellationToken = default)
    {
        var actor = acquiredActor;
        acquiredActor = null;
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
