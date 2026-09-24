using System.Text;
using TagEkyc.Infrastructure.Secrets;

namespace TagEkyc.Infrastructure.ProtectedValues;

internal sealed class SecretRefProtectedValueProvider : IProtectedValueProvider
{
    public string Scheme => "secret-ref";

    public ValueTask<ProtectedValueProviderResolution> ResolveAsync(
        ProtectedValueDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!StringComparer.Ordinal.Equals(descriptor.ExactReference.Scheme, Scheme))
            return ValueTask.FromResult(ProtectedValueProviderResolution.Rejected(
                ProtectedValueFailureCause.ReferenceMismatch));
        try
        {
            var resolved = SecretRefResolver.Resolve(descriptor.ExactReference.Target);
            var bytes = Encoding.UTF8.GetBytes(resolved.Value);
            if (bytes.Length is 0 or > ProtectedValueMaterialLease.AbsoluteMaximumBytes)
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
                return ValueTask.FromResult(ProtectedValueProviderResolution.Rejected(
                    ProtectedValueFailureCause.MaterialInvalid));
            }
            return ValueTask.FromResult(ProtectedValueProviderResolution.Found(
                ProtectedValueMaterialLease.CreateOwned(bytes),
                new ProtectedValueTechnicalMetadata(
                    null, null, null, resolved.SourceType,
                    resolved.RedactedIdentifier, null)));
        }
        catch (SecretRefResolutionException exception)
        {
            return ValueTask.FromResult(exception.ErrorKind == SecretRefErrorKind.Missing
                ? ProtectedValueProviderResolution.NotFound(ProtectedValueFailureCause.ValueNotFound)
                : ProtectedValueProviderResolution.Rejected(ProtectedValueFailureCause.ReferenceInvalid));
        }
    }
}
