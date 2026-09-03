using System.Security.Cryptography;
using TagEkyc.Application.Ports;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class RecipientPublicKeyProfileValidator : IRecipientPublicKeyProfileValidator
{
    public bool TryValidate(
        string algorithm,
        string spkiBase64,
        string fingerprintHex,
        out ValidatedRecipientPublicKey? key)
    {
        key = null;
        if (!string.Equals(algorithm, "RSA-OAEP-256", StringComparison.Ordinal)) return false;
        byte[] spki;
        byte[] claimed;
        try
        {
            spki = Convert.FromBase64String(spkiBase64);
            claimed = Convert.FromHexString(fingerprintHex);
        }
        catch (FormatException)
        {
            return false;
        }

        try
        {
            if (claimed.Length != 32 || spki.Length == 0) return false;
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(spki, out var consumed);
            if (consumed != spki.Length || rsa.KeySize is < 3072 or > 4096) return false;
            var computed = SHA256.HashData(spki);
            try
            {
                if (!CryptographicOperations.FixedTimeEquals(computed, claimed)) return false;
                key = new(spki.ToArray(), computed.ToArray());
                return true;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(computed);
            }
        }
        catch (CryptographicException)
        {
            return false;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(claimed);
            CryptographicOperations.ZeroMemory(spki);
        }
    }
}
