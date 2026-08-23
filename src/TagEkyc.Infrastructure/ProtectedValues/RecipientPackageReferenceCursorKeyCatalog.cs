namespace TagEkyc.Infrastructure.ProtectedValues;

internal sealed class RecipientPackageReferenceCursorKeyCatalog(
    IReadOnlySet<(string KeyId, int KeyVersion)> accepted) : IProtectedValueCatalog
{
    internal static ProtectedValueRequest CreateRequest(string keyId, int keyVersion) =>
        new(ProtectedValuePurpose.PackageReferenceCursorHmac, new ProtectedValueId($"{keyId}:{keyVersion}"));

    public ValueTask<ProtectedValueDescriptor?> FindAsync(
        ProtectedValueRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var pair in accepted)
        {
            var expected = CreateRequest(pair.KeyId, pair.KeyVersion);
            if (request == expected)
            {
                return ValueTask.FromResult<ProtectedValueDescriptor?>(new(
                    expected,
                    new ProtectedValueReference(
                        $"config:TagEkyc:RawExport:PackageReference:CursorKeys:{pair.KeyId}:{pair.KeyVersion}")));
            }
        }
        return ValueTask.FromResult<ProtectedValueDescriptor?>(null);
    }
}
