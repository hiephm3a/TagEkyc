using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Application.CaptureRuntime;

public sealed class RawSourceConsentApplicationService(IRawSourceRetentionGateway gateway) : IRawSourceConsentService
{
    public async Task<SessionOperationResult<E01BoundResponse>> RecordAsync(
        AuthenticatedClientContext actor, Guid sessionId, E01RecordRequest request, Guid idempotencyKey,
        ReadOnlyMemory<byte> exactRequestBody, CancellationToken cancellationToken = default)
    {
        if (!Eligible(actor)) return Failure<E01BoundResponse>("ACCESS_DENIED");
        if (sessionId == Guid.Empty || idempotencyKey == Guid.Empty || exactRequestBody.Length is < 1 or > 8192 ||
            request.ExpectedReferenceRevision < 0 ||
            !RawSourceRetentionProfile.TextValid(request.ExternalConsentArtifactRef, 512) ||
            !RawSourceRetentionProfile.TextValid(request.SourceVersion, 128) ||
            !RawSourceRetentionProfile.TextValid(request.ConsentTextVersion, 128) ||
            !RawSourceRetentionProfile.TextValid(request.ConsentTextContentHash, 256) ||
            !TimestampValid(request.ValidFromUtc) || !TimestampValid(request.ValidUntilUtc) ||
            request.ValidFromUtc >= request.ValidUntilUtc)
            return Failure<E01BoundResponse>("REQUEST_INVALID");
        var fingerprint = Fingerprint(actor, "E01-R",
            $"/api/ekyc/verification-sessions/{sessionId:N}/source-consent-reference", idempotencyKey, exactRequestBody);
        var result = await gateway.RecordAsync(new(actor.PrincipalId, actor.ClientApplicationId, sessionId,
            request, idempotencyKey, fingerprint), cancellationToken);
        if (result.ResultCode is "Bound" or "Replay" && result.ConsentReferenceId is { } reference && reference != Guid.Empty &&
            result.ConsentReferenceRevision is > 0 && result.ConsentBindingId is { } binding && binding != Guid.Empty)
            return SessionOperationResult<E01BoundResponse>.Success(new(reference, result.ConsentReferenceRevision.Value, binding),
                result.ResultCode == "Replay");
        if (result.ConsentReferenceId is not null || result.ConsentReferenceRevision is not null || result.ConsentBindingId is not null)
            return Failure<E01BoundResponse>("NOT_READY");
        return Failure<E01BoundResponse>(result.ResultCode);
    }

    public async Task<SessionOperationResult<E01WithdrawnResponse>> WithdrawAsync(
        AuthenticatedClientContext actor, Guid referenceId, E01WithdrawRequest request, Guid idempotencyKey,
        ReadOnlyMemory<byte> exactRequestBody, CancellationToken cancellationToken = default)
    {
        if (!Eligible(actor)) return Failure<E01WithdrawnResponse>("ACCESS_DENIED");
        if (referenceId == Guid.Empty || idempotencyKey == Guid.Empty || exactRequestBody.Length is < 1 or > 8192 ||
            request.ExpectedReferenceRevision <= 0 || !RawSourceRetentionProfile.TextValid(request.SourceVersion, 128) ||
            !RawSourceRetentionProfile.TextValid(request.DecisionRef, 256))
            return Failure<E01WithdrawnResponse>("REQUEST_INVALID");
        var fingerprint = Fingerprint(actor, "E01-W",
            $"/api/ekyc/source-consent-references/{referenceId:N}/withdraw", idempotencyKey, exactRequestBody);
        var result = await gateway.WithdrawAsync(new(actor.PrincipalId, actor.ClientApplicationId, referenceId,
            request, idempotencyKey, fingerprint), cancellationToken);
        if (result.ResultCode is "Withdrawn" or "Replay" && result.ConsentReferenceId == referenceId &&
            result.ConsentReferenceRevision is > 0)
            return SessionOperationResult<E01WithdrawnResponse>.Success(new(referenceId, result.ConsentReferenceRevision.Value, "Withdrawn"),
                result.ResultCode == "Replay");
        if (result.ConsentReferenceId is not null || result.ConsentReferenceRevision is not null)
            return Failure<E01WithdrawnResponse>("NOT_READY");
        return Failure<E01WithdrawnResponse>(result.ResultCode);
    }

    private static bool Eligible(AuthenticatedClientContext actor) =>
        actor.CallerCategory == AuthenticatedCallerCategory.BusinessConsumer &&
        actor.PrincipalId != Guid.Empty && actor.ClientApplicationId != Guid.Empty;
    private static bool TimestampValid(DateTimeOffset value) =>
        value.Offset == TimeSpan.Zero && value.Ticks % 10 == 0 &&
        value > DateTimeOffset.MinValue && value < DateTimeOffset.MaxValue;
    private static byte[] Fingerprint(AuthenticatedClientContext actor, string operation, string route, Guid key,
        ReadOnlyMemory<byte> body)
    {
        byte[] partition = [.. actor.ClientApplicationId.ToByteArray(bigEndian: true), .. actor.PrincipalId.ToByteArray(bigEndian: true)];
        return CaptureRuntimeHttpFingerprint.Compute(operation, route, body, key, partition);
    }
    private static SessionOperationResult<T> Failure<T>(string code) => code switch
    {
        "Denied" or "ACCESS_DENIED" => SessionOperationResult<T>.Failure("ACCESS_DENIED", "Access denied.", 403),
        "Conflict" => SessionOperationResult<T>.Failure("CONFLICT", "Operation conflict.", 409),
        "REQUEST_INVALID" => SessionOperationResult<T>.Failure("REQUEST_INVALID", "Invalid request.", 400),
        _ => SessionOperationResult<T>.Failure("NOT_READY", "Dependency unavailable.", 503)
    };
}
