using Microsoft.Extensions.DependencyInjection;

namespace TagEkyc.Infrastructure.RawExport;

internal interface IRawExportAssemblyReconcilerScopeFactory
{
    ValueTask<IRawExportAssemblyReconcilerScope> OpenAsync(
        CancellationToken cancellationToken);
}

internal interface IRawExportAssemblyReconcilerScope : IAsyncDisposable
{
    IProvisionalObjectReconciler Reconciler { get; }
}

// Production assembly reads borrow the already-qualified reconciler authority.
// This adapter owns the role scope; neither the reconciler nor its exact read
// can escape the operation that disposes this scope.
internal sealed class CaptureRuntimeAssemblyReconcilerScopeFactory(
    CaptureRuntimeCustodyProviderScopes custodyScopes)
    : IRawExportAssemblyReconcilerScopeFactory
{
    public async ValueTask<IRawExportAssemblyReconcilerScope> OpenAsync(
        CancellationToken cancellationToken)
    {
        var roleScope = await custodyScopes.OpenReconcilerAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            return new Scope(
                roleScope,
                roleScope.Services.GetRequiredService<IProvisionalObjectReconciler>());
        }
        catch
        {
            await roleScope.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private sealed class Scope(
        CaptureRuntimeCustodyProviderScopes.RoleScope roleScope,
        IProvisionalObjectReconciler reconciler)
        : IRawExportAssemblyReconcilerScope
    {
        public IProvisionalObjectReconciler Reconciler => reconciler;

        public ValueTask DisposeAsync() => roleScope.DisposeAsync();
    }
}

// FixtureProof retains its historical root fixture reconciler. This type is
// registered only for the non-production FixtureProof topology and is rejected
// by the exact Production registration guard.
internal sealed class FixtureAssemblyReconcilerScopeFactory(
    IProvisionalObjectReconciler reconciler)
    : IRawExportAssemblyReconcilerScopeFactory
{
    public ValueTask<IRawExportAssemblyReconcilerScope> OpenAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IRawExportAssemblyReconcilerScope>(
            new Scope(reconciler));
    }

    private sealed class Scope(IProvisionalObjectReconciler reconciler)
        : IRawExportAssemblyReconcilerScope
    {
        public IProvisionalObjectReconciler Reconciler => reconciler;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
