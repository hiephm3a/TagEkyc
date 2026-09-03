using System.Security.Cryptography;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RecipientPackageDeliveryCoordinator(
    RecipientPackageDeliveryOptions options,
    RecipientPackageDeliveryRepository repository,
    IRecipientPackageDeliveryReader reader,
    RecipientPackageDeliverySpoolPool spoolPool) : IRecipientPackageDeliveryGateway
{
    public async Task<SessionOperationResult<RecipientPackageDeliveryCreation>> CreateAsync(
        AuthenticatedClientContext actor, Guid packageId, string idempotencyKey,
        byte[] correlationDigest, CancellationToken cancellationToken)
    {
        if (!Ready()) return Failure<RecipientPackageDeliveryCreation>("Unavailable");
        var digest = RecipientPackageDeliveryCodec.IdempotencyKeyDigest(idempotencyKey);
        try
        {
            var deliveryId = RecipientPackageDeliveryCodec.DeliveryId(actor.ClientApplicationId, digest);
            var result = await repository.CreateAsync(actor.ClientApplicationId, packageId, deliveryId, digest,
                actor.ApiKeyId, actor.PrincipalId, correlationDigest, cancellationToken).ConfigureAwait(false);
            return result.Delivery is not null && result.Outcome is "Created" or "ExistingMatch"
                ? SessionOperationResult<RecipientPackageDeliveryCreation>.Success(
                    new(ToDto(result.Delivery), result.Outcome == "Created"))
                : Failure<RecipientPackageDeliveryCreation>(result.Outcome);
        }
        finally { CryptographicOperations.ZeroMemory(digest); }
    }

    public async Task<SessionOperationResult<RecipientPackageDeliveryDto>> ReadAsync(
        AuthenticatedClientContext actor, Guid deliveryId, CancellationToken cancellationToken)
    {
        if (!Ready()) return Failure<RecipientPackageDeliveryDto>("Unavailable");
        return SuccessOrFailure(await repository.ReadAsync(actor.ClientApplicationId, deliveryId, cancellationToken).ConfigureAwait(false));
    }

    public async Task<SessionOperationResult<RecipientPackageDeliveryContentLease>> PrepareContentAsync(
        AuthenticatedClientContext actor, Guid deliveryId, byte[] correlationDigest,
        CancellationToken cancellationToken)
    {
        if (!Ready()) return Failure<RecipientPackageDeliveryContentLease>("Unavailable");
        var probe = await repository.ProbeContentAsync(actor.ClientApplicationId, deliveryId, cancellationToken).ConfigureAwait(false);
        if (probe.Outcome != "Authorized" && probe.Outcome != "Interrupted")
            return Failure<RecipientPackageDeliveryContentLease>(probe.Outcome);

        var permit = await spoolPool.TryAcquireAsync(cancellationToken).ConfigureAwait(false);
        if (permit is null)
            return SessionOperationResult<RecipientPackageDeliveryContentLease>.Failure(
                RecipientPackageDeliveryErrorCodes.CapacityUnavailable,
                "Package delivery capacity is temporarily unavailable.", 503);

        var ownershipTransferred = false;
        try
        {
            var begun = await repository.BeginAsync(actor.ClientApplicationId, deliveryId, actor.ApiKeyId,
                actor.PrincipalId, correlationDigest, cancellationToken).ConfigureAwait(false);
            if (begun.Outcome != "Started" || begun.Delivery is null)
                return Failure<RecipientPackageDeliveryContentLease>(begun.Outcome);
            var delivery = begun.Delivery;
            var provider = options.Provider!;
            var endpoint = RecipientPackageCodec.ProviderEndpointFingerprint(provider);
            var expectedBinding = RecipientPackageCodec.ObjectBindingDigest(
                delivery.ProviderConfigurationId, endpoint, delivery.BucketName, delivery.ObjectKey);
            try
            {
                if (!CryptographicOperations.FixedTimeEquals(expectedBinding, delivery.ObjectBindingDigest))
                    return await IntegrityAsync(delivery, permit,
                        "ObjectBindingMismatch", null, null, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(endpoint);
                CryptographicOperations.ZeroMemory(expectedBinding);
            }

            var opened = await reader.OpenExactAsync(new(
                delivery.ProviderConfigurationId, provider.ServiceUrl, provider.RegionIdentifier,
                provider.ForcePathStyle, delivery.BucketName, delivery.ObjectKey,
                delivery.ObjectBindingDigest), cancellationToken).ConfigureAwait(false);
            if (opened.Outcome == RecipientPackageDeliveryReadOutcome.PositivelyAbsent)
                return await IntegrityAsync(delivery, permit,
                    "ObjectAbsent", 0, null, null, cancellationToken).ConfigureAwait(false);
            if (opened.Outcome != RecipientPackageDeliveryReadOutcome.Opened || opened.Content is null)
            {
                await InterruptAsync(delivery, "ProviderUnavailable", null, null, null, cancellationToken).ConfigureAwait(false);
                return Failure<RecipientPackageDeliveryContentLease>("Unavailable");
            }

            await using (opened.Content.ConfigureAwait(false))
            {
                var buffer = new byte[64 * 1024];
                long total = 0;
                try
                {
                    while (true)
                    {
                        var read = await opened.Content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                        if (read == 0) break;
                        total = checked(total + read);
                        if (total > RecipientPackageDeliveryOptions.MaximumEncryptedPackageLength
                            || total > delivery.EncryptedPackageLength)
                            return await IntegrityAsync(delivery, permit,
                                "LengthMismatch", total, null, null, cancellationToken).ConfigureAwait(false);
                        await permit.Spool.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    }
                }
                finally { CryptographicOperations.ZeroMemory(buffer); }

                permit.Spool.Seal();
                var digest = permit.Spool.PackageCiphertextDigest;
                if (total != delivery.EncryptedPackageLength
                    || !CryptographicOperations.FixedTimeEquals(digest, delivery.PackageCiphertextDigest))
                    return await IntegrityAsync(delivery, permit,
                        "CiphertextMismatch", total, digest, null, cancellationToken).ConfigureAwait(false);
                using var verify = permit.Spool.OpenRead();
                if (!RecipientPackageCodec.TryReadEnvelopeDigest(verify, out var envelope))
                    return await IntegrityAsync(delivery, permit,
                        "EnvelopeMalformed", total, digest, null, cancellationToken).ConfigureAwait(false);
                try
                {
                    if (!CryptographicOperations.FixedTimeEquals(envelope, delivery.EnvelopeDigest))
                        return await IntegrityAsync(delivery, permit,
                            "EnvelopeMismatch", total, digest, envelope, cancellationToken).ConfigureAwait(false);
                }
                finally { CryptographicOperations.ZeroMemory(envelope); CryptographicOperations.ZeroMemory(digest); }
            }

            var stream = new OwnedSpoolStream(permit.Spool.OpenRead(), permit);
            ownershipTransferred = true;
            return SessionOperationResult<RecipientPackageDeliveryContentLease>.Success(new(
                stream, ToDto(delivery),
                async (length, digest, token) =>
                {
                    var completed = await repository.CompleteAsync(delivery.DeliveryId, delivery.Revision,
                        delivery.DeliveryFence, length, digest, token).ConfigureAwait(false);
                    if (completed.Outcome is not ("Completed" or "ExistingMatch"))
                        throw new InvalidOperationException("RAW_EXPORT_PACKAGE_DELIVERY_COMPLETION_FAILED");
                    if (completed.Delivery is null || completed.Delivery.StreamStartedAtUtc is null
                        || completed.Delivery.ServerStreamCompletedAtUtc is null
                        || completed.Delivery.DeliveryReceiptDigest is null)
                        throw new InvalidOperationException("RAW_EXPORT_PACKAGE_DELIVERY_RECEIPT_INVALID");
                    var expectedReceipt = RecipientPackageDeliveryCodec.ReceiptDigest(
                        completed.Delivery.DeliveryId, completed.Delivery.PackageId,
                        completed.Delivery.RecipientClientApplicationId, completed.Delivery.StreamAttemptCount,
                        completed.Delivery.DeliveryFence, completed.Delivery.PackageCiphertextDigest,
                        completed.Delivery.EncryptedPackageLength, completed.Delivery.AuthorizedAtUtc,
                        completed.Delivery.StreamStartedAtUtc.Value,
                        completed.Delivery.ServerStreamCompletedAtUtc.Value, actor.ApiKeyId, actor.PrincipalId);
                    try
                    {
                        if (!CryptographicOperations.FixedTimeEquals(
                            expectedReceipt, completed.Delivery.DeliveryReceiptDigest))
                            throw new InvalidOperationException("RAW_EXPORT_PACKAGE_DELIVERY_RECEIPT_INVALID");
                    }
                    finally { CryptographicOperations.ZeroMemory(expectedReceipt); }
                },
                (kind, observedLength, observedDigest, token) =>
                    InterruptAsync(delivery, kind, observedLength, observedDigest, null, token)));
        }
        finally
        {
            if (!ownershipTransferred) await permit.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<SessionOperationResult<RecipientPackageDeliveryContentLease>> IntegrityAsync(
        RecipientPackageDeliveryProjection delivery, RecipientPackageDeliverySpoolPool.Lease permit,
        string kind, long? observedLength, byte[]? observedDigest, byte[]? observedEnvelopeDigest,
        CancellationToken cancellationToken)
    {
        var observations = RecipientPackageDeliveryCodec.EncodeFailureObservations(
            observedLength, observedDigest ?? [], observedDigest is not null,
            observedEnvelopeDigest ?? [], observedEnvelopeDigest is not null);
        try { await repository.IntegrityUnavailableAsync(delivery.DeliveryId, delivery.Revision,
            delivery.DeliveryFence, kind, observations, cancellationToken).ConfigureAwait(false); }
        finally { CryptographicOperations.ZeroMemory(observations); }
        return Failure<RecipientPackageDeliveryContentLease>("IntegrityUnavailable");
    }

    private async Task InterruptAsync(
        RecipientPackageDeliveryProjection delivery, string kind, long? observedLength,
        byte[]? observedDigest, byte[]? observedEnvelopeDigest, CancellationToken cancellationToken)
    {
        var observations = RecipientPackageDeliveryCodec.EncodeFailureObservations(
            observedLength, observedDigest ?? [], observedDigest is not null,
            observedEnvelopeDigest ?? [], observedEnvelopeDigest is not null);
        try { await repository.InterruptAsync(delivery.DeliveryId, delivery.Revision, delivery.DeliveryFence,
            kind, observations, cancellationToken).ConfigureAwait(false); }
        finally { CryptographicOperations.ZeroMemory(observations); }
    }

    private bool Ready() => options.Topology == RecipientPackageDeliveryTopology.S3CompatibleDurable
        && options.IsSyntacticallyValid && options.Provider is not null;

    private static SessionOperationResult<RecipientPackageDeliveryDto> SuccessOrFailure(RecipientPackageDeliveryMutation result) =>
        result.Delivery is not null && result.Outcome is "Created" or "ExistingMatch"
            ? SessionOperationResult<RecipientPackageDeliveryDto>.Success(ToDto(result.Delivery))
            : Failure<RecipientPackageDeliveryDto>(result.Outcome);

    private static RecipientPackageDeliveryDto ToDto(RecipientPackageDeliveryProjection row) => new(
        row.DeliveryId, row.PackageId, row.State, row.AuthorizedAtUtc, row.AuthorizationExpiresAtUtc,
        row.StreamAttemptCount, row.ServerStreamCompletedAtUtc, row.EncryptedPackageLength,
        RecipientPackageDeliveryCodec.Base64Url(row.PackageCiphertextDigest),
        row.DeliveryReceiptDigest is null ? null : RecipientPackageDeliveryCodec.Base64Url(row.DeliveryReceiptDigest));

    private static SessionOperationResult<T> Failure<T>(string outcome)
    {
        var error = RecipientPackageDeliveryErrors.ByOutcome.TryGetValue(outcome, out var mapped)
            ? mapped : RecipientPackageDeliveryErrors.ByOutcome["Unavailable"];
        return SessionOperationResult<T>.Failure(error.Code, error.Message, error.Status);
    }

    private sealed class OwnedSpoolStream(Stream inner, RecipientPackageDeliverySpoolPool.Lease owner) : Stream
    {
        public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false;
        public override long Length => inner.Length; public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken=default) => inner.ReadAsync(buffer,cancellationToken);
        public override void Flush()=>throw new NotSupportedException(); public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();
        public override void SetLength(long v)=>throw new NotSupportedException(); public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if(disposing){inner.Dispose(); owner.DisposeAsync().AsTask().GetAwaiter().GetResult();} base.Dispose(disposing); }
        public override async ValueTask DisposeAsync(){await inner.DisposeAsync().ConfigureAwait(false);await owner.DisposeAsync().ConfigureAwait(false);GC.SuppressFinalize(this);}
    }
}
