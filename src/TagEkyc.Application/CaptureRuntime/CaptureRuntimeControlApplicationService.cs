using System.Security.Cryptography;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Application.CaptureRuntime;

public sealed class CaptureRuntimeControlApplicationService(
    ICaptureRuntimeControlGateway gateway)
    : ICaptureRuntimeControlService
{
    public Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishTrustProfileAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeTrustProfilePublicationRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403));
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || !(request.CatalogId != Guid.Empty && request.ExpectedHeadRevision >= 0 && request.RuntimeType == "Managed" && request.ExpiresAtUtc > request.EffectiveAtUtc))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400));
        var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R16", "/api/ekyc/operator/capture-runtime-control/trust-profiles/publish",
            exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
        return gateway.PublishTrustProfileAsync(actor, request, idempotencyKey, fingerprint, DateTimeOffset.UtcNow, cancellationToken);
    }

    public Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishRolePolicyAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRolePolicyPublicationRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403));
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || !(request.CatalogId != Guid.Empty && request.ExpectedHeadRevision >= 0 && ValidRoles(request.Roles)))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400));
        var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R17", "/api/ekyc/operator/capture-runtime-control/role-policies/publish",
            exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
        return gateway.PublishRolePolicyAsync(actor, request, idempotencyKey, fingerprint, DateTimeOffset.UtcNow, cancellationToken);
    }

    public Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishConfigurationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeConfigurationPublicationRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (!Authorized(actor))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403));
        if (request is null || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty
            || !(request.CatalogId != Guid.Empty && request.ExpectedHeadRevision >= 0 && request.ExpiresAtUtc > request.EffectiveAtUtc && ValidConfiguration(request)))
            return Task.FromResult(SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400));
        var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R18", "/api/ekyc/operator/capture-runtime-control/configurations/publish",
            exactRequestBody, idempotencyKey, actor.CredentialId.ToByteArray(bigEndian: true));
        return gateway.PublishConfigurationAsync(actor, request, idempotencyKey, fingerprint, DateTimeOffset.UtcNow, cancellationToken);
    }

    private static bool ValidRoles(IReadOnlyList<string> roles) =>
        roles is not null && roles.Count > 0
        && roles.SequenceEqual(roles.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        && roles.All(r => r is "Bind" or "Configuration" or "CaptureObservation" or "TrustedEvidence" or "RawIngress" or "CredentialRotation");

    private static bool ValidConfiguration(CaptureRuntimeConfigurationPublicationRequest r) =>
        r.PlaintextBudgetSeconds > 0 && r.RawExportSourceClaimSafetyMarginMilliseconds >= 0
        && r.CaptureAgentConfigurationPollingIntervalSeconds > 0
        && r.RawExportSourceMaximumChipDg2PortraitBytes is >= 1 and <= 67108864
        && r.RawExportSourceMaximumLiveSelfieImageBytes is >= 1 and <= 67108864
        && r.RawExportCaptureMaximumAggregatePlaintextBytesPerHost is >= 1 and <= 2147483647
        && r.RawExportCustodyMaximumPlaintextWindowBytesPerStream is >= 1 and <= 16777216
        && r.RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment is >= 1 and <= 2147483647
        && r.RawExportIngressMaximumPreAdmissionBufferedBytes is >= 1 and <= 65536
        && r.CaptureAgentConfigurationPollingIntervalSeconds + r.RawExportSourceClaimSafetyMarginMilliseconds / 1000.0 < (r.ExpiresAtUtc - r.EffectiveAtUtc).TotalSeconds;

    private static bool Authorized(AuthenticatedPlatformOperatorContext actor) =>
        actor.CredentialId != Guid.Empty && actor.PrincipalId != Guid.Empty
        && actor.CallerCategory == "OperatorAdmin"
        && actor.Scopes.SetEquals(new[] { "operator.capture-runtime.manage" });
}
