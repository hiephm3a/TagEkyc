// Copied from SignFlow ProtectedValues (Codex_SignFlow) — TagEkyc-owned fork;
// do not add a SignFlow project reference.
using System.Text.RegularExpressions;

namespace TagEkyc.Infrastructure.ProtectedValues;

internal enum ProtectedValueResolutionState
{
    Found,
    NotFound,
    Rejected,
    ProviderFailure,
}

internal enum ProtectedValueFailureCause
{
    CatalogEntryNotFound,
    ScopeInvalid,
    ProviderUnknown,
    ReferenceInvalid,
    ReferenceMismatch,
    ValueNotFound,
    ValueDisabled,
    ValueNotYetValid,
    ValueExpired,
    ProviderForbidden,
    ProviderAuthenticationFailed,
    ProviderUnavailable,
    MaterialInvalid,
    ConfigurationInvalid,
}

internal sealed class ProtectedValuePurpose :
    IEquatable<ProtectedValuePurpose>
{
    internal static ProtectedValuePurpose ContentCommitmentHmac { get; } =
        new("raw-export.content-commitment.hmac");

    internal static ProtectedValuePurpose SubjectRefTokenHmac { get; } =
        new("raw-export.subject-ref-token.hmac");

    internal static ProtectedValuePurpose PackageReferenceCursorHmac { get; } =
        new("raw-export.package-reference-cursor.hmac");

    private ProtectedValuePurpose(string code)
    {
        Code = code;
    }

    internal string Code { get; }

    public bool Equals(ProtectedValuePurpose? other) =>
        other is not null
        && StringComparer.Ordinal.Equals(Code, other.Code);

    public override bool Equals(object? obj) =>
        obj is ProtectedValuePurpose other && Equals(other);

    public override int GetHashCode() =>
        StringComparer.Ordinal.GetHashCode(Code);

    public override string ToString() => Code;
}

internal sealed class ProtectedValueId : IEquatable<ProtectedValueId>
{
    private static readonly Regex Grammar = new(
        "^[A-Za-z0-9][A-Za-z0-9._:-]{0,255}$",
        RegexOptions.CultureInvariant);

    internal ProtectedValueId(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (value.Any(char.IsControl) || !Grammar.IsMatch(value))
        {
            throw new ArgumentException(
                "Protected value identifier is invalid.",
                nameof(value));
        }

        Value = value;
    }

    internal string Value { get; }

    public bool Equals(ProtectedValueId? other) =>
        other is not null
        && StringComparer.Ordinal.Equals(Value, other.Value);

    public override bool Equals(object? obj) =>
        obj is ProtectedValueId other && Equals(other);

    public override int GetHashCode() =>
        StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;
}

internal sealed class ProtectedValueReference :
    IEquatable<ProtectedValueReference>
{
    private static readonly Regex SchemeGrammar = new(
        "^[a-z][a-z0-9-]{0,31}$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    internal ProtectedValueReference(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var separator = value.IndexOf(':');
        if (separator <= 0 || separator == value.Length - 1)
        {
            throw new ArgumentException(
                "Protected value reference is invalid.",
                nameof(value));
        }

        var scheme = value[..separator];
        var target = value[(separator + 1)..];
        if (!SchemeGrammar.IsMatch(scheme)
            || target.Length > 2048
            || target.Any(char.IsControl))
        {
            throw new ArgumentException(
                "Protected value reference is invalid.",
                nameof(value));
        }

        Scheme = scheme;
        Target = target;
    }

    internal string Scheme { get; }

    internal string Target { get; }

    public bool Equals(ProtectedValueReference? other) =>
        other is not null
        && StringComparer.Ordinal.Equals(Scheme, other.Scheme)
        && StringComparer.Ordinal.Equals(Target, other.Target);

    public override bool Equals(object? obj) =>
        obj is ProtectedValueReference other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(
            StringComparer.Ordinal.GetHashCode(Scheme),
            StringComparer.Ordinal.GetHashCode(Target));

    public override string ToString() => $"{Scheme}:<redacted>";
}

internal sealed record ProtectedValueRequest(
    ProtectedValuePurpose Purpose,
    ProtectedValueId ValueId);

internal sealed record ProtectedValueDescriptor(
    ProtectedValueRequest LogicalIdentity,
    ProtectedValueReference ExactReference);

internal sealed record ProtectedValueTechnicalMetadata(
    string? ResolvedVersion,
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpiresOn,
    string SourceType,
    string RedactedIdentifier,
    string? ProviderCorrelationId);

internal interface IProtectedValueResolver
{
    ValueTask<ProtectedValueResolution> ResolveAsync(
        ProtectedValueRequest request,
        CancellationToken cancellationToken);
}

internal interface IProtectedValueCatalog
{
    ValueTask<ProtectedValueDescriptor?> FindAsync(
        ProtectedValueRequest request,
        CancellationToken cancellationToken);
}

internal interface IProtectedValueProvider
{
    string Scheme { get; }

    ValueTask<ProtectedValueProviderResolution> ResolveAsync(
        ProtectedValueDescriptor descriptor,
        CancellationToken cancellationToken);
}

internal sealed class ProtectedValueProviderResolution
{
    private ProtectedValueProviderResolution(
        ProtectedValueResolutionState state,
        ProtectedValueFailureCause? cause,
        ProtectedValueMaterialLease? materialLease,
        ProtectedValueTechnicalMetadata? metadata)
    {
        State = state;
        Cause = cause;
        MaterialLease = materialLease;
        Metadata = metadata;
    }

    internal ProtectedValueResolutionState State { get; }

    internal ProtectedValueFailureCause? Cause { get; }

    internal ProtectedValueMaterialLease? MaterialLease { get; }

    internal ProtectedValueTechnicalMetadata? Metadata { get; }

    internal static ProtectedValueProviderResolution Found(
        ProtectedValueMaterialLease materialLease,
        ProtectedValueTechnicalMetadata metadata)
    {
        ProtectedValueMetadataValidator.ValidateOrDispose(
            materialLease,
            metadata);
        return new(
            ProtectedValueResolutionState.Found,
            null,
            materialLease,
            metadata);
    }

    internal static ProtectedValueProviderResolution NotFound(
        ProtectedValueFailureCause cause) =>
        CreateFailure(
            ProtectedValueResolutionState.NotFound,
            cause,
            [ProtectedValueFailureCause.ValueNotFound]);

    internal static ProtectedValueProviderResolution Rejected(
        ProtectedValueFailureCause cause) =>
        CreateFailure(
            ProtectedValueResolutionState.Rejected,
            cause,
            [
                ProtectedValueFailureCause.ReferenceInvalid,
                ProtectedValueFailureCause.ReferenceMismatch,
                ProtectedValueFailureCause.ValueDisabled,
                ProtectedValueFailureCause.ValueNotYetValid,
                ProtectedValueFailureCause.ValueExpired,
                ProtectedValueFailureCause.MaterialInvalid,
            ]);

    internal static ProtectedValueProviderResolution ProviderFailure(
        ProtectedValueFailureCause cause) =>
        CreateFailure(
            ProtectedValueResolutionState.ProviderFailure,
            cause,
            [
                ProtectedValueFailureCause.ProviderForbidden,
                ProtectedValueFailureCause.ProviderAuthenticationFailed,
                ProtectedValueFailureCause.ProviderUnavailable,
                ProtectedValueFailureCause.ConfigurationInvalid,
            ]);

    private static ProtectedValueProviderResolution CreateFailure(
        ProtectedValueResolutionState state,
        ProtectedValueFailureCause cause,
        ProtectedValueFailureCause[] allowed)
    {
        if (!allowed.Contains(cause))
        {
            throw new ArgumentOutOfRangeException(nameof(cause));
        }

        return new(state, cause, null, null);
    }
}

internal sealed class ProtectedValueResolution
{
    private ProtectedValueResolution(
        ProtectedValueResolutionState state,
        ProtectedValueFailureCause? cause,
        ProtectedValueMaterialLease? materialLease,
        ProtectedValueTechnicalMetadata? metadata)
    {
        State = state;
        Cause = cause;
        MaterialLease = materialLease;
        Metadata = metadata;
    }

    internal ProtectedValueResolutionState State { get; }

    internal ProtectedValueFailureCause? Cause { get; }

    internal ProtectedValueMaterialLease? MaterialLease { get; }

    internal ProtectedValueTechnicalMetadata? Metadata { get; }

    internal static ProtectedValueResolution Found(
        ProtectedValueMaterialLease materialLease,
        ProtectedValueTechnicalMetadata metadata)
    {
        ProtectedValueMetadataValidator.ValidateOrDispose(
            materialLease,
            metadata);
        return new(
            ProtectedValueResolutionState.Found,
            null,
            materialLease,
            metadata);
    }

    internal static ProtectedValueResolution NotFound(
        ProtectedValueFailureCause cause) =>
        CreateFailure(
            ProtectedValueResolutionState.NotFound,
            cause,
            [
                ProtectedValueFailureCause.CatalogEntryNotFound,
                ProtectedValueFailureCause.ValueNotFound,
            ]);

    internal static ProtectedValueResolution Rejected(
        ProtectedValueFailureCause cause) =>
        CreateFailure(
            ProtectedValueResolutionState.Rejected,
            cause,
            [
                ProtectedValueFailureCause.ScopeInvalid,
                ProtectedValueFailureCause.ReferenceInvalid,
                ProtectedValueFailureCause.ReferenceMismatch,
                ProtectedValueFailureCause.ValueDisabled,
                ProtectedValueFailureCause.ValueNotYetValid,
                ProtectedValueFailureCause.ValueExpired,
                ProtectedValueFailureCause.MaterialInvalid,
            ]);

    internal static ProtectedValueResolution ProviderFailure(
        ProtectedValueFailureCause cause) =>
        CreateFailure(
            ProtectedValueResolutionState.ProviderFailure,
            cause,
            [
                ProtectedValueFailureCause.ProviderUnknown,
                ProtectedValueFailureCause.ProviderForbidden,
                ProtectedValueFailureCause.ProviderAuthenticationFailed,
                ProtectedValueFailureCause.ProviderUnavailable,
                ProtectedValueFailureCause.ConfigurationInvalid,
            ]);

    private static ProtectedValueResolution CreateFailure(
        ProtectedValueResolutionState state,
        ProtectedValueFailureCause cause,
        ProtectedValueFailureCause[] allowed)
    {
        if (!allowed.Contains(cause))
        {
            throw new ArgumentOutOfRangeException(nameof(cause));
        }

        return new(state, cause, null, null);
    }
}

internal static class ProtectedValueMetadataValidator
{
    private static readonly Regex SourceTypeGrammar = new(
        "^[a-z][a-z0-9-]{0,31}$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    internal static void ValidateOrDispose(
        ProtectedValueMaterialLease? materialLease,
        ProtectedValueTechnicalMetadata? metadata)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(materialLease);
            ArgumentNullException.ThrowIfNull(metadata);

            if (!IsValidSourceType(metadata.SourceType)
                || !IsSafeRequired(metadata.RedactedIdentifier, 256)
                || !IsSafeOptional(metadata.ProviderCorrelationId, 256))
            {
                throw new ArgumentException(
                    "Protected value metadata is invalid.",
                    nameof(metadata));
            }
        }
        catch
        {
            materialLease?.Dispose();
            throw;
        }
    }

    private static bool IsValidSourceType(string? value) =>
        value is not null && SourceTypeGrammar.IsMatch(value);

    private static bool IsSafeRequired(
        string? value,
        int maximumLength) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= maximumLength
        && !value.Any(char.IsControl);

    private static bool IsSafeOptional(
        string? value,
        int maximumLength) =>
        value is null || IsSafeRequired(value, maximumLength);
}
