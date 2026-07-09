using System.Security.Cryptography;
using System.Text;

namespace TagEkyc.Infrastructure.Auth;

public static class ApiKeyHasher
{
    public static byte[] Hash(byte[] pepper, string presentedApiKey)
    {
        using var hmac = new HMACSHA256(pepper);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(presentedApiKey));
    }

    public static bool FixedTimeEquals(byte[] left, byte[] right) =>
        CryptographicOperations.FixedTimeEquals(left, right);
}
