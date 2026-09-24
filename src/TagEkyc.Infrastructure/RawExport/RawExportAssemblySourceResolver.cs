using System.Security.Cryptography;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

internal enum RawExportAssemblySourceDisposition
{
    Verified,
    KeyAccessIndeterminate,
    ObjectReadIndeterminate,
    DeterministicCiphertextInvalid,
    HistoricCommitmentMismatch,
    CallerCancelled,
}

internal sealed record ResolvedAssemblySource(
    RawExportAssemblyItemDescriptor Descriptor,
    Func<Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask>, CancellationToken, Task<RawExportAssemblySourceDisposition>> ReadVerifiedChunksAsync);

internal sealed class RawExportAssemblySourceResolver(
    RawExportAssemblyRepository repository,
    IProvisionalObjectReconciler objectReconciler,
    RawExportFramedSourceVerificationService framedVerification)
{
    internal async Task<ResolvedAssemblySource?> ResolveAsync(
        RawExportAssemblyExecutionRequest request,
        int ordinal,
        CancellationToken cancellationToken)
    {
        var context = await repository.ReadVerificationContextAsync(request, ordinal, cancellationToken).ConfigureAwait(false);
        if (context is null || context.Object.State != "VerifiedCompleted"
            || context.Object.CiphertextLength is null || context.Object.CiphertextDigest is null)
            return null;

        var descriptor = new RawExportAssemblyItemDescriptor(
            ordinal,
            context.Verification.RawClass,
            context.Verification.SourceArtifactId,
            context.Verification.CaptureArtifactId,
            context.Verification.CaptureRevision,
            context.Verification.MediaType,
            context.Verification.ClaimedPlaintextLength,
            context.Verification.ContentCommitmentSchemaVersion,
            context.Verification.ContentCommitmentKeyId,
            context.Verification.ContentCommitmentKeyVersion,
            context.Verification.ContentCommitment.ToArray());

        return new(
            descriptor,
            (consumer, token) => ReadAsync(request, ordinal, context.BindingFingerprint, consumer, token));
    }

    private async Task<RawExportAssemblySourceDisposition> ReadAsync(
        RawExportAssemblyExecutionRequest request,
        int ordinal,
        byte[] frozenBindingFingerprint,
        Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> plaintextConsumer,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = await repository.ReadVerificationContextAsync(request, ordinal, cancellationToken).ConfigureAwait(false);
            if (context is null || !Fixed(frozenBindingFingerprint, context.BindingFingerprint)
                || context.Object.State != "VerifiedCompleted"
                || context.Object.CiphertextLength is null || context.Object.CiphertextDigest is null)
                return RawExportAssemblySourceDisposition.ObjectReadIndeterminate;

            await using var exact = await objectReconciler.OpenExactReadAsync(
                new(context.Object.ProvisionalObjectIdentity, context.Object.ObjectKey, context.Object.ObjectBindingDigest),
                cancellationToken).ConfigureAwait(false);
            if (exact.CiphertextLength != context.Object.CiphertextLength)
                return await CompleteDispositionAsync(
                    request, ordinal, RawExportAssemblySourceDisposition.DeterministicCiphertextInvalid,
                    cancellationToken).ConfigureAwait(false);

            var result = await framedVerification.VerifyAsync(
                exact.Ciphertext,
                exact.CiphertextLength,
                context.Verification,
                context.Object,
                plaintextConsumer,
                cancellationToken).ConfigureAwait(false);
            try
            {
                var disposition = Fixed(result.ContentCommitment, context.Verification.ContentCommitment)
                    ? RawExportAssemblySourceDisposition.Verified
                    : RawExportAssemblySourceDisposition.HistoricCommitmentMismatch;
                return await CompleteDispositionAsync(
                    request, ordinal, disposition, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(result.ContentCommitment);
                CryptographicOperations.ZeroMemory(result.CiphertextDigest);
                CryptographicOperations.ZeroMemory(result.VerificationEvidenceDigest);
            }
        }
        catch (Exception exception)
        {
            var disposition = ClassifyVerificationFailure(
                exception, cancellationToken.IsCancellationRequested);
            return await CompleteDispositionAsync(
                request, ordinal, disposition, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<RawExportAssemblySourceDisposition> CompleteDispositionAsync(
        RawExportAssemblyExecutionRequest request,
        int ordinal,
        RawExportAssemblySourceDisposition disposition,
        CancellationToken cancellationToken)
    {
        if (disposition is not (RawExportAssemblySourceDisposition.DeterministicCiphertextInvalid
            or RawExportAssemblySourceDisposition.HistoricCommitmentMismatch))
            return disposition;
        try
        {
            var recorded = await repository.RecordSourceIntegrityFailureAsync(
                request, ordinal, disposition, cancellationToken).ConfigureAwait(false);
            return recorded.Outcome == "Recorded" && recorded.EvidenceDigest is { Length: 32 }
                ? disposition
                : RawExportAssemblySourceDisposition.ObjectReadIndeterminate;
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return RawExportAssemblySourceDisposition.ObjectReadIndeterminate;
        }
    }

    internal static RawExportAssemblySourceDisposition ClassifyVerificationFailure(
        Exception exception,
        bool callerCancellationRequested) => exception switch
    {
        AttemptAeadKeyAccessIndeterminateException or RawExportContentCommitmentProviderUnavailableException
            => RawExportAssemblySourceDisposition.KeyAccessIndeterminate,
        OperationCanceledException when callerCancellationRequested
            => RawExportAssemblySourceDisposition.CallerCancelled,
        RawExportR2DeterministicInvalidException deterministic
            when deterministic.Code == "RAW_EXPORT_R2_CONTENT_COMMITMENT_MISMATCH"
            => RawExportAssemblySourceDisposition.HistoricCommitmentMismatch,
        RawExportR2DeterministicInvalidException
            => RawExportAssemblySourceDisposition.DeterministicCiphertextInvalid,
        _ => RawExportAssemblySourceDisposition.ObjectReadIndeterminate,
    };

    private static bool Fixed(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) =>
        left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
}
