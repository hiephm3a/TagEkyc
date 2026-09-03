// Copied from SignFlow ProtectedValues (Codex_SignFlow) — TagEkyc-owned fork;
// do not add a SignFlow project reference.
namespace TagEkyc.Infrastructure.ProtectedValues;

internal sealed class ProtectedValueResolverOptions
{
    internal static readonly TimeSpan DefaultTotalBudget =
        TimeSpan.FromSeconds(5);

    internal const int DefaultMaxMaterialBytes = 64 * 1024;

    internal ProtectedValueResolverOptions(
        TimeSpan? totalBudget = null,
        int maxMaterialBytes = DefaultMaxMaterialBytes)
    {
        TotalBudget = totalBudget ?? DefaultTotalBudget;
        MaxMaterialBytes = maxMaterialBytes;

        if (TotalBudget <= TimeSpan.Zero
            || TotalBudget > TimeSpan.FromSeconds(30))
        {
            throw new ArgumentOutOfRangeException(nameof(totalBudget));
        }

        if (MaxMaterialBytes is <= 0
            or > ProtectedValueMaterialLease.AbsoluteMaximumBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMaterialBytes));
        }
    }

    internal TimeSpan TotalBudget { get; }

    internal int MaxMaterialBytes { get; }
}

internal sealed class ProtectedValueResolver : IProtectedValueResolver
{
    private readonly IProtectedValueCatalog _catalog;
    private readonly ProtectedValueProviderRegistry _registry;
    private readonly ProtectedValueResolverOptions _options;
    private readonly TimeProvider _timeProvider;

    public ProtectedValueResolver(
        IProtectedValueCatalog catalog,
        ProtectedValueProviderRegistry registry,
        ProtectedValueResolverOptions options,
        TimeProvider timeProvider)
    {
        _catalog =
            catalog ?? throw new ArgumentNullException(nameof(catalog));
        _registry =
            registry ?? throw new ArgumentNullException(nameof(registry));
        _options =
            options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async ValueTask<ProtectedValueResolution> ResolveAsync(
        ProtectedValueRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Purpose is null || request.ValueId is null)
        {
            return ProtectedValueResolution.Rejected(
                ProtectedValueFailureCause.ScopeInvalid);
        }

        using var budgetCts = new CancellationTokenSource(
            _options.TotalBudget,
            _timeProvider);
        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                budgetCts.Token);

        try
        {
            var descriptor =
                await _catalog.FindAsync(request, linkedCts.Token);
            if (descriptor is null)
            {
                return ProtectedValueResolution.NotFound(
                    ProtectedValueFailureCause.CatalogEntryNotFound);
            }

            if (descriptor.LogicalIdentity is null)
            {
                return ProtectedValueResolution.Rejected(
                    ProtectedValueFailureCause.ReferenceMismatch);
            }

            if (descriptor.ExactReference is null)
            {
                return ProtectedValueResolution.Rejected(
                    ProtectedValueFailureCause.ReferenceInvalid);
            }

            if (descriptor.LogicalIdentity != request)
            {
                return ProtectedValueResolution.Rejected(
                    ProtectedValueFailureCause.ReferenceMismatch);
            }

            if (!_registry.TryGet(
                    descriptor.ExactReference.Scheme,
                    out var provider)
                || provider is null)
            {
                return ProtectedValueResolution.ProviderFailure(
                    ProtectedValueFailureCause.ProviderUnknown);
            }

            var providerResult =
                await provider.ResolveAsync(descriptor, linkedCts.Token);
            if (providerResult is null)
            {
                return ProtectedValueResolution.ProviderFailure(
                    ProtectedValueFailureCause.ProviderUnavailable);
            }

            return MapProviderResult(providerResult);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
            when (budgetCts.IsCancellationRequested)
        {
            return ProtectedValueResolution.ProviderFailure(
                ProtectedValueFailureCause.ProviderUnavailable);
        }
        catch
        {
            return ProtectedValueResolution.ProviderFailure(
                ProtectedValueFailureCause.ProviderUnavailable);
        }
    }

    private ProtectedValueResolution MapProviderResult(
        ProtectedValueProviderResolution result)
    {
        if (result.State == ProtectedValueResolutionState.Found)
        {
            if (result.MaterialLease is null || result.Metadata is null)
            {
                result.MaterialLease?.Dispose();
                return ProtectedValueResolution.Rejected(
                    ProtectedValueFailureCause.MaterialInvalid);
            }

            int materialLength;
            try
            {
                materialLength = result.MaterialLease.Material.Length;
            }
            catch (ObjectDisposedException)
            {
                return ProtectedValueResolution.Rejected(
                    ProtectedValueFailureCause.MaterialInvalid);
            }

            if (materialLength == 0
                || materialLength > _options.MaxMaterialBytes)
            {
                result.MaterialLease.Dispose();
                return ProtectedValueResolution.Rejected(
                    ProtectedValueFailureCause.MaterialInvalid);
            }

            return ProtectedValueResolution.Found(
                result.MaterialLease,
                result.Metadata);
        }

        if (result.Cause is null)
        {
            return ProtectedValueResolution.ProviderFailure(
                ProtectedValueFailureCause.ProviderUnavailable);
        }

        return result.State switch
        {
            ProtectedValueResolutionState.NotFound =>
                ProtectedValueResolution.NotFound(result.Cause.Value),
            ProtectedValueResolutionState.Rejected =>
                ProtectedValueResolution.Rejected(result.Cause.Value),
            ProtectedValueResolutionState.ProviderFailure =>
                ProtectedValueResolution.ProviderFailure(result.Cause.Value),
            _ => ProtectedValueResolution.ProviderFailure(
                ProtectedValueFailureCause.ProviderUnavailable),
        };
    }
}
