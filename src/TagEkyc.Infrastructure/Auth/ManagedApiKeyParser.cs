namespace TagEkyc.Infrastructure.Auth;

public sealed record ParsedManagedApiKey(string Prefix);

public static class ManagedApiKeyParser
{
    public static ParsedManagedApiKey? Parse(string presentedApiKey)
    {
        if (string.IsNullOrEmpty(presentedApiKey))
        {
            return null;
        }

        const string marker = "tek_";
        if (!presentedApiKey.StartsWith(marker, StringComparison.Ordinal))
        {
            return null;
        }

        var separator = presentedApiKey.IndexOf('_', marker.Length);
        if (separator < 0)
        {
            return null;
        }

        var prefix = presentedApiKey[marker.Length..separator];
        var secret = presentedApiKey[(separator + 1)..];
        if (prefix.Length != ManagedApiKeyConstants.KeyPrefixLength ||
            secret.Length < ManagedApiKeyConstants.MinimumSecretLength)
        {
            return null;
        }

        return IsBase64Url(prefix) && IsBase64Url(secret)
            ? new ParsedManagedApiKey(prefix)
            : null;
    }

    private static bool IsBase64Url(string value)
    {
        foreach (var character in value)
        {
            if ((character >= 'A' && character <= 'Z') ||
                (character >= 'a' && character <= 'z') ||
                (character >= '0' && character <= '9') ||
                character == '-' ||
                character == '_')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
