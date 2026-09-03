using System.Security.Cryptography;

namespace TagEkyc.Infrastructure.RawExport;

public enum ProvisionalObjectCapability { Writer, Reconciler, Lifecycle, PostureProbe }
internal enum ConditionalPutOutcome { Created, ConditionalConflict, OutcomeUnknown }
internal enum ExactObjectInspectionOutcome { Present, PositivelyAbsent, Indeterminate, ProviderUnavailable }
internal enum ExactDeleteOutcome { DeletedAcknowledged, OutcomeUnknown, ProviderUnavailable }

internal sealed record ExactObjectLocator(
    Guid ProvisionalObjectIdentity,
    string ObjectKey,
    byte[] ObjectBindingDigest);

internal sealed record ExactWriteRequest(
    ExactObjectLocator Locator,
    Guid PutOperationId,
    long CiphertextLength);

internal sealed record ConditionalPutResult(
    ConditionalPutOutcome Outcome,
    int? StatusCode,
    long BytesConsumed,
    byte[]? CiphertextDigest,
    byte[]? ProviderReceiptDigest);

internal sealed record ExactObjectInspection(
    ExactObjectInspectionOutcome Outcome,
    long? CiphertextLength,
    byte[]? ObjectBindingDigest,
    byte[]? PutOperationDigest,
    int? StatusCode);

internal sealed record ExactObjectRead(Stream Ciphertext, long CiphertextLength) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Ciphertext.DisposeAsync();
}

internal sealed record ExactDeleteResult(ExactDeleteOutcome Outcome, int? StatusCode);

internal sealed record ObjectBucketPosture(
    bool BucketAvailable,
    bool VersioningNeverEnabled,
    bool ObjectLockAbsent,
    bool LifecycleRulesAbsent,
    bool PublicAccessAbsent);

internal interface IProvisionalObjectWriter
{
    Task<ConditionalPutResult> PutIfAbsentAsync(
        ExactWriteRequest request,
        Stream ciphertext,
        CancellationToken cancellationToken);
}

internal interface IProvisionalObjectReconciler
{
    Task<ExactObjectInspection> InspectExactAsync(
        ExactObjectLocator locator,
        CancellationToken cancellationToken);

    Task<ExactObjectRead> OpenExactReadAsync(
        ExactObjectLocator locator,
        CancellationToken cancellationToken);
}

internal interface IProvisionalObjectLifecycle
{
    Task<ExactDeleteResult> DeleteExactAsync(
        ExactObjectLocator locator,
        CancellationToken cancellationToken);
}

internal interface IProvisionalObjectPostureProbe
{
    Task<ObjectBucketPosture> InspectBucketPostureAsync(CancellationToken cancellationToken);
}

internal static class ProvisionalObjectDigests
{
    internal static byte[] PutOperation(Guid operationId) =>
        SHA256.HashData(operationId.ToByteArray(bigEndian: true));
}
