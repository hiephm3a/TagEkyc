namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RecipientPackageDeliverySpoolPool : IDisposable
{
    private readonly SemaphoreSlim permits = new(
        RecipientPackageDeliveryOptions.MaximumConcurrentSpools,
        RecipientPackageDeliveryOptions.MaximumConcurrentSpools);

    internal async Task<Lease?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        if (!await permits.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false)) return null;
        return new Lease(permits, new RecipientPackageEncryptedSpool());
    }

    public void Dispose() => permits.Dispose();

    internal sealed class Lease(SemaphoreSlim owner, RecipientPackageEncryptedSpool spool) : IAsyncDisposable
    {
        private int disposed;
        internal RecipientPackageEncryptedSpool Spool { get; } = spool;
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            await Spool.DisposeAsync().ConfigureAwait(false);
            owner.Release();
        }
    }
}
