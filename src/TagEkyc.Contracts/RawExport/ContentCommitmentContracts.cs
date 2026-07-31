using System.Text.RegularExpressions;

namespace TagEkyc.Contracts.RawExport;

public sealed class CommitmentKeySelector : IEquatable<CommitmentKeySelector>
{
    private static readonly Regex KeyIdGrammar = new(
        "^[A-Za-z0-9][A-Za-z0-9._:-]{0,255}$",
        RegexOptions.CultureInvariant);

    public CommitmentKeySelector(string keyId, int keyVersion)
    {
        if (keyId is null)
        {
            throw new ArgumentNullException(nameof(keyId));
        }

        if (!KeyIdGrammar.IsMatch(keyId))
        {
            throw new ArgumentException(
                "Commitment key identifier is invalid.",
                nameof(keyId));
        }

        if (keyVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(keyVersion));
        }

        KeyId = keyId;
        KeyVersion = keyVersion;
    }

    public string KeyId { get; }

    public int KeyVersion { get; }

    public bool Equals(CommitmentKeySelector? other) =>
        other is not null
        && KeyVersion == other.KeyVersion
        && StringComparer.Ordinal.Equals(KeyId, other.KeyId);

    public override bool Equals(object? obj) =>
        obj is CommitmentKeySelector other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(StringComparer.Ordinal.GetHashCode(KeyId), KeyVersion);

    public override string ToString() =>
        $"CommitmentKeySelector:{KeyId}:v{KeyVersion}";
}

public enum ContentCommitmentFailure
{
    ProviderFailure,
}

public sealed class ContentCommitmentResult
{
    public const int MacLength = 32;

    private readonly byte[]? _mac;

    private ContentCommitmentResult(
        byte[]? mac,
        ContentCommitmentFailure? failure)
    {
        _mac = mac;
        Failure = failure;
    }

    public bool IsSuccess => _mac is not null;

    public ContentCommitmentFailure? Failure { get; }

    public ReadOnlyMemory<byte> Mac =>
        _mac is not null
            ? _mac.ToArray()
            : throw new InvalidOperationException(
                "A failed content-commitment result has no MAC.");

    public static ContentCommitmentResult Success(ReadOnlySpan<byte> mac)
    {
        if (mac.Length != MacLength)
        {
            throw new ArgumentException(
                "Content-commitment MAC must be exactly 32 bytes.",
                nameof(mac));
        }

        return new ContentCommitmentResult(mac.ToArray(), null);
    }

    public static ContentCommitmentResult Failed(
        ContentCommitmentFailure failure)
    {
        if (failure != ContentCommitmentFailure.ProviderFailure)
        {
            throw new ArgumentOutOfRangeException(nameof(failure));
        }

        return new ContentCommitmentResult(null, failure);
    }

    public override string ToString() =>
        IsSuccess
            ? "ContentCommitmentResult:Success:<redacted>"
            : $"ContentCommitmentResult:Failed:{Failure}";
}

public interface IContentCommitmentService
{
    ValueTask<ContentCommitmentResult> ComputeAsync(
        CommitmentKeySelector selector,
        ReadOnlyMemory<byte> lpPayload,
        CancellationToken cancellationToken);
}
