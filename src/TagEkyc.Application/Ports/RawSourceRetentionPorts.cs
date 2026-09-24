using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Application.Ports;

public interface IRawSourceConsentService
{
    Task<SessionOperationResult<E01BoundResponse>> RecordAsync(
        AuthenticatedClientContext actor, Guid sessionId, E01RecordRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody, CancellationToken cancellationToken = default);
    Task<SessionOperationResult<E01WithdrawnResponse>> WithdrawAsync(
        AuthenticatedClientContext actor, Guid referenceId, E01WithdrawRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody, CancellationToken cancellationToken = default);
}

public sealed record RawSourceRecordCommand(
    Guid PrincipalId, Guid ClientApplicationId, Guid VerificationSessionId,
    E01RecordRequest Request, Guid IdempotencyKey, byte[] RequestFingerprint);
public sealed record RawSourceWithdrawCommand(
    Guid PrincipalId, Guid ClientApplicationId, Guid ConsentReferenceId,
    E01WithdrawRequest Request, Guid IdempotencyKey, byte[] RequestFingerprint);
public sealed record RawSourceRecordResult(
    string ResultCode, Guid? ConsentReferenceId, long? ConsentReferenceRevision, Guid? ConsentBindingId);
public sealed record RawSourceWithdrawResult(
    string ResultCode, Guid? ConsentReferenceId, long? ConsentReferenceRevision);

// One invocation owns one atomic E01 boundary, including actor context and commit.
public interface IRawSourceRetentionGateway
{
    Task<RawSourceRecordResult> RecordAsync(RawSourceRecordCommand command, CancellationToken cancellationToken);
    Task<RawSourceWithdrawResult> WithdrawAsync(RawSourceWithdrawCommand command, CancellationToken cancellationToken);
}
