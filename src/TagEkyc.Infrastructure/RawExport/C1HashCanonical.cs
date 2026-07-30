using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TagEkyc.Infrastructure.RawExport;

public static class C1HashCanonical
{
    public abstract record Component;

    public sealed record Scalar(string Value) : Component;

    public sealed record OrderedArray(IReadOnlyList<IReadOnlyList<string>> Elements) : Component;

    public static byte[] Compute(string domain, params Component[] components)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        ArgumentNullException.ThrowIfNull(components);

        using var stream = new MemoryStream();
        WriteLengthPrefixedText(stream, domain);
        foreach (var component in components)
        {
            switch (component)
            {
                case Scalar scalar:
                    WriteLengthPrefixedText(stream, scalar.Value);
                    break;
                case OrderedArray array:
                    WriteLengthPrefixedText(
                        stream,
                        array.Elements.Count.ToString(CultureInfo.InvariantCulture));
                    foreach (var element in array.Elements)
                    {
                        ArgumentNullException.ThrowIfNull(element);
                        foreach (var field in element)
                        {
                            WriteLengthPrefixedText(stream, field);
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(components),
                        component,
                        "Unknown C1 canonical component.");
            }
        }

        return SHA256.HashData(stream.ToArray());
    }

    public static byte[] ComputeIngressIdentityFingerprint(
        Guid clientApplicationId,
        string producerId,
        string captureAgentInstanceId,
        Guid ingressIdempotencyKey,
        Guid authenticatedPrincipalId,
        Guid verificationSessionId,
        Guid captureAcceptanceId,
        Guid captureArtifactId,
        int captureRevision,
        string rawClass,
        string sessionChallengeHash,
        string authoritySnapshotId)
    {
        if (captureRevision < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(captureRevision));
        }

        return Compute(
            "tip-88c1-ingress-identity-v1",
            new Scalar(CanonicalGuid(clientApplicationId)),
            new Scalar(RequiredText(producerId, nameof(producerId))),
            new Scalar(RequiredText(captureAgentInstanceId, nameof(captureAgentInstanceId))),
            new Scalar(CanonicalGuid(ingressIdempotencyKey)),
            new Scalar(CanonicalGuid(authenticatedPrincipalId)),
            new Scalar(CanonicalGuid(verificationSessionId)),
            new Scalar(CanonicalGuid(captureAcceptanceId)),
            new Scalar(CanonicalGuid(captureArtifactId)),
            new Scalar(captureRevision.ToString(CultureInfo.InvariantCulture)),
            new Scalar(RequiredText(rawClass, nameof(rawClass))),
            new Scalar(RequiredText(sessionChallengeHash, nameof(sessionChallengeHash))),
            new Scalar(RequiredText(authoritySnapshotId, nameof(authoritySnapshotId))));
    }

    public static byte[] ComputeProducerClaimEnvelopeFingerprint(
        ReadOnlySpan<byte> ingressIdentityFingerprint,
        long claimedPlaintextLength,
        string mediaType,
        DateTimeOffset capturedAtUtc,
        DateTimeOffset plaintextRetentionStartedAtUtc,
        DateTimeOffset plaintextRetentionExpiresAtUtc,
        int plaintextRetentionBudgetSeconds)
    {
        if (ingressIdentityFingerprint.Length != 32)
        {
            throw new ArgumentException(
                "Ingress identity fingerprint must be exactly 32 bytes.",
                nameof(ingressIdentityFingerprint));
        }

        if (claimedPlaintextLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(claimedPlaintextLength));
        }

        if (plaintextRetentionBudgetSeconds < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(plaintextRetentionBudgetSeconds));
        }

        return Compute(
            "tip-88c1-producer-claim-envelope-v2",
            new Scalar(Convert.ToHexString(ingressIdentityFingerprint).ToLowerInvariant()),
            new Scalar(claimedPlaintextLength.ToString(CultureInfo.InvariantCulture)),
            new Scalar(RequiredText(mediaType, nameof(mediaType))),
            new Scalar(CanonicalTimestamp(capturedAtUtc)),
            new Scalar(CanonicalTimestamp(plaintextRetentionStartedAtUtc)),
            new Scalar(CanonicalTimestamp(plaintextRetentionExpiresAtUtc)),
            new Scalar(plaintextRetentionBudgetSeconds.ToString(CultureInfo.InvariantCulture)));
    }

    public static string CanonicalTimestamp(DateTimeOffset value)
    {
        var utcTicks = value.UtcTicks;
        var truncatedTicks = utcTicks - (utcTicks % TimeSpan.TicksPerMicrosecond);
        var normalized = new DateTimeOffset(truncatedTicks, TimeSpan.Zero);
        return normalized.ToString(
            "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'",
            CultureInfo.InvariantCulture);
    }

    private static string CanonicalGuid(Guid value) =>
        value.ToString("N");

    private static string RequiredText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static void WriteLengthPrefixedText(Stream stream, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var normalized = value.Normalize(NormalizationForm.FormC);
        var bytes = Encoding.UTF8.GetBytes(normalized);
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, checked((uint)bytes.Length));
        stream.Write(length);
        stream.Write(bytes);
    }
}
