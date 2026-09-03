using System.Security.Cryptography;
using System.Text;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Application.RawExport;

public sealed class RecipientPackageDeliveryApplicationService(IRecipientPackageDeliveryGateway gateway)
    : IRecipientPackageDeliveryApplicationService
{
    public const string RequiredScope = "business.raw-export.package.download";
    public Task<SessionOperationResult<RecipientPackageDeliveryCreation>> CreateAsync(
        AuthenticatedClientContext actor,
        Guid packageId,
        string? idempotencyKey,
        string? traceIdentifier,
        CancellationToken cancellationToken)
    {
        var actorError = ValidateActor<RecipientPackageDeliveryCreation>(actor);
        if (actorError is not null) return Task.FromResult(actorError);
        if (packageId == Guid.Empty || !ValidIdempotencyKey(idempotencyKey))
            return Task.FromResult(Failure<RecipientPackageDeliveryCreation>(RecipientPackageDeliveryErrorCodes.RequestInvalid, "Package delivery request is invalid.", 400));
        var correlation = Correlation(traceIdentifier);
        if (correlation is null)
            return Task.FromResult(Failure<RecipientPackageDeliveryCreation>(RecipientPackageDeliveryErrorCodes.Unavailable, "Package delivery is temporarily unavailable.", 503));
        return gateway.CreateAsync(actor, packageId, idempotencyKey!, correlation, cancellationToken);
    }

    public Task<SessionOperationResult<RecipientPackageDeliveryDto>> ReadAsync(
        AuthenticatedClientContext actor,
        Guid deliveryId,
        CancellationToken cancellationToken)
    {
        var actorError = ValidateActor<RecipientPackageDeliveryDto>(actor);
        if (actorError is not null) return Task.FromResult(actorError);
        if (deliveryId == Guid.Empty)
            return Task.FromResult(Failure<RecipientPackageDeliveryDto>(RecipientPackageDeliveryErrorCodes.NotFound, "Package delivery was not found.", 404));
        return gateway.ReadAsync(actor, deliveryId, cancellationToken);
    }

    public Task<SessionOperationResult<RecipientPackageDeliveryContentLease>> PrepareContentAsync(
        AuthenticatedClientContext actor,
        Guid deliveryId,
        string? traceIdentifier,
        CancellationToken cancellationToken)
    {
        var actorError = ValidateActor<RecipientPackageDeliveryContentLease>(actor);
        if (actorError is not null) return Task.FromResult(actorError);
        if (deliveryId == Guid.Empty)
            return Task.FromResult(Failure<RecipientPackageDeliveryContentLease>(RecipientPackageDeliveryErrorCodes.NotFound, "Package delivery was not found.", 404));
        var correlation = Correlation(traceIdentifier);
        if (correlation is null)
            return Task.FromResult(Failure<RecipientPackageDeliveryContentLease>(RecipientPackageDeliveryErrorCodes.Unavailable, "Package delivery is temporarily unavailable.", 503));
        return gateway.PrepareContentAsync(actor, deliveryId, correlation, cancellationToken);
    }

    private static SessionOperationResult<T>? ValidateActor<T>(AuthenticatedClientContext actor)
    {
        if (actor.CallerCategory != AuthenticatedCallerCategory.BusinessConsumer)
            return Failure<T>(RecipientPackageDeliveryErrorCodes.Forbidden, "Package delivery is not authorized.", 403);
        if (actor.PrincipalId == Guid.Empty)
            return Failure<T>(RecipientPackageDeliveryErrorCodes.PrincipalRequired, "Authenticated principal is not eligible for package delivery.", 403);
        if (!actor.Scopes.Contains(RequiredScope))
            return Failure<T>(RecipientPackageDeliveryErrorCodes.Forbidden, "Package delivery is not authorized.", 403);
        return null;
    }

    private static bool ValidIdempotencyKey(string? value) => value is { Length: >= 1 and <= 128 }
        && Encoding.ASCII.GetByteCount(value) == value.Length
        && value.All(character => character is >= '!' and <= '~')
        && !value.Contains(',', StringComparison.Ordinal);

    private static byte[]? Correlation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Normalize(NormalizationForm.FormC);
        var label = Encoding.UTF8.GetBytes("tip-88c1-c3-request-correlation-v1");
        var scalar = Encoding.UTF8.GetBytes(normalized);
        try
        {
            var preimage = new byte[checked(4 + label.Length + 4 + scalar.Length)];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(preimage, label.Length);
            label.CopyTo(preimage.AsSpan(4));
            var offset = 4 + label.Length;
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(preimage.AsSpan(offset), scalar.Length);
            scalar.CopyTo(preimage.AsSpan(offset + 4));
            return SHA256.HashData(preimage);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(label);
            CryptographicOperations.ZeroMemory(scalar);
        }
    }

    private static SessionOperationResult<T> Failure<T>(string code, string message, int status) =>
        SessionOperationResult<T>.Failure(code, message, status);
}
