namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfPersistenceFaultInjector
{
    public bool ThrowAfterSessionUpdateInFinalization { get; set; }

    public Func<CancellationToken, Task>? BeforeFinalizationSaveAsync { get; set; }

    public Task WaitBeforeFinalizationSaveAsync(CancellationToken cancellationToken) =>
        BeforeFinalizationSaveAsync?.Invoke(cancellationToken) ?? Task.CompletedTask;

    public void ThrowIfFinalizationSessionUpdated()
    {
        if (ThrowAfterSessionUpdateInFinalization)
        {
            throw new InvalidOperationException("Injected finalization failure after session update.");
        }
    }
}
