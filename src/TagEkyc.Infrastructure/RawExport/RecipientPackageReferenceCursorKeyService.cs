using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using TagEkyc.Infrastructure.ProtectedValues;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RecipientPackageReferenceCursorKeyService(
    IConfiguration configuration,
    RecipientPackageReferenceOptions options)
{
    internal async ValueTask<RecipientPackageReferenceKeyLease?> ResolveAsync(
        RecipientPackageReferenceKeyIdentity identity,
        CancellationToken cancellationToken)
    {
        if (!options.AcceptedKeys.Contains(identity)) return null;
        var accepted = options.AcceptedKeys.Select(value => (value.KeyId, value.KeyVersion)).ToHashSet();
        var catalog = new RecipientPackageReferenceCursorKeyCatalog(accepted);
        var request = RecipientPackageReferenceCursorKeyCatalog.CreateRequest(identity.KeyId, identity.KeyVersion);
        var descriptor = await catalog.FindAsync(request, cancellationToken).ConfigureAwait(false);
        if (descriptor is null) return null;
        var raw = configuration[descriptor.ExactReference.Target];
        byte[]? material = null;
        if (string.IsNullOrEmpty(raw) || !TryDecode(raw, out material) || material is null || material.Length != 32)
        {
            if (material is not null) CryptographicOperations.ZeroMemory(material);
            return null;
        }
        return new RecipientPackageReferenceKeyLease(material);
    }

    private static bool TryDecode(string value, out byte[]? bytes)
    {
        bytes = null;
        if (value.Contains('=') || value.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))) return false;
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 += (base64.Length % 4) switch { 2 => "==", 3 => "=", 0 => "", _ => "!" };
        try
        {
            bytes = Convert.FromBase64String(base64);
            var canonical = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            if (!string.Equals(canonical, value, StringComparison.Ordinal))
            { CryptographicOperations.ZeroMemory(bytes); bytes = null; return false; }
            return true;
        }
        catch (FormatException) { return false; }
    }
}

internal sealed class RecipientPackageReferenceKeyLease(byte[] material) : IDisposable
{
    private byte[]? material = material;
    internal byte[] Material => material ?? throw new ObjectDisposedException(nameof(RecipientPackageReferenceKeyLease));
    public void Dispose()
    {
        var value = Interlocked.Exchange(ref material, null);
        if (value is not null) CryptographicOperations.ZeroMemory(value);
    }
    public override string ToString() => "RecipientPackageReferenceKeyLease:<redacted>";
}
