namespace TagEkyc.RawExport.Client;

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class TagEkycRecipientPackageDecoder
{
    private static readonly byte[] PackageMagic = "TIP-88C1-C2-PACKAGE-V1"u8.ToArray();
    private static readonly byte[] AssemblyMagic = "TIP-88C1-ASSEMBLY-V1"u8.ToArray();
    private const int MaximumHeaderLength = 1_848;
    private const int MaximumAssemblyLength = 33_554_432;
    private const int MaximumFrames = 32;

    internal static TagEkycRawBiometricMaterialLease Decode(
        byte[] package,
        ReadOnlySpan<byte> privateKey,
        Guid expectedPackageId,
        Guid expectedJobId,
        Guid expectedVerificationSessionId,
        Guid expectedRecipientClientApplicationId,
        string expectedKeyId,
        int expectedKeyVersion)
    {
        ArgumentNullException.ThrowIfNull(package);
        using var input = new MemoryStream(package, writable: false);
        var prefix = ReadExactly(input, PackageMagic.Length + 4);
        if (!prefix.AsSpan(0, PackageMagic.Length).SequenceEqual(PackageMagic)) Invalid();
        var headerLength = checked((int)BinaryPrimitives.ReadUInt32BigEndian(prefix.AsSpan(PackageMagic.Length)));
        if (headerLength is < 1 or > MaximumHeaderLength) Invalid();
        var headerBytes = ReadExactly(input, headerLength);
        var envelope = new byte[prefix.Length + headerBytes.Length];
        prefix.CopyTo(envelope, 0);
        headerBytes.CopyTo(envelope, prefix.Length);
        var envelopeDigest = SHA256.HashData(envelope);
        byte[]? cek = null;
        try
        {
            using var document = JsonDocument.Parse(headerBytes);
            var root = document.RootElement;
            RequireString(root, "contentEncryptionAlgorithm", "A256GCM-FRAME-V1");
            RequireString(root, "keyWrapAlgorithm", "RSA-OAEP-256");
            RequireString(root, "signatureAlgorithm", "none");
            RequireString(root, "packageProfile", "tip-88c1-c2-package-profile-v1");
            var packageId = RequiredGuid(root, "packageId");
            var recipient = RequiredGuid(root, "recipientClientApplicationId");
            var keyId = RequiredString(root, "recipientKeyId");
            var keyVersion = RequiredInt32(root, "recipientKeyVersion");
            var completeLength = RequiredInt64(root, "completeAssemblyLength");
            var noncePrefix = Base64Url(RequiredString(root, "noncePrefix"));
            var wrappedCek = Base64Url(RequiredString(root, "wrappedCek"));
            if (packageId != expectedPackageId || recipient != expectedRecipientClientApplicationId
                || !string.Equals(keyId, expectedKeyId, StringComparison.Ordinal)
                || keyVersion != expectedKeyVersion || completeLength is < 1 or > MaximumAssemblyLength
                || noncePrefix.Length != 8)
                Invalid();

            using var rsa = RSA.Create();
            ImportPrivateKey(rsa, privateKey);
            cek = rsa.Decrypt(wrappedCek, RSAEncryptionPadding.OaepSHA256);
            if (cek.Length != 32) Invalid();
            var plaintext = DecryptFrames(input, cek, noncePrefix, envelopeDigest, completeLength);
            try
            {
                return ReadAssembly(plaintext, expectedPackageId, expectedJobId,
                    expectedVerificationSessionId, expectedRecipientClientApplicationId);
            }
            finally { CryptographicOperations.ZeroMemory(plaintext); }
        }
        catch (TagEkycRawExportClientException) { throw; }
        catch (Exception exception) when (exception is JsonException or CryptographicException
            or FormatException or OverflowException or EndOfStreamException)
        {
            throw new TagEkycRawExportClientException(
                "RAW_EXPORT_PACKAGE_INVALID",
                "The TagEkyc recipient package was invalid or could not be decrypted.",
                innerException: exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(prefix);
            CryptographicOperations.ZeroMemory(headerBytes);
            CryptographicOperations.ZeroMemory(envelope);
            CryptographicOperations.ZeroMemory(envelopeDigest);
            if (cek is not null) CryptographicOperations.ZeroMemory(cek);
        }
    }

    private static byte[] DecryptFrames(
        Stream input,
        byte[] cek,
        byte[] noncePrefix,
        byte[] envelopeDigest,
        long expectedLength)
    {
        using var output = new MemoryStream(checked((int)expectedLength));
        using var aes = new AesGcm(cek, 16);
        var expectedOrdinal = 0;
        while (true)
        {
            var ordinalBytes = ReadExactly(input, 4);
            var ordinal = BinaryPrimitives.ReadUInt32BigEndian(ordinalBytes);
            CryptographicOperations.ZeroMemory(ordinalBytes);
            if (ordinal == uint.MaxValue)
            {
                var tail = ReadExactly(input, 8 + 4 + 16);
                var total = checked((long)BinaryPrimitives.ReadUInt64BigEndian(tail));
                var frameCount = checked((int)BinaryPrimitives.ReadUInt32BigEndian(tail.AsSpan(8)));
                var nonce = Nonce(noncePrefix, uint.MaxValue);
                var aad = Aad("tip-88c1-c2-completion-aad-v1",
                    Convert.ToHexString(envelopeDigest).ToLowerInvariant(),
                    frameCount.ToString(CultureInfo.InvariantCulture),
                    total.ToString(CultureInfo.InvariantCulture));
                try
                {
                    aes.Decrypt(nonce, ReadOnlySpan<byte>.Empty, tail.AsSpan(12, 16), Span<byte>.Empty, aad);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(tail);
                    CryptographicOperations.ZeroMemory(nonce);
                    CryptographicOperations.ZeroMemory(aad);
                }
                if (total != expectedLength || frameCount != expectedOrdinal || input.Position != input.Length) Invalid();
                return output.ToArray();
            }

            if (ordinal != expectedOrdinal || expectedOrdinal >= MaximumFrames) Invalid();
            var lengthBytes = ReadExactly(input, 4);
            var length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(lengthBytes));
            if (length is < 1 or > 1_048_576 || output.Length + length > expectedLength) Invalid();
            var ciphertext = ReadExactly(input, length);
            var tag = ReadExactly(input, 16);
            var plaintext = new byte[length];
            var nonceData = Nonce(noncePrefix, ordinal);
            var frameAad = Aad("tip-88c1-c2-frame-aad-v1",
                Convert.ToHexString(envelopeDigest).ToLowerInvariant(),
                expectedOrdinal.ToString(CultureInfo.InvariantCulture),
                length.ToString(CultureInfo.InvariantCulture), "data");
            try
            {
                aes.Decrypt(nonceData, ciphertext, tag, plaintext, frameAad);
                output.Write(plaintext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(lengthBytes);
                CryptographicOperations.ZeroMemory(ciphertext);
                CryptographicOperations.ZeroMemory(tag);
                CryptographicOperations.ZeroMemory(plaintext);
                CryptographicOperations.ZeroMemory(nonceData);
                CryptographicOperations.ZeroMemory(frameAad);
            }
            expectedOrdinal++;
        }
    }

    private static TagEkycRawBiometricMaterialLease ReadAssembly(
        byte[] plaintext,
        Guid packageId,
        Guid expectedJobId,
        Guid expectedSessionId,
        Guid expectedRecipient)
    {
        using var input = new MemoryStream(plaintext, writable: false);
        var prefix = ReadExactly(input, AssemblyMagic.Length + 4);
        if (!prefix.AsSpan(0, AssemblyMagic.Length).SequenceEqual(AssemblyMagic)) Invalid();
        var headerLength = checked((int)BinaryPrimitives.ReadUInt32BigEndian(prefix.AsSpan(AssemblyMagic.Length)));
        if (headerLength is < 1 or > 128 * 1024) Invalid();
        var headerBytes = ReadExactly(input, headerLength);
        using var document = JsonDocument.Parse(headerBytes);
        var root = document.RootElement;
        var jobId = RequiredGuid(root, "jobId");
        var sessionId = RequiredGuid(root, "verificationSessionId");
        var recipient = RequiredGuid(root, "recipientClientApplicationId");
        if (jobId != expectedJobId || sessionId != expectedSessionId || recipient != expectedRecipient) Invalid();
        var descriptors = root.GetProperty("items").EnumerateArray()
            .Select(item => new ItemDescriptor(
                RequiredInt32(item, "ordinal"),
                RequiredString(item, "rawClass"),
                RequiredInt64(item, "plaintextLength")))
            .OrderBy(item => item.Ordinal)
            .ToArray();
        if (descriptors.Length != 2
            || descriptors[0].Ordinal != 0
            || !string.Equals(descriptors[0].RawClass, "ChipDg2Portrait", StringComparison.Ordinal)
            || descriptors[1].Ordinal != 1
            || !string.Equals(descriptors[1].RawClass, "LiveSelfieImage", StringComparison.Ordinal))
            Invalid();
        byte[]? dg2 = null;
        byte[]? selfie = null;
        try
        {
            foreach (var descriptor in descriptors)
            {
                var ordinalBytes = ReadExactly(input, 4);
                var lengthBytes = ReadExactly(input, 8);
                var ordinal = checked((int)BinaryPrimitives.ReadUInt32BigEndian(ordinalBytes));
                var length = checked((long)BinaryPrimitives.ReadUInt64BigEndian(lengthBytes));
                CryptographicOperations.ZeroMemory(ordinalBytes);
                CryptographicOperations.ZeroMemory(lengthBytes);
                if (ordinal != descriptor.Ordinal || length != descriptor.Length
                    || length is < 1 or > MaximumAssemblyLength) Invalid();
                var value = ReadExactly(input, checked((int)length));
                if (descriptor.Ordinal == 0) dg2 = value;
                else selfie = value;
            }
            if (input.Position != input.Length || dg2 is null || selfie is null) Invalid();
            var lease = new TagEkycRawBiometricMaterialLease(sessionId, jobId, packageId, dg2!, selfie!);
            dg2 = null;
            selfie = null;
            return lease;
        }
        finally
        {
            if (dg2 is not null) CryptographicOperations.ZeroMemory(dg2);
            if (selfie is not null) CryptographicOperations.ZeroMemory(selfie);
            CryptographicOperations.ZeroMemory(prefix);
            CryptographicOperations.ZeroMemory(headerBytes);
        }
    }

    private static byte[] Aad(string domain, params string[] fields)
    {
        using var stream = new MemoryStream();
        WriteField(stream, domain);
        foreach (var field in fields) WriteField(stream, field);
        return stream.ToArray();
    }

    private static void WriteField(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value.Normalize(NormalizationForm.FormC));
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, checked((uint)bytes.Length));
        stream.Write(length);
        stream.Write(bytes);
        CryptographicOperations.ZeroMemory(bytes);
    }

    private static byte[] Nonce(byte[] prefix, uint ordinal)
    {
        var nonce = new byte[12];
        prefix.CopyTo(nonce, 0);
        BinaryPrimitives.WriteUInt32BigEndian(nonce.AsSpan(8), ordinal);
        return nonce;
    }

    private static void ImportPrivateKey(RSA rsa, ReadOnlySpan<byte> key)
    {
        if (key.Length == 0) Invalid();
        if (key[0] == (byte)'-')
        {
            rsa.ImportFromPem(Encoding.UTF8.GetString(key));
            return;
        }
        rsa.ImportPkcs8PrivateKey(key, out var consumed);
        if (consumed != key.Length) Invalid();
    }

    private static byte[] Base64Url(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized += (normalized.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            0 => "",
            _ => throw new FormatException(),
        };
        return Convert.FromBase64String(normalized);
    }

    private static byte[] ReadExactly(Stream stream, int length)
    {
        var value = new byte[length];
        stream.ReadExactly(value);
        return value;
    }

    private static string RequiredString(JsonElement value, string name) =>
        value.GetProperty(name).GetString() is { Length: > 0 } text ? text : throw new JsonException();

    private static void RequireString(JsonElement value, string name, string expected)
    {
        if (!string.Equals(RequiredString(value, name), expected, StringComparison.Ordinal)) Invalid();
    }

    private static Guid RequiredGuid(JsonElement value, string name) =>
        Guid.TryParseExact(RequiredString(value, name), "D", out var result) && result != Guid.Empty
            ? result
            : throw new JsonException();

    private static int RequiredInt32(JsonElement value, string name) => value.GetProperty(name).GetInt32();
    private static long RequiredInt64(JsonElement value, string name) => value.GetProperty(name).GetInt64();
    private static void Invalid() => throw new TagEkycRawExportClientException(
        "RAW_EXPORT_PACKAGE_INVALID",
        "The TagEkyc recipient package was invalid or could not be decrypted.");

    private sealed record ItemDescriptor(int Ordinal, string RawClass, long Length);
}
