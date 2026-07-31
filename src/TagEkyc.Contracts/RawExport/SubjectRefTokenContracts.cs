using System.Text.RegularExpressions;

namespace TagEkyc.Contracts.RawExport;

public sealed class SubjectTokenKeySelector :
    IEquatable<SubjectTokenKeySelector>
{
    private static readonly Regex KeyIdGrammar = new(
        "^[A-Za-z0-9][A-Za-z0-9._:-]{0,255}$",
        RegexOptions.CultureInvariant);

    public SubjectTokenKeySelector(string keyId, int keyVersion)
    {
        if (keyId is null)
        {
            throw new ArgumentNullException(nameof(keyId));
        }

        if (!KeyIdGrammar.IsMatch(keyId))
        {
            throw new ArgumentException(
                "Subject-token key identifier is invalid.",
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

    public bool Equals(SubjectTokenKeySelector? other) =>
        other is not null
        && KeyVersion == other.KeyVersion
        && StringComparer.Ordinal.Equals(KeyId, other.KeyId);

    public override bool Equals(object? obj) =>
        obj is SubjectTokenKeySelector other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(
            StringComparer.Ordinal.GetHashCode(KeyId),
            KeyVersion);

    public override string ToString() =>
        $"SubjectTokenKeySelector:{KeyId}:v{KeyVersion}";
}

public enum SubjectRefTokenFailure
{
    ProviderFailure,
}

public sealed class SubjectRefTokenResult
{
    public const int TokenLength = 32;

    private readonly byte[]? _token;

    private SubjectRefTokenResult(
        byte[]? token,
        SubjectRefTokenFailure? failure)
    {
        _token = token;
        Failure = failure;
    }

    public bool IsSuccess => _token is not null;

    public SubjectRefTokenFailure? Failure { get; }

    public ReadOnlyMemory<byte> Token =>
        _token is not null
            ? _token.ToArray()
            : throw new InvalidOperationException(
                "A failed subject-ref-token result has no token.");

    public static SubjectRefTokenResult Success(
        ReadOnlySpan<byte> token)
    {
        if (token.Length != TokenLength)
        {
            throw new ArgumentException(
                "Subject-ref token must be exactly 32 bytes.",
                nameof(token));
        }

        return new SubjectRefTokenResult(token.ToArray(), null);
    }

    public static SubjectRefTokenResult Failed(
        SubjectRefTokenFailure failure)
    {
        if (failure != SubjectRefTokenFailure.ProviderFailure)
        {
            throw new ArgumentOutOfRangeException(nameof(failure));
        }

        return new SubjectRefTokenResult(null, failure);
    }

    public override string ToString() =>
        IsSuccess
            ? "SubjectRefTokenResult:Success:<redacted>"
            : $"SubjectRefTokenResult:Failed:{Failure}";
}

public interface ISubjectRefTokenService
{
    ValueTask<SubjectRefTokenResult> ComputeAsync(
        SubjectTokenKeySelector selector,
        ReadOnlyMemory<byte> lpPayload,
        CancellationToken cancellationToken);
}
