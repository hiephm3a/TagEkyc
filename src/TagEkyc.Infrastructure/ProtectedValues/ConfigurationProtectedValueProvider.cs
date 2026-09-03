// Copied from SignFlow ProtectedValues (Codex_SignFlow) — TagEkyc-owned fork;
// do not add a SignFlow project reference.
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace TagEkyc.Infrastructure.ProtectedValues;

internal sealed class ConfigurationProtectedValueProvider :
    IProtectedValueProvider
{
    private static readonly Regex TargetGrammar = new(
        "^[A-Za-z0-9_.-]+(?::[A-Za-z0-9_.-]+)*$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private readonly IConfiguration _configuration;

    public ConfigurationProtectedValueProvider(
        IConfiguration configuration)
    {
        _configuration =
            configuration
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string Scheme => "config";

    public ValueTask<ProtectedValueProviderResolution> ResolveAsync(
        ProtectedValueDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!StringComparer.Ordinal.Equals(
                descriptor.ExactReference.Scheme,
                Scheme))
        {
            return ValueTask.FromResult(
                ProtectedValueProviderResolution.Rejected(
                    ProtectedValueFailureCause.ReferenceMismatch));
        }

        var target = descriptor.ExactReference.Target;
        if (!TargetGrammar.IsMatch(target))
        {
            return ValueTask.FromResult(
                ProtectedValueProviderResolution.Rejected(
                    ProtectedValueFailureCause.ReferenceInvalid));
        }

        var value = _configuration[target];
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValueTask.FromResult(
                ProtectedValueProviderResolution.NotFound(
                    ProtectedValueFailureCause.ValueNotFound));
        }

        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > ProtectedValueMaterialLease.AbsoluteMaximumBytes)
        {
            return ValueTask.FromResult(
                ProtectedValueProviderResolution.Rejected(
                    ProtectedValueFailureCause.MaterialInvalid));
        }

        var material = Encoding.UTF8.GetBytes(value);
        var lease = ProtectedValueMaterialLease.CreateOwned(material);
        return ValueTask.FromResult(
            ProtectedValueProviderResolution.Found(
                lease,
                new ProtectedValueTechnicalMetadata(
                    null,
                    null,
                    null,
                    Scheme,
                    "redacted",
                    null)));
    }
}
