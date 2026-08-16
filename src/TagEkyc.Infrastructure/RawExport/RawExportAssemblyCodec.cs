using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

public sealed record RawExportAssemblyPlaintextItem(
    RawExportAssemblyItemDescriptor Descriptor,
    Func<Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask>, CancellationToken, Task> ReadVerifiedChunksAsync);

public sealed record RawExportAssemblyDerivation(
    Guid AssemblyId,
    byte[] HeaderBytes,
    long CompleteAssemblyLength,
    byte[] AssemblyDigest,
    byte[] ManifestDigest,
    byte[] AssemblyAuthenticationValue,
    byte[] AssemblyFingerprint,
    Guid C2PreparationId,
    byte[] PreparationFingerprint);

public static class RawExportAssemblyCodec
{
    private static readonly byte[] Magic = "TIP-88C1-ASSEMBLY-V1"u8.ToArray();

    public static Guid AssemblyId(Guid jobId) =>
        EvidenceCanonicalization.DeterministicGuid(
            "tip-88c1-assembly-id-v1",
            new { jobId = jobId.ToString("N") });

    public static Guid C2PreparationId(Guid assemblyId, ReadOnlySpan<byte> assemblyFingerprint) =>
        EvidenceCanonicalization.DeterministicGuid(
            "tip-88c1-c2-preparation-id-v1",
            new
            {
                assemblyFingerprint = Hex(assemblyFingerprint),
                assemblyId = assemblyId.ToString("N"),
            });

    public static byte[] SerializeHeader(RawExportAssemblyHeader header)
    {
        ValidateHeader(header);
        var value = new
        {
            assemblyId = header.AssemblyId.ToString("D").ToLowerInvariant(),
            clientApplicationId = header.ClientApplicationId.ToString("D").ToLowerInvariant(),
            createdAtUtc = C1HashCanonical.CanonicalTimestamp(header.CreatedAtUtc),
            exportMode = Nfc(header.ExportMode),
            items = header.Items.OrderBy(item => item.Ordinal).Select(item => new
            {
                ordinal = item.Ordinal,
                rawClass = Nfc(item.RawClass),
                sourceArtifactId = item.SourceArtifactId.ToString("D").ToLowerInvariant(),
                captureArtifactId = item.CaptureArtifactId.ToString("D").ToLowerInvariant(),
                captureRevision = item.CaptureRevision,
                mediaType = Nfc(item.MediaType),
                plaintextLength = item.PlaintextLength,
                contentCommitmentSchemaVersion = item.ContentCommitmentSchemaVersion,
                contentCommitmentKeyId = Nfc(item.ContentCommitmentKeyId),
                contentCommitmentKeyVersion = item.ContentCommitmentKeyVersion,
                contentCommitment = Hex(item.ContentCommitment),
            }).ToArray(),
            jobId = header.JobId.ToString("D").ToLowerInvariant(),
            manifestVersion = header.ManifestVersion,
            permitId = header.PermitId.ToString("D").ToLowerInvariant(),
            policyId = header.PolicyId.ToString("D").ToLowerInvariant(),
            policyVersion = header.PolicyVersion,
            purposeCode = Nfc(header.PurposeCode),
            recipientClientApplicationId = header.RecipientClientApplicationId.ToString("D").ToLowerInvariant(),
            subjectRefToken = Hex(header.SubjectRefToken),
            verificationSessionId = header.VerificationSessionId.ToString("D").ToLowerInvariant(),
        };
        return Encoding.UTF8.GetBytes(EvidenceCanonicalization.Canonicalize(value));
    }

    public static long CompleteLength(int headerLength, IReadOnlyList<RawExportAssemblyItemDescriptor> items) =>
        checked(Magic.Length + 4L + headerLength + items.Sum(item => 12L + item.PlaintextLength));

    public static async Task<byte[]> ComputeAssemblyDigestAsync(
        RawExportAssemblyHeader header,
        IReadOnlyList<RawExportAssemblyPlaintextItem> items,
        CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using var sink = new HashOnlyStream(hash);
        await WriteAssemblyAsync(header, items, sink, cancellationToken).ConfigureAwait(false);
        return hash.GetHashAndReset();
    }

    public static async Task WriteAssemblyAsync(
        RawExportAssemblyHeader header,
        IReadOnlyList<RawExportAssemblyPlaintextItem> items,
        Stream destination,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var ordered = items.OrderBy(item => item.Descriptor.Ordinal).ToArray();
        if (!ordered.Select(item => item.Descriptor).SequenceEqual(header.Items.OrderBy(item => item.Ordinal)))
            throw new InvalidOperationException("RAW_EXPORT_ASSEMBLY_ITEM_SET_INVALID");
        var headerBytes = SerializeHeader(header);
        try
        {
            await destination.WriteAsync(Magic, cancellationToken).ConfigureAwait(false);
            await WriteUInt32Async(destination, checked((uint)headerBytes.Length), cancellationToken).ConfigureAwait(false);
            await destination.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
            foreach (var item in ordered)
            {
                await WriteUInt32Async(destination, checked((uint)item.Descriptor.Ordinal), cancellationToken).ConfigureAwait(false);
                await WriteUInt64Async(destination, checked((ulong)item.Descriptor.PlaintextLength), cancellationToken).ConfigureAwait(false);
                long observed = 0;
                await item.ReadVerifiedChunksAsync(
                    async (chunk, token) =>
                    {
                        observed = checked(observed + chunk.Length);
                        if (observed > item.Descriptor.PlaintextLength)
                            throw new InvalidOperationException("RAW_EXPORT_ASSEMBLY_PLAINTEXT_LENGTH_INVALID");
                        await destination.WriteAsync(chunk, token).ConfigureAwait(false);
                    },
                    cancellationToken).ConfigureAwait(false);
                if (observed != item.Descriptor.PlaintextLength)
                    throw new InvalidOperationException("RAW_EXPORT_ASSEMBLY_PLAINTEXT_LENGTH_INVALID");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(headerBytes);
        }
    }

    public static byte[] ManifestDigest(
        RawExportAssemblyHeader header,
        Guid attemptId,
        long fencingToken,
        DateTimeOffset jobExpiresAtUtc,
        int subjectRefTokenSchemaVersion,
        string subjectRefTokenKeyId,
        int subjectRefTokenKeyVersion,
        string authenticationKeyId,
        int authenticationKeyVersion,
        ReadOnlySpan<byte> assemblyDigest)
    {
        var descriptors = header.Items.OrderBy(item => item.Ordinal)
            .Select(item => (IReadOnlyList<string>)
            [
                item.Ordinal.ToString(CultureInfo.InvariantCulture), item.RawClass,
                item.SourceArtifactId.ToString("N"), item.CaptureArtifactId.ToString("N"),
                item.CaptureRevision.ToString(CultureInfo.InvariantCulture), item.MediaType,
                item.PlaintextLength.ToString(CultureInfo.InvariantCulture),
                item.ContentCommitmentSchemaVersion.ToString(CultureInfo.InvariantCulture),
                item.ContentCommitmentKeyId,
                item.ContentCommitmentKeyVersion.ToString(CultureInfo.InvariantCulture), Hex(item.ContentCommitment),
            ]).ToArray();
        return C1HashCanonical.Compute(
            "tip-88c1-assembly-manifest-v1",
            new C1HashCanonical.Scalar(header.ManifestVersion.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar(header.AssemblyId.ToString("N")),
            new C1HashCanonical.Scalar(header.JobId.ToString("N")),
            new C1HashCanonical.Scalar(attemptId.ToString("N")),
            new C1HashCanonical.Scalar(fencingToken.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar(C1HashCanonical.CanonicalTimestamp(jobExpiresAtUtc)),
            new C1HashCanonical.Scalar(C1HashCanonical.CanonicalTimestamp(header.CreatedAtUtc)),
            new C1HashCanonical.Scalar(subjectRefTokenSchemaVersion.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar(subjectRefTokenKeyId),
            new C1HashCanonical.Scalar(subjectRefTokenKeyVersion.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar(Hex(header.SubjectRefToken)),
            new C1HashCanonical.Scalar(authenticationKeyId),
            new C1HashCanonical.Scalar(authenticationKeyVersion.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar(Hex(assemblyDigest)),
            new C1HashCanonical.OrderedArray(descriptors));
    }

    public static byte[] AssemblyFingerprint(
        Guid assemblyId,
        Guid jobId,
        Guid attemptId,
        long fencingToken,
        ReadOnlySpan<byte> manifestDigest,
        string authenticationKeyId,
        int authenticationKeyVersion,
        ReadOnlySpan<byte> authenticationValue) =>
        C1HashCanonical.Compute(
            "tip-88c1-assembly-fingerprint-v2",
            new C1HashCanonical.Scalar(assemblyId.ToString("N")),
            new C1HashCanonical.Scalar(jobId.ToString("N")),
            new C1HashCanonical.Scalar(attemptId.ToString("N")),
            new C1HashCanonical.Scalar(fencingToken.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar(Hex(manifestDigest)),
            new C1HashCanonical.Scalar(authenticationKeyId),
            new C1HashCanonical.Scalar(authenticationKeyVersion.ToString(CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar(Hex(authenticationValue)));

    public static byte[] PreparationFingerprint(
        Guid c2PreparationId,
        Guid assemblyId,
        ReadOnlySpan<byte> assemblyFingerprint,
        ReadOnlySpan<byte> manifestDigest,
        ReadOnlySpan<byte> assemblyDigest,
        long completeLength) =>
        C1HashCanonical.Compute(
            "tip-88c1-c2-preparation-v1",
            new C1HashCanonical.Scalar(c2PreparationId.ToString("N")),
            new C1HashCanonical.Scalar(assemblyId.ToString("N")),
            new C1HashCanonical.Scalar(Hex(assemblyFingerprint)),
            new C1HashCanonical.Scalar(Hex(manifestDigest)),
            new C1HashCanonical.Scalar(Hex(assemblyDigest)),
            new C1HashCanonical.Scalar(completeLength.ToString(CultureInfo.InvariantCulture)));

    internal static string Hex(ReadOnlySpan<byte> value) => Convert.ToHexString(value).ToLowerInvariant();

    private static string Nfc(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Normalize(NormalizationForm.FormC);
    }

    private static void ValidateHeader(RawExportAssemblyHeader header)
    {
        if (header.AssemblyId == Guid.Empty || header.JobId == Guid.Empty || header.PermitId == Guid.Empty
            || header.VerificationSessionId == Guid.Empty || header.ClientApplicationId == Guid.Empty
            || header.RecipientClientApplicationId == Guid.Empty || header.PolicyId == Guid.Empty
            || header.ManifestVersion != 1 || header.PolicyVersion < 1 || header.SubjectRefToken.Length != 32
            || header.Items.Count == 0)
            throw new ArgumentException("RAW_EXPORT_ASSEMBLY_HEADER_INVALID", nameof(header));
        var expected = 0;
        foreach (var item in header.Items.OrderBy(item => item.Ordinal))
        {
            if (item.Ordinal != expected++ || item.SourceArtifactId == Guid.Empty || item.CaptureArtifactId == Guid.Empty
                || item.CaptureRevision < 1 || item.PlaintextLength < 1 || item.ContentCommitment.Length != 32)
                throw new ArgumentException("RAW_EXPORT_ASSEMBLY_ITEM_INVALID", nameof(header));
        }
    }

    private static async ValueTask WriteUInt32Async(Stream stream, uint value, CancellationToken cancellationToken)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        CryptographicOperations.ZeroMemory(bytes);
    }

    private static async ValueTask WriteUInt64Async(Stream stream, ulong value, CancellationToken cancellationToken)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        CryptographicOperations.ZeroMemory(bytes);
    }

    private sealed class HashOnlyStream(IncrementalHash hash) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override void Write(byte[] buffer, int offset, int count) => hash.AppendData(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => hash.AppendData(buffer);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            hash.AppendData(buffer.Span);
            return ValueTask.CompletedTask;
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
