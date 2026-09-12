using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.TrustedAdapter;

namespace TagEkyc.Application.CaptureRuntime;

public sealed class CaptureRuntimeAppendApplicationService(
    IAppendBusinessTransaction transaction,
    ICaptureRuntimeAppendAuthority authority,
    IVerificationSessionRepository sessions,
    ILocalDevClientPolicyProvider policies,
    ICaptureArtifactRepository artifacts,
    IAuthorityNeutralVerificationEvidencePlanner planner,
    IAuthorityNeutralVerificationEvidenceWriter writer) : ICaptureRuntimeAppendGateway
{
    public Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> AppendCaptureArtifactAsync(
        AuthenticatedCaptureRuntimeContext actor, Guid bindingId,
        CaptureRuntimeCaptureArtifactRequest request, Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (bindingId == Guid.Empty || bindingId != request.BindingId ||
            idempotencyKey == Guid.Empty || request.Payload is null)
            return Task.FromResult(Invalid<CaptureArtifactSubmissionResponseDto>());
        return transaction.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var validated = await authority.ValidateCaptureAsync(actor, bindingId, now, ct);
            if (!validated.IsSuccess) return Propagate<CaptureArtifactSubmissionResponseDto>(validated.Error!);
            var verified = validated.Value!;
            if (!ValidAuthority(verified, bindingId)) return Denied<CaptureArtifactSubmissionResponseDto>();
            var session = await sessions.GetAsync(verified.VerificationSessionId, ct);
            if (session is null) return Denied<CaptureArtifactSubmissionResponseDto>();
            var policy = await policies.GetPolicyAsync(session.ClientApplicationId, ct);
            if (policy is null) return NotReady<CaptureArtifactSubmissionResponseDto>();
            var principal = Principal(actor, verified);
            var plan = await planner.PlanCaptureArtifactAsync(principal, session, policy,
                AuthorityNeutralPayloadAdapters.MapRuntimeCapturePayload(request.Payload, principal),
                idempotencyKey.ToString("N"), now, ct);
            if (!plan.IsSuccess) return Propagate<CaptureArtifactSubmissionResponseDto>(plan.Error!);
            return await writer.ApplyCaptureArtifactAsync(principal, plan.Value!, ct);
        }, cancellationToken);
    }

    public Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> AppendEvidenceResultAsync(
        AuthenticatedCaptureRuntimeContext actor, Guid verificationSessionId,
        CaptureRuntimeEvidenceResultRequest request, Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (verificationSessionId == Guid.Empty || request.BindingId == Guid.Empty ||
            idempotencyKey == Guid.Empty || request.Payload is null)
            return Task.FromResult(Invalid<EvidenceResultSubmissionResponseDto>());
        return transaction.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var validated = await authority.ValidateEvidenceAsync(actor, request.BindingId, now, ct);
            if (!validated.IsSuccess) return Propagate<EvidenceResultSubmissionResponseDto>(validated.Error!);
            var verified = validated.Value!;
            // Equality is checked before *any* session, policy, artifact, planner or writer access.
            if (!ValidAuthority(verified, request.BindingId) ||
                verified.VerificationSessionId != verificationSessionId)
                return Denied<EvidenceResultSubmissionResponseDto>();
            var session = await sessions.GetAsync(verified.VerificationSessionId, ct);
            if (session is null) return Denied<EvidenceResultSubmissionResponseDto>();
            var policy = await policies.GetPolicyAsync(session.ClientApplicationId, ct);
            if (policy is null) return NotReady<EvidenceResultSubmissionResponseDto>();
            var principal = Principal(actor, verified);
            var sessionArtifacts = await artifacts.ListBySessionAsync(session.Id, ct);
            var plan = await planner.PlanEvidenceResultAsync(principal, session, policy, sessionArtifacts,
                AuthorityNeutralPayloadAdapters.MapRuntimeEvidencePayload(request.Payload, principal),
                idempotencyKey.ToString("N"), now, ct);
            if (!plan.IsSuccess) return Propagate<EvidenceResultSubmissionResponseDto>(plan.Error!);
            return await writer.ApplyEvidenceResultAsync(principal, plan.Value!, ct);
        }, cancellationToken);
    }

    private static bool ValidAuthority(VerifiedRuntimeAppendAuthority row, Guid bindingId) =>
        row.VerificationSessionId != Guid.Empty && row.BindingId == bindingId &&
        row.CapabilityId != Guid.Empty && row.RolePolicyId != Guid.Empty &&
        row.RolePolicyRevision > 0 && row.RuntimeRevision > 0 && row.InstallationRevision > 0 &&
        row.CredentialRevision > 0 && row.CapabilityRevision > 0;
    private static VerifiedAppendPrincipal.Runtime Principal(
        AuthenticatedCaptureRuntimeContext actor, VerifiedRuntimeAppendAuthority row) =>
        new(actor.CaptureAgentId, actor.DeviceInstallationId, actor.CredentialId,
            actor.CredentialGeneration, row.RolePolicyId, row.RolePolicyRevision, row.BindingId);
    private static SessionOperationResult<T> Propagate<T>(SessionOperationError error) =>
        SessionOperationResult<T>.Failure(error.Code, error.Message, error.StatusCode);
    private static SessionOperationResult<T> Denied<T>() =>
        SessionOperationResult<T>.Failure("ACCESS_DENIED", "Access denied.", 403);
    private static SessionOperationResult<T> Invalid<T>() =>
        SessionOperationResult<T>.Failure("REQUEST_INVALID", "Request is invalid.", 400);
    private static SessionOperationResult<T> NotReady<T>() =>
        SessionOperationResult<T>.Failure("NOT_READY", "Capture Runtime is not ready.", 503);
}
