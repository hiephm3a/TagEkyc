using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace TagEkyc.Infrastructure.RawExport;

public static class RecipientPackageReferenceCursorCodec
{
    public const string MacLabel = "tip-88c1-c4-cursor-mac-v1";
    public const int MaximumTokenLength = 244;
    private static readonly byte[] Magic = "C4R1"u8.ToArray();

    public static string Encode(RecipientPackageReferenceCursor value, ReadOnlySpan<byte> key)
    {
        if (key.Length != 32 || !RecipientPackageReferenceOptions.ValidKeyId(value.KeyId)
            || value.KeyVersion <= 0 || value.ClientApplicationId == Guid.Empty
            || value.PrincipalId == Guid.Empty || value.PageSize is < 1 or > 50
            || value.BoundaryPackageId == Guid.Empty || value.BoundaryFinalizedAtUtc.Offset != TimeSpan.Zero
            || value.ExpiresAtUnixSeconds != checked(value.IssuedAtUnixSeconds + 900))
            throw new ArgumentException("Recipient package reference cursor is invalid.", nameof(value));

        var id = Encoding.ASCII.GetBytes(value.KeyId);
        var payload = new byte[86 + id.Length];
        Magic.CopyTo(payload, 0); payload[4] = 1; payload[5] = checked((byte)id.Length); id.CopyTo(payload, 6);
        var offset = 6 + id.Length;
        BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(offset), value.KeyVersion); offset += 4;
        payload[offset++] = 0; payload[offset++] = 1;
        WriteGuid(payload.AsSpan(offset, 16), value.ClientApplicationId); offset += 16;
        WriteGuid(payload.AsSpan(offset, 16), value.PrincipalId); offset += 16;
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(offset), checked((ushort)value.PageSize)); offset += 2;
        BinaryPrimitives.WriteInt64BigEndian(payload.AsSpan(offset), value.BoundaryFinalizedAtUtc.UtcDateTime.Ticks); offset += 8;
        WriteGuid(payload.AsSpan(offset, 16), value.BoundaryPackageId); offset += 16;
        BinaryPrimitives.WriteInt64BigEndian(payload.AsSpan(offset), value.IssuedAtUnixSeconds); offset += 8;
        BinaryPrimitives.WriteInt64BigEndian(payload.AsSpan(offset), value.ExpiresAtUnixSeconds);
        var preimage = Preimage(payload);
        try
        {
            var mac = HMACSHA256.HashData(key, preimage);
            try { return Base64Url(payload) + "." + Base64Url(mac); }
            finally { CryptographicOperations.ZeroMemory(mac); }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(id);
            CryptographicOperations.ZeroMemory(preimage);
            CryptographicOperations.ZeroMemory(payload);
        }
    }

    public static RecipientPackageReferenceCursorParseResult ParseStructure(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > MaximumTokenLength || token.Any(char.IsWhiteSpace)) return Invalid();
        var dot = token.IndexOf('.');
        if (dot <= 0 || dot != token.LastIndexOf('.') || dot == token.Length - 1) return Invalid();
        if (!TryDecodeCanonical(token[..dot], out var payload) || !TryDecodeCanonical(token[(dot + 1)..], out var mac)) return Invalid();
        try
        {
            if (mac.Length != 32 || payload.Length < 87 || !payload.AsSpan(0, 4).SequenceEqual(Magic)
                || payload[4] != 1) return InvalidOwned(payload, mac);
            var idLength = payload[5];
            if (idLength is < 1 or > 64 || payload.Length != 86 + idLength) return InvalidOwned(payload, mac);
            var keyId = Encoding.ASCII.GetString(payload, 6, idLength);
            if (!RecipientPackageReferenceOptions.ValidKeyId(keyId)) return InvalidOwned(payload, mac);
            var offset = 6 + idLength;
            var version = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(offset)); offset += 4;
            if (version <= 0 || payload[offset++] != 0 || payload[offset++] != 1) return InvalidOwned(payload, mac);
            var client = ReadGuid(payload.AsSpan(offset, 16)); offset += 16;
            var principal = ReadGuid(payload.AsSpan(offset, 16)); offset += 16;
            var pageSize = BinaryPrimitives.ReadUInt16BigEndian(payload.AsSpan(offset)); offset += 2;
            var ticks = BinaryPrimitives.ReadInt64BigEndian(payload.AsSpan(offset)); offset += 8;
            var boundaryId = ReadGuid(payload.AsSpan(offset, 16)); offset += 16;
            var issued = BinaryPrimitives.ReadInt64BigEndian(payload.AsSpan(offset)); offset += 8;
            var expires = BinaryPrimitives.ReadInt64BigEndian(payload.AsSpan(offset));
            if (client == Guid.Empty || principal == Guid.Empty || pageSize is < 1 or > 50 || boundaryId == Guid.Empty
                || ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks
                || expires != checked(issued + 900)) return InvalidOwned(payload, mac);
            var boundary = new DateTimeOffset(ticks, TimeSpan.Zero);
            return new(RecipientPackageReferenceCursorParseOutcome.Parsed,
                new(keyId, version, client, principal, pageSize, boundary, boundaryId, issued, expires), payload, mac);
        }
        catch (Exception exception) when (exception is ArgumentException or OverflowException)
        {
            return InvalidOwned(payload, mac);
        }
    }

    public static bool Verify(RecipientPackageReferenceCursorParseResult parsed, ReadOnlySpan<byte> key)
    {
        if (parsed.Outcome != RecipientPackageReferenceCursorParseOutcome.Parsed
            || parsed.Payload is null || parsed.Mac is null || key.Length != 32) return false;
        var preimage = Preimage(parsed.Payload);
        try
        {
            var expected = HMACSHA256.HashData(key, preimage);
            try { return CryptographicOperations.FixedTimeEquals(expected, parsed.Mac); }
            finally { CryptographicOperations.ZeroMemory(expected); }
        }
        finally { CryptographicOperations.ZeroMemory(preimage); }
    }

    public static void Release(RecipientPackageReferenceCursorParseResult parsed)
    {
        if (parsed.Payload is not null) CryptographicOperations.ZeroMemory(parsed.Payload);
        if (parsed.Mac is not null) CryptographicOperations.ZeroMemory(parsed.Mac);
    }

    private static byte[] Preimage(ReadOnlySpan<byte> payload)
    {
        var label = Encoding.ASCII.GetBytes(MacLabel);
        var value = new byte[label.Length + 1 + payload.Length];
        label.CopyTo(value, 0); value[label.Length] = 0; payload.CopyTo(value.AsSpan(label.Length + 1));
        CryptographicOperations.ZeroMemory(label);
        return value;
    }
    private static string Base64Url(ReadOnlySpan<byte> bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static bool TryDecodeCanonical(string text, out byte[] bytes)
    {
        bytes = [];
        if (text.Length == 0 || text.Contains('=') || text.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))) return false;
        var canonical = text.Replace('-', '+').Replace('_', '/');
        canonical += (canonical.Length % 4) switch { 2 => "==", 3 => "=", 0 => "", _ => "!" };
        try { bytes = Convert.FromBase64String(canonical); return Base64Url(bytes) == text; }
        catch (FormatException) { return false; }
    }
    private static void WriteGuid(Span<byte> target, Guid value) => value.TryWriteBytes(target, bigEndian: true, out _);
    private static Guid ReadGuid(ReadOnlySpan<byte> source) => new(source, bigEndian: true);
    private static RecipientPackageReferenceCursorParseResult Invalid() => new(RecipientPackageReferenceCursorParseOutcome.Invalid, null, null, null);
    private static RecipientPackageReferenceCursorParseResult InvalidOwned(byte[] payload, byte[] mac)
    { CryptographicOperations.ZeroMemory(payload); CryptographicOperations.ZeroMemory(mac); return Invalid(); }
}
