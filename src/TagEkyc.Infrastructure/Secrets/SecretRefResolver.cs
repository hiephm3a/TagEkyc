namespace TagEkyc.Infrastructure.Secrets;

public enum SecretRefErrorKind
{
    Invalid = 0,
    Missing = 1,
}

public sealed record SecretRefValue(
    string Value,
    string SourceType,
    string RedactedIdentifier);

public sealed class SecretRefResolutionException(
    SecretRefErrorKind errorKind,
    string sourceType,
    string redactedIdentifier) : Exception("SECRET_REF_RESOLUTION_FAILED")
{
    public SecretRefErrorKind ErrorKind { get; } = errorKind;

    public string SourceType { get; } = sourceType;

    public string RedactedIdentifier { get; } = redactedIdentifier;
}

public static class SecretRefResolver
{
    public static SecretRefValue Resolve(string secretRef)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
        {
            throw Invalid("unknown", "redacted");
        }

        var separator = secretRef.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0)
        {
            throw Invalid("unknown", "redacted");
        }

        var scheme = secretRef[..separator];
        var target = secretRef[(separator + 1)..];
        if (string.Equals(scheme, "env", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveEnvironment(target);
        }

        if (string.Equals(scheme, "file", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveFile(target);
        }

        throw Invalid(scheme, "redacted");
    }

    private static SecretRefValue ResolveEnvironment(string variableName)
    {
        if (string.IsNullOrWhiteSpace(variableName))
        {
            throw Invalid("env", "redacted");
        }

        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Missing("env", "redacted");
        }

        return new SecretRefValue(value, "env", "redacted");
    }

    private static SecretRefValue ResolveFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
        {
            throw Invalid("file", "redacted");
        }

        if (Directory.Exists(path))
        {
            throw Invalid("file", "redacted");
        }

        if (!File.Exists(path))
        {
            throw Missing("file", "redacted");
        }

        var value = File.ReadAllText(path).TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Missing("file", "redacted");
        }

        return new SecretRefValue(value, "file", "redacted");
    }

    private static SecretRefResolutionException Invalid(string sourceType, string redactedIdentifier) =>
        new(SecretRefErrorKind.Invalid, sourceType, redactedIdentifier);

    private static SecretRefResolutionException Missing(string sourceType, string redactedIdentifier) =>
        new(SecretRefErrorKind.Missing, sourceType, redactedIdentifier);
}
