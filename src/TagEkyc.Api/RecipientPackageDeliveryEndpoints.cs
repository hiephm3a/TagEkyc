using System.Security.Cryptography;
using Microsoft.AspNetCore.Routing;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Api;

public sealed class DFormatGuidRouteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection) =>
        values.TryGetValue(routeKey, out var raw)
        && raw is not null
        && Guid.TryParseExact(Convert.ToString(raw, System.Globalization.CultureInfo.InvariantCulture), "D", out _);
}

public static class RecipientPackageDeliveryEndpoints
{
    public static IEndpointRouteBuilder MapRecipientPackageDeliveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ekyc/raw-export/packages/{packageId:D}/deliveries", CreateAsync);
        endpoints.MapGet("/api/ekyc/raw-export/deliveries/{deliveryId:D}", ReadAsync);
        endpoints.MapGet("/api/ekyc/raw-export/deliveries/{deliveryId:D}/content", ContentAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        HttpContext context, Guid packageId, IApiKeyAuthenticator authenticator,
        IRecipientPackageDeliveryApplicationService service, CancellationToken cancellationToken)
    {
        var auth = await authenticator.AuthenticateAsync(context,
            RecipientPackageDeliveryApplicationService.RequiredScope, cancellationToken).ConfigureAwait(false);
        if (!auth.IsSuccess) return Error(auth.Error!, context.TraceIdentifier);
        var values = context.Request.Headers["Idempotency-Key"];
        var key = values.Count == 1 ? values[0] : null;
        var result = await service.CreateAsync(auth.Value!, packageId, key, context.TraceIdentifier, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) return Error(result.Error!, context.TraceIdentifier);
        return result.Value!.Created
            ? Results.Created($"/api/ekyc/raw-export/deliveries/{result.Value.Delivery.DeliveryId:D}", result.Value.Delivery)
            : Results.Ok(result.Value.Delivery);
    }

    private static async Task<IResult> ReadAsync(
        HttpContext context, Guid deliveryId, IApiKeyAuthenticator authenticator,
        IRecipientPackageDeliveryApplicationService service, CancellationToken cancellationToken)
    {
        var auth = await authenticator.AuthenticateAsync(context,
            RecipientPackageDeliveryApplicationService.RequiredScope, cancellationToken).ConfigureAwait(false);
        if (!auth.IsSuccess) return Error(auth.Error!, context.TraceIdentifier);
        var result = await service.ReadAsync(auth.Value!, deliveryId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Results.Ok(result.Value) : Error(result.Error!, context.TraceIdentifier);
    }

    private static async Task ContentAsync(
        HttpContext context, Guid deliveryId, IApiKeyAuthenticator authenticator,
        IRecipientPackageDeliveryApplicationService service, CancellationToken cancellationToken)
    {
        var auth = await authenticator.AuthenticateAsync(context,
            RecipientPackageDeliveryApplicationService.RequiredScope, cancellationToken).ConfigureAwait(false);
        if (!auth.IsSuccess) { await WriteErrorAsync(context, auth.Error!, cancellationToken); return; }

        var owned = await service.ReadAsync(auth.Value!, deliveryId, cancellationToken).ConfigureAwait(false);
        if (!owned.IsSuccess) { await WriteErrorAsync(context, owned.Error!, cancellationToken); return; }
        if (context.Request.Headers.ContainsKey("Range") || context.Request.Headers.ContainsKey("If-Range"))
        {
            await WriteErrorAsync(context, new(RecipientPackageDeliveryErrorCodes.RangeNotSupported,
                "Partial package delivery is not supported.", 416), cancellationToken); return;
        }

        var result = await service.PrepareContentAsync(auth.Value!, deliveryId, context.TraceIdentifier, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess) { await WriteErrorAsync(context, result.Error!, cancellationToken); return; }
        await using var lease = result.Value!;
        var delivery = lease.Delivery;
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/octet-stream";
        context.Response.ContentLength = delivery.EncryptedPackageLength;
        context.Response.Headers.ContentDisposition = $"attachment; filename=\"tagekyc-package-{delivery.PackageId:D}.t88pkg\"";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.AcceptRanges = "none";

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[64 * 1024];
        long copied = 0;
        try
        {
            while (true)
            {
                var count = await lease.Content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (count == 0) break;
                hash.AppendData(buffer, 0, count);
                copied = checked(copied + count);
                await context.Response.Body.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
            }
            var digest = hash.GetHashAndReset();
            try { await lease.CompleteAsync(copied, digest, CancellationToken.None).ConfigureAwait(false); }
            finally { CryptographicOperations.ZeroMemory(digest); }
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException)
        {
            await lease.InterruptAsync(exception is OperationCanceledException ? "CopyCancelled" : "CopyFailed",
                copied, null, CancellationToken.None).ConfigureAwait(false);
        }
        finally { CryptographicOperations.ZeroMemory(buffer); }
    }

    private static IResult Error(SessionOperationError error, string correlationId) =>
        Results.Json(new ErrorEnvelope(new ErrorBody(error.Code, error.Message, correlationId)), statusCode: error.StatusCode);

    private static async Task WriteErrorAsync(HttpContext context, SessionOperationError error, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = error.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ErrorEnvelope(new ErrorBody(error.Code, error.Message, context.TraceIdentifier)), cancellationToken);
    }

    private sealed record ErrorEnvelope(ErrorBody Error);
    private sealed record ErrorBody(string Code, string Message, string CorrelationId);
}
