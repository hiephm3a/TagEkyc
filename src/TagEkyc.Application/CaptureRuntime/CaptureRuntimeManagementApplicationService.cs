using System.Security.Cryptography;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Application.CaptureRuntime;

public sealed class CaptureRuntimeManagementApplicationService(
    ICaptureRuntimeManagementGateway gateway, ICaptureRuntimeVerifierPepperSource peppers)
    : ICaptureRuntimeManagementService
{
    public Task<SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>> RevokeBootstrapAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeBootstrapRevokeRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403));
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || !(request.BootstrapIssuanceId != Guid.Empty && request.ExpectedRevision > 0 && request.Reason == "OperatorRevocation"))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400));
        var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R04", "/api/ekyc/operator/capture-runtimes/bootstrap-issuances/revoke",
            exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
        return gateway.RevokeBootstrapAsync(actor, request, idempotencyKey, fingerprint, DateTimeOffset.UtcNow, cancellationToken);
    }

    public Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> SuspendRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403));
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || !(request.CaptureAgentId != Guid.Empty && request.ExpectedRevision > 0 && request.Reason == "OperatorSuspension"))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400));
        var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R06", "/api/ekyc/operator/capture-runtimes/suspend",
            exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
        return gateway.SuspendRuntimeAsync(actor, request, idempotencyKey, fingerprint, DateTimeOffset.UtcNow, cancellationToken);
    }

    public Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> ReactivateRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403));
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || !(request.CaptureAgentId != Guid.Empty && request.ExpectedRevision > 0 && request.Reason == "OperatorReactivation"))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400));
        var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R07", "/api/ekyc/operator/capture-runtimes/reactivate",
            exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
        return gateway.ReactivateRuntimeAsync(actor, request, idempotencyKey, fingerprint, DateTimeOffset.UtcNow, cancellationToken);
    }

    public Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> RevokeRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403));
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || !(request.CaptureAgentId != Guid.Empty && request.ExpectedRevision > 0 && request.Reason == "OperatorRevocation"))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400));
        var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R08", "/api/ekyc/operator/capture-runtimes/revoke",
            exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
        return gateway.RevokeRuntimeAsync(actor, request, idempotencyKey, fingerprint, DateTimeOffset.UtcNow, cancellationToken);
    }

    public Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> RetireRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403));
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || !(request.CaptureAgentId != Guid.Empty && request.ExpectedRevision > 0 && request.Reason == "OperatorRetirement"))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400));
        var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R09", "/api/ekyc/operator/capture-runtimes/retire",
            exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
        return gateway.RetireRuntimeAsync(actor, request, idempotencyKey, fingerprint, DateTimeOffset.UtcNow, cancellationToken);
    }

    public Task<SessionOperationResult<CaptureRuntimeCredentialResponse>> RevokeCredentialAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeCredentialRevokeRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeCredentialResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403));
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || !(request.CaptureAgentId != Guid.Empty && request.InstallationId != Guid.Empty && request.CredentialId != Guid.Empty && request.Generation > 0 && request.ExpectedCredentialRevision > 0 && request.Reason == "CredentialCompromise"))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeCredentialResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400));
        var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R10", "/api/ekyc/operator/capture-runtimes/credentials/revoke",
            exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
        return gateway.RevokeCredentialAsync(actor, request, idempotencyKey, fingerprint, DateTimeOffset.UtcNow, cancellationToken);
    }

    public Task<SessionOperationResult<CaptureRuntimeRotationResponse>> AuthorizeRotationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRotationAuthorizeRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeRotationResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403));
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || !(request.CaptureAgentId != Guid.Empty && request.InstallationId != Guid.Empty && request.CredentialId != Guid.Empty && request.CurrentGeneration > 0 && request.ExpectedCredentialRevision > 0))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeRotationResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400));
        var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R11", "/api/ekyc/operator/capture-runtimes/credential-rotations/authorize",
            exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
        return gateway.AuthorizeRotationAsync(actor, request, idempotencyKey, fingerprint, DateTimeOffset.UtcNow, cancellationToken);
    }

    public Task<SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>> RevokeRotationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRotationRevokeRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403));
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || !(request.RotationId != Guid.Empty && request.ExpectedRevision > 0 && request.Reason == "OperatorRevocation"))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400));
        var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R12", "/api/ekyc/operator/capture-runtimes/credential-rotations/revoke",
            exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
        return gateway.RevokeRotationAsync(actor, request, idempotencyKey, fingerprint, DateTimeOffset.UtcNow, cancellationToken);
    }

    public Task<SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>> AssignRolePolicyAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRolePolicyAssignmentRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403));
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || !(request.AgentId != Guid.Empty && request.RolePolicyId != Guid.Empty && request.RolePolicyRevision > 0 && request.ExpectedRuntimeRevision > 0))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400));
        var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R14", "/api/ekyc/operator/capture-runtimes/role-policies/assign",
            exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
        return gateway.AssignRolePolicyAsync(actor, request, idempotencyKey, fingerprint, DateTimeOffset.UtcNow, cancellationToken);
    }

    public Task<SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>> AssignConfigurationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeConfigurationAssignmentRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403));
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || !(request.AgentId != Guid.Empty && request.ConfigurationId != Guid.Empty && request.ConfigurationRevision > 0 && request.ExpectedRuntimeRevision > 0 && request.OverrideId != Guid.Empty))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400));
        var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R15", "/api/ekyc/operator/capture-runtimes/configurations/assign",
            exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
        return gateway.AssignConfigurationAsync(actor, request, idempotencyKey, fingerprint, DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task<SessionOperationResult<CaptureRuntimeBootstrapIssueResponse>> IssueBootstrapAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeBootstrapIssueRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor)) return SessionOperationResult<CaptureRuntimeBootstrapIssueResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403);
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || request.RuntimeType != "Managed" || request.TrustProfileId == Guid.Empty
            || request.RolePolicyId == Guid.Empty || request.ConfigurationId == Guid.Empty
            || request.TrustProfileRevision <= 0 || request.RolePolicyRevision <= 0
            || request.ConfigurationRevision <= 0 || request.HandoffAttestationDigest is null
            || request.HandoffAttestationDigest.Length != 64
            || request.HandoffAttestationDigest.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            return SessionOperationResult<CaptureRuntimeBootstrapIssueResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400);
        var version = peppers.CurrentVersion;
        using var lease = await peppers.TryResolveAsync(version, CaptureRuntimeVerifierPepperDomain.BootstrapDigest, cancellationToken);
        if (version <= 0 || lease is null || lease.Version != version || lease.Domain != CaptureRuntimeVerifierPepperDomain.BootstrapDigest || lease.Key.Length != 32)
            return SessionOperationResult<CaptureRuntimeBootstrapIssueResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        var secret = RandomNumberGenerator.GetBytes(32);
        byte[]? digest = null;
        try
        {
            digest = CaptureRuntimeVerifierCryptography.ComputeDigest(lease.Key.Span, CaptureRuntimeVerifierPepperDomain.BootstrapDigest, secret);
            var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R03", "/api/ekyc/operator/capture-runtimes/bootstrap-issuances",
                exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
            var canonicalSecret = new char[43];
            CaptureRuntimeVerifierCryptography.EncodeCanonicalSecret(secret, canonicalSecret);
            var prefix = new string(canonicalSecret, 0, 12);
            Array.Clear(canonicalSecret);
            var result = await gateway.IssueBootstrapAsync(actor, request, idempotencyKey, fingerprint, prefix, digest, version, DateTimeOffset.UtcNow, cancellationToken);
            if (!result.IsSuccess) return SessionOperationResult<CaptureRuntimeBootstrapIssueResponse>.Failure(result.Error!.Code, result.Error.Message, result.Error.StatusCode);
            var value = result.Value;
            if (value is null || !value.SecretAvailable || value.BootstrapIssuanceId == Guid.Empty || value.Revision <= 0)
                return SessionOperationResult<CaptureRuntimeBootstrapIssueResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            return SessionOperationResult<CaptureRuntimeBootstrapIssueResponse>.Success(new(
                value.BootstrapIssuanceId, CaptureRuntimeVerifierCryptography.EncodeCanonicalSecret(secret), value.ExpiresAtUtc, value.Revision));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
            if (digest is not null) CryptographicOperations.ZeroMemory(digest);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeReadinessResponse>> ReadReadinessAsync(
        AuthenticatedPlatformOperatorContext actor, Guid captureAgentId,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor)) return SessionOperationResult<CaptureRuntimeReadinessResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403);
        if (captureAgentId == Guid.Empty) return SessionOperationResult<CaptureRuntimeReadinessResponse>.Failure(CaptureRuntimeErrorCodes.ResourceNotAvailable, "Resource not available.", 404);
        var result = await gateway.ReadReadinessAsync(actor, captureAgentId, DateTimeOffset.UtcNow, cancellationToken);
        if (!result.IsSuccess) return result;
        var value = result.Value!;
        var versions = value.RequiredPepperVersions.Append(peppers.CurrentVersion).Distinct().ToArray();
        if (versions.Any(v => v <= 0))
            return SessionOperationResult<CaptureRuntimeReadinessResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        foreach (var version in versions)
        foreach (var domain in new[] { CaptureRuntimeVerifierPepperDomain.PlatformCredential,
            CaptureRuntimeVerifierPepperDomain.BootstrapDigest, CaptureRuntimeVerifierPepperDomain.CapabilityDigest })
        {
            using var lease = await peppers.TryResolveAsync(version, domain, cancellationToken);
            if (lease is null || lease.Version != version || lease.Domain != domain || lease.Key.Length != 32)
                return SessionOperationResult<CaptureRuntimeReadinessResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
        var ready = value.DatabaseReady && value.NonceStoreReady && value.CutoverSentinelReady
            && value.RuntimeState == "Active" && value.InstallationState == "Active" && value.CredentialState == "Active";
        return SessionOperationResult<CaptureRuntimeReadinessResponse>.Success(value with { PlatformPepperReady = true, IsReady = ready });
    }

    private static bool Authorized(AuthenticatedPlatformOperatorContext actor) =>
        actor.CredentialId != Guid.Empty && actor.PrincipalId != Guid.Empty
        && actor.CallerCategory == "OperatorAdmin"
        && actor.Scopes.SetEquals(new[] { "operator.capture-runtime.manage" });
}
