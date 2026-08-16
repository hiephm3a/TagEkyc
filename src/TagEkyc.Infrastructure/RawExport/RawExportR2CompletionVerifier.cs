using System.Security.Cryptography;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RawExportR2CompletionVerifier(
    RawExportR2Repository repository,
    IProvisionalObjectReconciler objectReconciler,
    TagEkyc.Contracts.RawExport.IAttemptAeadVerificationOperation verification,
    TagEkyc.Contracts.RawExport.IContentCommitmentService contentCommitments)
{
    private readonly RawExportFramedSourceVerificationService framedVerification =
        new(verification, contentCommitments);

    internal async Task<RawExportR2VerifierResult> ExecuteAsync(
        RawExportR2VerificationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ActorPrincipalId == Guid.Empty
            || request.AttemptId == Guid.Empty
            || request.ObjectCustodyId == Guid.Empty
            || request.ExpectedEncryptionAttemptRevision < 1
            || request.ExpectedFence < 1)
            return Failure(request, RawExportR2VerificationDisposition.VerificationIndeterminateRetry);

        RawExportR2ObjectContext? objectContext;
        RawExportR2VerificationContext? context;
        try
        {
            await repository.SetActorAsync(request.ActorPrincipalId, cancellationToken).ConfigureAwait(false);
            objectContext = await repository.ReadObjectContextAsync(
                request.ObjectCustodyId,
                cancellationToken).ConfigureAwait(false);
            context = await repository.ReadVerificationContextAsync(
                request.AttemptId,
                request.ExpectedEncryptionAttemptRevision,
                request.ExpectedFence,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Failure(request, RawExportR2VerificationDisposition.VerificationIndeterminateRetry);
        }
        catch
        {
            return Failure(request, RawExportR2VerificationDisposition.VerificationIndeterminateRetry);
        }

        if (objectContext is null || context is null || !BindingsMatch(request, context, objectContext))
            return Failure(request, RawExportR2VerificationDisposition.VerificationIndeterminateRetry);
        if (objectContext.State != "ObjectPresentPendingVerification"
            || objectContext.CiphertextLength is null
            || objectContext.CiphertextDigest is null)
            return Failure(request, RawExportR2VerificationDisposition.VerificationIndeterminateRetry);

        try
        {
            await using var exact = await objectReconciler.OpenExactReadAsync(
                new(
                    objectContext.ProvisionalObjectIdentity,
                    objectContext.ObjectKey,
                    objectContext.ObjectBindingDigest),
                cancellationToken).ConfigureAwait(false);
            if (exact.CiphertextLength != objectContext.CiphertextLength)
                throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_OBJECT_LENGTH_INVALID");

            var verified = await framedVerification.VerifyAsync(
                exact.Ciphertext,
                exact.CiphertextLength,
                context,
                objectContext,
                plaintextConsumer: null,
                cancellationToken).ConfigureAwait(false);

            ProvisionalObjectMutationResult transition;
            try
            {
                transition = await repository.MarkVerifiedAsync(
                    objectContext.ObjectCustodyId,
                    objectContext.StateRevision,
                    verified.VerificationEvidenceDigest,
                    cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                Zero(verified);
                return Failure(request, RawExportR2VerificationDisposition.VerificationIndeterminateRetry);
            }

            if (transition.OutcomeCode is not ("Verified" or "ExistingMatch"))
            {
                Zero(verified);
                return Failure(request, RawExportR2VerificationDisposition.VerificationIndeterminateRetry);
            }

            return new(
                request.AttemptId,
                request.ObjectCustodyId,
                verified.PlaintextLength,
                verified.ContentCommitment,
                verified.CiphertextLength,
                verified.CiphertextDigest,
                verified.VerificationEvidenceDigest,
                RawExportR2VerificationDisposition.Verified);
        }
        catch (AttemptAeadKeyAccessIndeterminateException)
        {
            return Failure(request, RawExportR2VerificationDisposition.VerificationIndeterminateRetry);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Failure(request, RawExportR2VerificationDisposition.VerificationIndeterminateRetry);
        }
        catch (RawExportR2DeterministicInvalidException)
        {
            try
            {
                await repository.MarkVerificationCleanupRequiredAsync(
                    objectContext,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                return Failure(request, RawExportR2VerificationDisposition.VerificationIndeterminateRetry);
            }
            return Failure(request, RawExportR2VerificationDisposition.VerificationFailedRequiresCleanup);
        }
        catch
        {
            return Failure(request, RawExportR2VerificationDisposition.VerificationIndeterminateRetry);
        }
    }

    private static bool BindingsMatch(
        RawExportR2VerificationRequest request,
        RawExportR2VerificationContext context,
        RawExportR2ObjectContext objectContext) =>
        context.AttemptId == request.AttemptId
        && context.EncryptionAttemptRevision == request.ExpectedEncryptionAttemptRevision
        && context.Fence == request.ExpectedFence
        && objectContext.ObjectCustodyId == request.ObjectCustodyId
        && objectContext.AttemptId == request.AttemptId
        && objectContext.AttemptKeyReservationId == context.AttemptKeyReservationId
        && objectContext.SourceArtifactId == context.SourceArtifactId
        && objectContext.ProvisionalObjectIdentity == context.ProvisionalObjectIdentity
        && objectContext.EncryptionAttemptRevision == context.EncryptionAttemptRevision
        && objectContext.AttemptFence == context.Fence
        && Fixed(objectContext.EncryptionAttemptFingerprint, context.EncryptionAttemptFingerprint);

    private static bool Fixed(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) =>
        left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);

    private static RawExportR2VerifierResult Failure(
        RawExportR2VerificationRequest request,
        RawExportR2VerificationDisposition disposition) =>
        RawExportR2VerifierResult.Failure(request.AttemptId, request.ObjectCustodyId, disposition);

    private static void Zero(RawExportFramedSourceVerificationResult set)
    {
        CryptographicOperations.ZeroMemory(set.ContentCommitment);
        CryptographicOperations.ZeroMemory(set.CiphertextDigest);
        CryptographicOperations.ZeroMemory(set.VerificationEvidenceDigest);
    }
}
