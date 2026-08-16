using System.Buffers.Binary;
using System.Security.Cryptography;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed record RawExportFramedSourceVerificationResult(
    long PlaintextLength,
    byte[] ContentCommitment,
    long CiphertextLength,
    byte[] CiphertextDigest,
    byte[] VerificationEvidenceDigest);

internal sealed class RawExportContentCommitmentProviderUnavailableException()
    : IOException("RAW_EXPORT_R2_CONTENT_COMMITMENT_PROVIDER_UNAVAILABLE");

internal sealed class RawExportFramedSourceVerificationService(
    IAttemptAeadVerificationOperation verification,
    IContentCommitmentService contentCommitments)
{
    internal async Task<RawExportFramedSourceVerificationResult> VerifyAsync(
        Stream stream,
        long reportedLength,
        RawExportR2VerificationContext context,
        RawExportR2ObjectContext objectContext,
        Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask>? plaintextConsumer,
        CancellationToken cancellationToken)
    {
        var header = CreateHeader(context, objectContext.ObjectBindingDigest);
        var headerBytes = new byte[RawExportR2FrameCodec.HeaderLength(header)];
        using var objectHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var dataHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var plaintextHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[]? headerDigest = null;
        byte[]? chain = null;
        byte[]? dataDigest = null;
        byte[]? plaintextDigest = null;
        byte[]? payload = null;
        byte[]? ciphertextDigest = null;
        byte[]? envelopeDigest = null;
        byte[]? evidence = null;
        try
        {
            await ReadExactAsync(stream, headerBytes, cancellationToken).ConfigureAwait(false);
            objectHash.AppendData(headerBytes);
            RawExportR2FrameCodec.ValidateHeaderBytes(headerBytes, header);
            headerDigest = SHA256.HashData(headerBytes);
            chain = RawExportR2FrameCodec.InitialAuthenticationChunkCommitment(headerDigest);

            var expectedChunkCount = checked((uint)((context.ClaimedPlaintextLength + context.ChunkSize - 1) / context.ChunkSize));
            ulong dataCiphertextLength = 0;
            for (uint ordinal = 0; ordinal < expectedChunkCount; ordinal++)
            {
                var plaintextLength = checked((int)Math.Min(
                    context.ChunkSize,
                    context.ClaimedPlaintextLength - (long)ordinal * context.ChunkSize));
                var frame = new byte[checked(37 + plaintextLength)];
                byte[]? aad = null;
                try
                {
                    await ReadExactAsync(stream, frame, cancellationToken).ConfigureAwait(false);
                    objectHash.AppendData(frame);
                    dataHash.AppendData(frame);
                    dataCiphertextLength = checked(dataCiphertextLength + (ulong)frame.Length);
                    if (frame[0] != RawExportR2FrameCodec.DataFrameType
                        || BinaryPrimitives.ReadUInt32BigEndian(frame.AsSpan(1, 4)) != ordinal
                        || BinaryPrimitives.ReadUInt32BigEndian(frame.AsSpan(5, 4)) != plaintextLength
                        || frame[9] != 0x00)
                        throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_DATA_FRAME_INVALID");

                    aad = RawExportR2FrameCodec.CreateDataAad(
                        context.EncryptionAttemptFingerprint,
                        headerDigest,
                        ordinal,
                        checked((uint)plaintextLength));
                    var decrypted = await verification.DecryptAndVerifyBoundedChunkAsync(
                        new(
                            context.AttemptKeyReservationId,
                            frame.AsMemory(21, plaintextLength),
                            frame.AsMemory(9, 12),
                            aad),
                        frame.AsMemory(21 + plaintextLength, 16),
                        cancellationToken).ConfigureAwait(false);
                    if (decrypted.Outcome == AttemptAeadVerificationOutcome.KeyAccessIndeterminate)
                        throw new AttemptAeadKeyAccessIndeterminateException();
                    if (decrypted.Outcome == AttemptAeadVerificationOutcome.AuthenticationFailed)
                        throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_AUTHENTICATION_FAILED");
                    var plaintext = decrypted.Output
                        ?? throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_AEAD_RESULT_INVALID");
                    try
                    {
                        plaintextHash.AppendData(plaintext);
                        if (plaintextConsumer is not null)
                            await plaintextConsumer(plaintext, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(plaintext);
                    }

                    var next = RawExportR2FrameCodec.NextAuthenticationChunkCommitment(
                        chain,
                        ordinal,
                        frame.AsSpan(21 + plaintextLength, 16));
                    CryptographicOperations.ZeroMemory(chain);
                    chain = next;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(frame);
                    if (aad is not null) CryptographicOperations.ZeroMemory(aad);
                }
            }

            dataDigest = dataHash.GetHashAndReset();
            var finalFrame = new byte[RawExportR2FrameCodec.FinalFrameLength];
            byte[]? finalAad = null;
            try
            {
                await ReadExactAsync(stream, finalFrame, cancellationToken).ConfigureAwait(false);
                objectHash.AppendData(finalFrame);
                if (finalFrame[0] != RawExportR2FrameCodec.FinalFrameType
                    || finalFrame[1] != 0x01
                    || BinaryPrimitives.ReadUInt32BigEndian(finalFrame.AsSpan(13, 4)) != RawExportR2FrameCodec.CompletionPlaintextLength)
                    throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_FINAL_FRAME_INVALID");

                finalAad = RawExportR2FrameCodec.CreateFinalAad(
                    context.EncryptionAttemptFingerprint,
                    headerDigest,
                    expectedChunkCount,
                    checked((ulong)context.ClaimedPlaintextLength));
                var decrypted = await verification.DecryptAndVerifyBoundedChunkAsync(
                    new(
                        context.AttemptKeyReservationId,
                        finalFrame.AsMemory(17, RawExportR2FrameCodec.CompletionPlaintextLength),
                        finalFrame.AsMemory(1, 12),
                        finalAad),
                    finalFrame.AsMemory(17 + RawExportR2FrameCodec.CompletionPlaintextLength, 16),
                    cancellationToken).ConfigureAwait(false);
                if (decrypted.Outcome == AttemptAeadVerificationOutcome.KeyAccessIndeterminate)
                    throw new AttemptAeadKeyAccessIndeterminateException();
                if (decrypted.Outcome == AttemptAeadVerificationOutcome.AuthenticationFailed)
                    throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_AUTHENTICATION_FAILED");
                var completionBytes = decrypted.Output
                    ?? throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_AEAD_RESULT_INVALID");
                RawExportR2Completion completion;
                try
                {
                    completion = RawExportR2FrameCodec.ParseCompletion(completionBytes);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(completionBytes);
                }

                if (completion.ChunkCount != expectedChunkCount
                    || completion.TotalDataPlaintextLength != (ulong)context.ClaimedPlaintextLength
                    || completion.DataCiphertextLength != dataCiphertextLength
                    || !Fixed(completion.DataCiphertextDigest, dataDigest)
                    || !Fixed(completion.ContentCommitment, context.ContentCommitment)
                    || !Fixed(completion.AuthenticationChunkCommitment, chain)
                    || !Fixed(completion.WrappedKeyMetadataDigest, context.WrappedDekMetadataDigest))
                    throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_COMPLETION_BINDING_INVALID");

                envelopeDigest = RawExportR2FrameCodec.ComputeEnvelopeMetadataDigest(
                    context.AttemptId,
                    context.AttemptKeyReservationId,
                    context.KeyProviderId,
                    context.KekId,
                    context.KekVersion,
                    context.KekFingerprint,
                    context.EncryptionSuiteId,
                    context.EncryptionFramingVersion,
                    context.ChunkSize,
                    context.NonceStrategyId,
                    context.FramingParametersDigest,
                    context.WrappedDekMetadataDigest);
                if (!Fixed(completion.EncryptionEnvelopeMetadataDigest, envelopeDigest))
                    throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_ENVELOPE_BINDING_INVALID");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(finalFrame);
                if (finalAad is not null) CryptographicOperations.ZeroMemory(finalAad);
            }

            var extra = new byte[1];
            try
            {
                if (await stream.ReadAsync(extra, cancellationToken).ConfigureAwait(false) != 0)
                    throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_TRAILING_DATA");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(extra);
            }

            ciphertextDigest = objectHash.GetHashAndReset();
            if (reportedLength != objectContext.CiphertextLength
                || reportedLength != RawExportR2FrameCodec.CiphertextLength(context.ClaimedPlaintextLength, header)
                || !Fixed(ciphertextDigest, objectContext.CiphertextDigest!))
                throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_OBJECT_DIGEST_INVALID");

            plaintextDigest = plaintextHash.GetHashAndReset();
            payload = RawExportR2FrameCodec.BuildHistoricCommitmentPayload(
                context.StableDataScopeId,
                context.ControllerIdentity,
                context.VerificationSessionId,
                context.CaptureArtifactId,
                context.CaptureRevision,
                context.RawClass,
                plaintextDigest,
                context.ClaimedPlaintextLength,
                context.MediaType);
            var commitmentResult = await contentCommitments.ComputeAsync(
                new(context.ContentCommitmentKeyId, context.ContentCommitmentKeyVersion),
                payload,
                cancellationToken).ConfigureAwait(false);
            EnsureContentCommitmentAvailable(commitmentResult);
            var recomputed = commitmentResult.Mac.ToArray();
            try
            {
                if (!Fixed(recomputed, context.ContentCommitment))
                    throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_CONTENT_COMMITMENT_MISMATCH");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(recomputed);
            }

            evidence = RawExportR2FrameCodec.ComputeVerificationEvidence(
                context.AttemptId,
                objectContext.ObjectCustodyId,
                context.ProvisionalObjectIdentity,
                objectContext.ObjectBindingDigest,
                context.EncryptionAttemptFingerprint,
                checked((ulong)reportedLength),
                ciphertextDigest,
                headerDigest,
                dataDigest,
                context.ContentCommitment,
                chain,
                envelopeDigest,
                context.WrappedDekMetadataDigest);

            return new(
                context.ClaimedPlaintextLength,
                context.ContentCommitment.ToArray(),
                reportedLength,
                ciphertextDigest.ToArray(),
                evidence.ToArray());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(headerBytes);
            if (headerDigest is not null) CryptographicOperations.ZeroMemory(headerDigest);
            if (chain is not null) CryptographicOperations.ZeroMemory(chain);
            if (dataDigest is not null) CryptographicOperations.ZeroMemory(dataDigest);
            if (plaintextDigest is not null) CryptographicOperations.ZeroMemory(plaintextDigest);
            if (payload is not null) CryptographicOperations.ZeroMemory(payload);
            if (ciphertextDigest is not null) CryptographicOperations.ZeroMemory(ciphertextDigest);
            if (envelopeDigest is not null) CryptographicOperations.ZeroMemory(envelopeDigest);
            if (evidence is not null) CryptographicOperations.ZeroMemory(evidence);
        }
    }

    internal static void EnsureContentCommitmentAvailable(ContentCommitmentResult result)
    {
        if (!result.IsSuccess)
            throw new RawExportContentCommitmentProviderUnavailableException();
    }

    private static async Task ReadExactAsync(Stream stream, Memory<byte> destination, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < destination.Length)
        {
            var read = await stream.ReadAsync(destination[offset..], cancellationToken).ConfigureAwait(false);
            if (read == 0)
                throw new RawExportR2DeterministicInvalidException("RAW_EXPORT_R2_FRAME_TRUNCATED");
            offset += read;
        }
    }

    private static RawExportR2Header CreateHeader(RawExportR2VerificationContext context, byte[] objectBindingDigest) =>
        new(context.AttemptId, context.AttemptKeyReservationId, context.SourceArtifactId,
            context.ProvisionalObjectIdentity, context.EncryptionAttemptFingerprint, objectBindingDigest,
            context.EncryptionSuiteId, context.EncryptionFramingVersion, context.ChunkSize,
            context.NonceStrategyId, context.FramingParametersDigest);

    private static bool Fixed(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) =>
        left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
}

internal sealed class AttemptAeadKeyAccessIndeterminateException : Exception;
