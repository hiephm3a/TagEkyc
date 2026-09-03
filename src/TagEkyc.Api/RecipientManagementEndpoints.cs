using System.Text.Json;
using System.Text.Json.Serialization;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Api;

public static class RecipientManagementEndpoints
{
    public const string RoutePrefix = "/api/ekyc/raw-export/recipient-management";
    public const string IdempotencyHeader = "Idempotency-Key";

    internal static readonly JsonSerializerOptions RecipientManagementJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static IEndpointRouteBuilder MapRecipientManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(RoutePrefix + "/recipients", EnrollRecipientAsync);
        endpoints.MapPost(RoutePrefix + "/credentials/issue", IssueCredentialAsync);
        endpoints.MapPost(RoutePrefix + "/credentials/replace", ReplaceCredentialAsync);
        endpoints.MapPost(RoutePrefix + "/credentials/revoke", RevokeCredentialAsync);
        endpoints.MapPost(RoutePrefix + "/keys/enroll", EnrollKeyAsync);
        endpoints.MapPost(RoutePrefix + "/keys/rotate", RotateKeyAsync);
        endpoints.MapPost(RoutePrefix + "/keys/revoke", RevokeKeyAsync);
        endpoints.MapGet(RoutePrefix + "/recipients/{recipientClientApplicationId:guid}/readiness", ReadReadinessAsync);
        return endpoints;
    }

    private static Task<IResult> EnrollRecipientAsync(
        HttpContext context, IApiKeyAuthenticator authenticator,
        IRecipientManagementApplicationService service, CancellationToken cancellationToken) =>
        HandlePostAsync<EnrollManagedRecipientRequest, ManagedRecipientIdentityDto>(
            context, authenticator, cancellationToken,
            (actor, request, key) => service.EnrollRecipientAsync(actor, request, key, cancellationToken));

    private static Task<IResult> IssueCredentialAsync(
        HttpContext context, IApiKeyAuthenticator authenticator,
        IRecipientManagementApplicationService service, CancellationToken cancellationToken) =>
        HandlePostAsync<IssueManagedRecipientCredentialRequest, ManagedCredentialDto>(
            context, authenticator, cancellationToken,
            (actor, request, key) => service.IssueCredentialAsync(actor, request, key, cancellationToken));

    private static Task<IResult> ReplaceCredentialAsync(
        HttpContext context, IApiKeyAuthenticator authenticator,
        IRecipientManagementApplicationService service, CancellationToken cancellationToken) =>
        HandlePostAsync<ReplaceManagedRecipientCredentialRequest, ManagedCredentialDto>(
            context, authenticator, cancellationToken,
            (actor, request, key) => service.ReplaceCredentialAsync(actor, request, key, cancellationToken));

    private static Task<IResult> RevokeCredentialAsync(
        HttpContext context, IApiKeyAuthenticator authenticator,
        IRecipientManagementApplicationService service, CancellationToken cancellationToken) =>
        HandlePostAsync<RevokeManagedRecipientCredentialRequest, ManagedCredentialDto>(
            context, authenticator, cancellationToken,
            (actor, request, key) => service.RevokeCredentialAsync(actor, request, key, cancellationToken));

    private static Task<IResult> EnrollKeyAsync(
        HttpContext context, IApiKeyAuthenticator authenticator,
        IRecipientManagementApplicationService service, CancellationToken cancellationToken) =>
        HandlePostAsync<EnrollRecipientPublicKeyRequest, RecipientPublicKeyOperationResultDto>(
            context, authenticator, cancellationToken,
            (actor, request, key) => service.EnrollKeyAsync(actor, request, key, cancellationToken));

    private static Task<IResult> RotateKeyAsync(
        HttpContext context, IApiKeyAuthenticator authenticator,
        IRecipientManagementApplicationService service, CancellationToken cancellationToken) =>
        HandlePostAsync<RotateRecipientPublicKeyRequest, RecipientPublicKeyOperationResultDto>(
            context, authenticator, cancellationToken,
            (actor, request, key) => service.RotateKeyAsync(actor, request, key, cancellationToken));

    private static Task<IResult> RevokeKeyAsync(
        HttpContext context, IApiKeyAuthenticator authenticator,
        IRecipientManagementApplicationService service, CancellationToken cancellationToken) =>
        HandlePostAsync<RevokeRecipientPublicKeyRequest, RecipientPublicKeyOperationResultDto>(
            context, authenticator, cancellationToken,
            (actor, request, key) => service.RevokeKeyAsync(actor, request, key, cancellationToken));

    private static async Task<IResult> ReadReadinessAsync(
        HttpContext context,
        Guid recipientClientApplicationId,
        IApiKeyAuthenticator authenticator,
        IRecipientManagementApplicationService service,
        CancellationToken cancellationToken)
    {
        var auth = await AuthenticateAsync(context, authenticator, cancellationToken).ConfigureAwait(false);
        if (!auth.IsSuccess) return Forbidden(context.TraceIdentifier);
        var result = await service.ReadReadinessAsync(
            auth.Value!, recipientClientApplicationId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Error(result.Error!, context.TraceIdentifier);
    }

    private static async Task<IResult> HandlePostAsync<TRequest, TResponse>(
        HttpContext context,
        IApiKeyAuthenticator authenticator,
        CancellationToken cancellationToken,
        Func<Application.AuthenticatedClientContext, TRequest, string?,
            Task<SessionOperationResult<RecipientManagementWriteResult<TResponse>>>> operation)
    {
        var auth = await AuthenticateAsync(context, authenticator, cancellationToken).ConfigureAwait(false);
        if (!auth.IsSuccess || !IsAuthorizedManager(auth.Value!))
            return Forbidden(context.TraceIdentifier);

        string? idempotencyKey = null;
        if (context.Request.Headers.TryGetValue(IdempotencyHeader, out var values)
            && values.Count == 1)
            idempotencyKey = values[0];

        TRequest? request;
        try
        {
            request = await context.Request.ReadFromJsonAsync<TRequest>(
                RecipientManagementJsonOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return RequestInvalid(context.TraceIdentifier);
        }
        catch (BadHttpRequestException)
        {
            return RequestInvalid(context.TraceIdentifier);
        }
        if (request is null) return RequestInvalid(context.TraceIdentifier);

        var result = await operation(auth.Value!, request, idempotencyKey).ConfigureAwait(false);
        if (!result.IsSuccess) return Error(result.Error!, context.TraceIdentifier);
        return Results.Json(result.Value!.Value, statusCode: result.Value.StatusCode);
    }

    private static Task<SessionOperationResult<Application.AuthenticatedClientContext>> AuthenticateAsync(
        HttpContext context, IApiKeyAuthenticator authenticator, CancellationToken cancellationToken) =>
        authenticator.AuthenticateAsync(
            context, RecipientManagementApplicationService.RequiredScope, cancellationToken);

    private static bool IsAuthorizedManager(Application.AuthenticatedClientContext actor) =>
        actor.CallerCategory == Application.AuthenticatedCallerCategory.OperatorAdmin
        && actor.PrincipalId != Guid.Empty
        && actor.ApiKeyId != Guid.Empty
        && actor.Scopes.Contains(RecipientManagementApplicationService.RequiredScope);

    private static IResult Forbidden(string correlationId) => Results.Json(
        new ErrorEnvelope(new ErrorBody(
            RecipientManagementErrorCodes.Forbidden,
            "Recipient management is not authorized.", correlationId)), statusCode: 403);

    private static IResult RequestInvalid(string correlationId) => Results.Json(
        new ErrorEnvelope(new ErrorBody(
            RecipientManagementErrorCodes.RequestInvalid,
            "Recipient management request is invalid.", correlationId)), statusCode: 400);

    private static IResult Error(SessionOperationError error, string correlationId) => Results.Json(
        new ErrorEnvelope(new ErrorBody(error.Code, error.Message, correlationId)),
        statusCode: error.StatusCode);

    private sealed record ErrorEnvelope(ErrorBody Error);
    private sealed record ErrorBody(string Code, string Message, string CorrelationId);
}
