using System.Security.Cryptography;
using System.Text;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class RawExportAssemblyAuthenticationService(
    IRawExportAssemblyAuthenticationProvider provider)
{
    public string KeyId => provider.KeyId;

    public int KeyVersion => provider.KeyVersion;

    public async Task<byte[]> AuthenticateAsync(
        ReadOnlyMemory<byte> manifestDigest,
        CancellationToken cancellationToken)
    {
        if (manifestDigest.Length != 32
            || string.IsNullOrWhiteSpace(provider.KeyId)
            || provider.KeyVersion < 1)
            throw new InvalidOperationException(RawExportAssemblyOptions.AuthenticatorUnavailable);

        var payload = BuildAuthenticationPayload(manifestDigest.Span);
        byte[] value;
        try
        {
            value = await provider.AuthenticateManifestAsync(
                payload,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(payload);
        }
        if (value.Length != 32)
        {
            CryptographicOperations.ZeroMemory(value);
            throw new InvalidOperationException(RawExportAssemblyOptions.AuthenticatorUnavailable);
        }

        return value;
    }

    public static byte[] BuildAuthenticationPayload(ReadOnlySpan<byte> manifestDigest)
    {
        if (manifestDigest.Length != 32)
            throw new ArgumentException("Manifest digest must contain 32 bytes.", nameof(manifestDigest));
        using var stream = new MemoryStream(75);
        WriteLengthPrefixed(stream, Encoding.ASCII.GetBytes("tip-88c1-assembly-authentication-v1"));
        WriteLengthPrefixed(stream, manifestDigest);
        return stream.ToArray();
    }

    private static void WriteLengthPrefixed(Stream stream, ReadOnlySpan<byte> value)
    {
        Span<byte> length = stackalloc byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(length, checked((uint)value.Length));
        stream.Write(length);
        stream.Write(value);
    }
}
