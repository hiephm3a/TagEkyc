using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Application.Ports;

public interface IRecipientPackageDeliveryGateway
{
    Task<SessionOperationResult<RecipientPackageDeliveryCreation>> CreateAsync(
        AuthenticatedClientContext actor,
        Guid packageId,
        string idempotencyKey,
        byte[] correlationDigest,
        CancellationToken cancellationToken);

    Task<SessionOperationResult<RecipientPackageDeliveryDto>> ReadAsync(
        AuthenticatedClientContext actor,
        Guid deliveryId,
        CancellationToken cancellationToken);

    Task<SessionOperationResult<RecipientPackageDeliveryContentLease>> PrepareContentAsync(
        AuthenticatedClientContext actor,
        Guid deliveryId,
        byte[] correlationDigest,
        CancellationToken cancellationToken);
}

public sealed record RecipientPackageDeliveryCreation(RecipientPackageDeliveryDto Delivery, bool Created);

public sealed class RecipientPackageDeliveryContentLease : IAsyncDisposable
{
    private readonly Func<long, byte[], CancellationToken, Task> complete;
    private readonly Func<string, long?, byte[]?, CancellationToken, Task> interrupt;
    private int terminalized;

    public RecipientPackageDeliveryContentLease(
        Stream content,
        RecipientPackageDeliveryDto delivery,
        Func<long, byte[], CancellationToken, Task> complete,
        Func<string, long?, byte[]?, CancellationToken, Task> interrupt)
    {
        Content = content;
        Delivery = delivery;
        this.complete = complete;
        this.interrupt = interrupt;
    }

    public Stream Content { get; }
    public RecipientPackageDeliveryDto Delivery { get; }

    public async Task CompleteAsync(long length, byte[] digest, CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref terminalized, 1) != 0)
            throw new InvalidOperationException("DELIVERY_LEASE_ALREADY_TERMINALIZED");
        await complete(length, digest, cancellationToken).ConfigureAwait(false);
    }

    public async Task InterruptAsync(
        string kind,
        long? observedLength,
        byte[]? observedDigest,
        CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref terminalized, 1) != 0)
            return;
        await interrupt(kind, observedLength, observedDigest, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync().ConfigureAwait(false);
    }
}

public interface IRecipientPackageDeliveryApplicationService
{
    Task<SessionOperationResult<RecipientPackageDeliveryCreation>> CreateAsync(
        AuthenticatedClientContext actor,
        Guid packageId,
        string? idempotencyKey,
        string? traceIdentifier,
        CancellationToken cancellationToken);

    Task<SessionOperationResult<RecipientPackageDeliveryDto>> ReadAsync(
        AuthenticatedClientContext actor,
        Guid deliveryId,
        CancellationToken cancellationToken);

    Task<SessionOperationResult<RecipientPackageDeliveryContentLease>> PrepareContentAsync(
        AuthenticatedClientContext actor,
        Guid deliveryId,
        string? traceIdentifier,
        CancellationToken cancellationToken);
}
