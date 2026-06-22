using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TagEkyc.Application.VerificationSessions;

public static class EvidenceCanonicalization
{
    public const string PackageVersion = "evidence-package-v2";
    public const string CanonicalizationScheme = "rfc8785-jcs-v1";
    public const string HashAlgorithm = "sha256";
    public const string LegacyPackageVersion = "tip-06-localdev-v1";
    public const string LegacyCanonicalizationScheme = "web-json-deterministic-v1";

    private static readonly JsonSerializerOptions SerializationOptions = CreateSerializationOptions();

    public static string HashCanonical(string label, object value)
    {
        var json = Canonicalize(value);
        var input = Encoding.UTF8.GetBytes($"{label}\n{json}");
        return $"{HashAlgorithm}:{Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant()}";
    }

    public static Guid DeterministicGuid(string label, object value)
    {
        var json = Canonicalize(value);
        var input = Encoding.UTF8.GetBytes($"{label}\n{json}");
        Span<byte> bytes = stackalloc byte[16];
        SHA256.HashData(input).AsSpan(0, 16).CopyTo(bytes);
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    public static string Canonicalize(object value)
    {
        using var document = JsonSerializer.SerializeToDocument(value, SerializationOptions);
        return CanonicalizeElement(document.RootElement);
    }

    public static string CanonicalizeJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return CanonicalizeElement(document.RootElement);
    }

    public static string FormatTimestamp(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);

    public static bool IsKnownHashMetadataCombination(
        string packageVersion,
        string canonicalizationScheme,
        string hashAlgorithm) =>
        IsCurrentHashMetadataCombination(packageVersion, canonicalizationScheme, hashAlgorithm) ||
        IsLegacyHashMetadataCombination(packageVersion, canonicalizationScheme, hashAlgorithm);

    public static void EnsureKnownHashMetadataCombination(
        string packageVersion,
        string canonicalizationScheme,
        string hashAlgorithm)
    {
        if (!IsKnownHashMetadataCombination(packageVersion, canonicalizationScheme, hashAlgorithm))
        {
            throw new EvidenceHashMetadataException(packageVersion, canonicalizationScheme, hashAlgorithm);
        }
    }

    public static bool IsCurrentHashMetadataCombination(
        string packageVersion,
        string canonicalizationScheme,
        string hashAlgorithm) =>
        string.Equals(packageVersion, PackageVersion, StringComparison.Ordinal) &&
        string.Equals(canonicalizationScheme, CanonicalizationScheme, StringComparison.Ordinal) &&
        string.Equals(hashAlgorithm, HashAlgorithm, StringComparison.Ordinal);

    public static bool IsLegacyHashMetadataCombination(
        string packageVersion,
        string canonicalizationScheme,
        string hashAlgorithm) =>
        string.Equals(packageVersion, LegacyPackageVersion, StringComparison.Ordinal) &&
        string.Equals(canonicalizationScheme, LegacyCanonicalizationScheme, StringComparison.Ordinal) &&
        string.Equals(hashAlgorithm, HashAlgorithm, StringComparison.Ordinal);

    private static string CanonicalizeElement(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => CanonicalizeObject(element),
            JsonValueKind.Array => CanonicalizeArray(element),
            JsonValueKind.String => QuoteString(element.GetString() ?? string.Empty),
            JsonValueKind.Number => FormatNumber(element),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => throw new InvalidOperationException($"Unsupported JSON value kind {element.ValueKind}."),
        };

    private static string CanonicalizeObject(JsonElement element)
    {
        var properties = element.EnumerateObject()
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => $"{QuoteString(property.Name)}:{CanonicalizeElement(property.Value)}");
        return $"{{{string.Join(",", properties)}}}";
    }

    private static string CanonicalizeArray(JsonElement element)
    {
        var items = element.EnumerateArray().Select(CanonicalizeElement);
        return $"[{string.Join(",", items)}]";
    }

    private static string QuoteString(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var ch in value)
        {
            builder.Append(ch switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\b' => "\\b",
                '\t' => "\\t",
                '\n' => "\\n",
                '\f' => "\\f",
                '\r' => "\\r",
                <= '\u001F' => $"\\u{(int)ch:x4}",
                _ => ch.ToString(),
            });
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static string FormatNumber(JsonElement element)
    {
        var value = element.GetDouble();
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new InvalidOperationException("RFC 8785 JCS does not support NaN or Infinity.");
        }

        if (value == 0d)
        {
            return "0";
        }

        var formatted = value.ToString("G", CultureInfo.InvariantCulture);
        var absolute = Math.Abs(value);
        if (HasExponent(formatted) && absolute >= 1e-6 && absolute < 1e21)
        {
            return ExpandExponential(formatted);
        }

        return NormalizeExponent(formatted);
    }

    private static string NormalizeExponent(string value)
    {
        var exponentIndex = value.IndexOfAny(['E', 'e']);
        if (exponentIndex < 0)
        {
            return value;
        }

        var mantissa = value[..exponentIndex];
        var exponent = value[(exponentIndex + 1)..];
        var sign = exponent[0] == '-' ? "-" : exponent[0] == '+' ? "+" : string.Empty;
        var digits = sign.Length == 0 ? exponent : exponent[1..];
        digits = digits.TrimStart('0');
        return $"{mantissa}e{sign}{(digits.Length == 0 ? "0" : digits)}";
    }

    private static bool HasExponent(string value) =>
        value.IndexOfAny(['E', 'e']) >= 0;

    private static string ExpandExponential(string value)
    {
        var exponentIndex = value.IndexOfAny(['E', 'e']);
        var mantissa = value[..exponentIndex];
        var exponent = int.Parse(value[(exponentIndex + 1)..], CultureInfo.InvariantCulture);
        var sign = mantissa.StartsWith("-", StringComparison.Ordinal) ? "-" : string.Empty;
        if (sign.Length > 0)
        {
            mantissa = mantissa[1..];
        }

        var decimalIndex = mantissa.IndexOf('.');
        var integerDigits = decimalIndex < 0 ? mantissa.Length : decimalIndex;
        var digits = decimalIndex < 0 ? mantissa : mantissa.Remove(decimalIndex, 1);
        var newDecimalIndex = integerDigits + exponent;
        string expanded;
        if (newDecimalIndex <= 0)
        {
            expanded = $"0.{new string('0', -newDecimalIndex)}{digits}";
        }
        else if (newDecimalIndex >= digits.Length)
        {
            expanded = $"{digits}{new string('0', newDecimalIndex - digits.Length)}";
        }
        else
        {
            expanded = $"{digits[..newDecimalIndex]}.{digits[newDecimalIndex..]}";
        }

        if (expanded.Contains('.', StringComparison.Ordinal))
        {
            expanded = expanded.TrimEnd('0').TrimEnd('.');
        }

        return $"{sign}{expanded}";
    }

    private static JsonSerializerOptions CreateSerializationOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new CanonicalDateTimeOffsetConverter());
        options.Converters.Add(new CanonicalDateTimeConverter());
        return options;
    }

    private sealed class CanonicalDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            DateTimeOffset.Parse(reader.GetString() ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
            writer.WriteStringValue(FormatTimestamp(value));
    }

    private sealed class CanonicalDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            DateTime.Parse(reader.GetString() ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
            writer.WriteStringValue(FormatTimestamp(new DateTimeOffset(value.ToUniversalTime())));
    }
}

public sealed class EvidenceHashMetadataException(
    string packageVersion,
    string canonicalizationScheme,
    string hashAlgorithm)
    : InvalidOperationException(
        $"Unknown evidence hash metadata combination: packageVersion='{packageVersion}', canonicalizationScheme='{canonicalizationScheme}', hashAlgorithm='{hashAlgorithm}'.")
{
    public string PackageVersion { get; } = packageVersion;

    public string CanonicalizationScheme { get; } = canonicalizationScheme;

    public string HashAlgorithm { get; } = hashAlgorithm;
}
