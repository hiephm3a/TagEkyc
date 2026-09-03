using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

public static class RecipientPackageReferenceServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycRecipientPackageReference(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = RecipientPackageReferenceOptions.Resolve(configuration);
        services.TryAddSingleton(configuration);
        services.TryAddSingleton(options);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<RecipientPackageReferenceReadinessValidator>();
        if (options.Topology == RecipientPackageReferenceTopology.PostgresDurable && options.IsSyntacticallyValid)
        {
            services.TryAddScoped<IRecipientPackageReferenceConnectionFactory, RecipientPackageReferenceConnectionFactory>();
            services.TryAddScoped<RecipientPackageReferenceRepository>();
            services.TryAddScoped<RecipientPackageReferenceCursorKeyService>();
            services.TryAddScoped<IRecipientPackageReferenceGateway, RecipientPackageReferenceGateway>();
        }
        else
        {
            services.TryAddSingleton<IRecipientPackageReferenceGateway, UnavailableRecipientPackageReferenceGateway>();
        }
        services.TryAddScoped<RecipientPackageReferenceApplicationService>();
        services.TryAddScoped<IRecipientPackageReferenceApplicationService>(provider =>
            provider.GetRequiredService<RecipientPackageReferenceApplicationService>());
        return services;
    }

    private sealed class UnavailableRecipientPackageReferenceGateway : IRecipientPackageReferenceGateway
    {
        public Task<SessionOperationResult<RecipientPackageReferencePageDto>> ListAsync(
            AuthenticatedClientContext actor, RecipientPackageReferenceRawQuery query,
            CancellationToken cancellationToken)
        {
            var shape = RecipientPackageReferenceGateway.Classify(query);
            if (shape.Error is not null) return Task.FromResult(shape.Error);
            if (shape.Cursor is not null)
            {
                var parsed = RecipientPackageReferenceCursorCodec.ParseStructure(shape.Cursor);
                try
                {
                    if (parsed.Outcome == RecipientPackageReferenceCursorParseOutcome.Invalid)
                        return Task.FromResult(RecipientPackageReferenceGateway.CursorInvalidResult());
                }
                finally { RecipientPackageReferenceCursorCodec.Release(parsed); }
            }
            return Task.FromResult(RecipientPackageReferenceGateway.Unavailable());
        }
    }
}

internal sealed class RecipientPackageReferenceGateway(
    RecipientPackageReferenceOptions options,
    RecipientPackageReferenceCursorKeyService keys,
    RecipientPackageReferenceRepository repository,
    TimeProvider timeProvider) : IRecipientPackageReferenceGateway
{
    public async Task<SessionOperationResult<RecipientPackageReferencePageDto>> ListAsync(
        AuthenticatedClientContext actor,
        RecipientPackageReferenceRawQuery query,
        CancellationToken cancellationToken)
    {
        var shape = Classify(query);
        if (shape.Error is not null) return shape.Error;
        var parsed = shape.Cursor is null ? null : RecipientPackageReferenceCursorCodec.ParseStructure(shape.Cursor);
        try
        {
            if (parsed is { Outcome: RecipientPackageReferenceCursorParseOutcome.Invalid }) return CursorInvalid();
            if (options.Topology != RecipientPackageReferenceTopology.PostgresDurable || !options.IsSyntacticallyValid
                || options.ActiveKey is null) return Unavailable();

            var cursor = parsed?.Cursor;
            var tokenIdentity = cursor is null ? null : new RecipientPackageReferenceKeyIdentity(cursor.KeyId, cursor.KeyVersion);
            if (tokenIdentity is not null && !options.AcceptedKeys.Contains(tokenIdentity)) return CursorInvalid();

            var requestClock = timeProvider.GetUtcNow();
            using var tokenKey = tokenIdentity is null ? null : await keys.ResolveAsync(tokenIdentity, cancellationToken).ConfigureAwait(false);
            using var activeKey = tokenIdentity == options.ActiveKey
                ? null : await keys.ResolveAsync(options.ActiveKey, cancellationToken).ConfigureAwait(false);
            if (tokenIdentity is not null && tokenKey is null) return Unavailable();
            if (tokenIdentity != options.ActiveKey && activeKey is null) return Unavailable();
            var verificationKey = tokenKey is not null ? tokenKey.Material : activeKey!.Material;
            var mintKey = activeKey is not null ? activeKey.Material : tokenKey!.Material;
            if (verificationKey.Length != 32 || mintKey.Length != 32) return Unavailable();

            var now = requestClock.ToUnixTimeSeconds();
            if (cursor is not null)
            {
                if (!RecipientPackageReferenceCursorCodec.Verify(parsed!, verificationKey)
                    || cursor.ClientApplicationId != actor.ClientApplicationId
                    || cursor.PrincipalId != actor.PrincipalId
                    || cursor.IssuedAtUnixSeconds > now || now >= cursor.ExpiresAtUnixSeconds
                    || (shape.PageSize is not null && shape.PageSize != cursor.PageSize)) return CursorInvalid();
            }
            var pageSize = cursor?.PageSize ?? shape.PageSize!.Value;
            IReadOnlyList<RecipientPackageReferenceRow> rows;
            try
            {
                rows = await repository.ListAsync(actor.ClientApplicationId,
                    cursor?.BoundaryFinalizedAtUtc, cursor?.BoundaryPackageId,
                    pageSize, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { return Unavailable(); }

            var hasMore = rows.Count > pageSize;
            var emitted = rows.Take(pageSize).Select(row =>
                new RecipientPackageReferenceDto(row.PackageId, row.FinalizedAtUtc)).ToArray();
            string? next = null;
            if (hasMore && emitted.Length != 0)
            {
                var last = emitted[^1];
                next = RecipientPackageReferenceCursorCodec.Encode(new RecipientPackageReferenceCursor(
                    options.ActiveKey.KeyId, options.ActiveKey.KeyVersion,
                    actor.ClientApplicationId, actor.PrincipalId, pageSize,
                    last.FinalizedAtUtc, last.PackageId, now, checked(now + 900)), mintKey);
            }
            return SessionOperationResult<RecipientPackageReferencePageDto>.Success(new(emitted, next));
        }
        finally
        {
            if (parsed is not null) RecipientPackageReferenceCursorCodec.Release(parsed);
        }
    }

    internal static QueryShape Classify(RecipientPackageReferenceRawQuery query)
    {
        if (query.Keys.Any(key => key is not ("pageSize" or "cursor"))) return new(null, null, RequestInvalid());
        var continuation = query.CursorValues.Count != 0;
        if (continuation && (query.CursorValues.Count != 1 || string.IsNullOrEmpty(query.CursorValues[0])))
            return new(null, null, CursorInvalid());
        if (!continuation)
        {
            if (query.PageSizeValues.Count == 0) return new(25, null, null);
            if (query.PageSizeValues.Count != 1 || !TryPageSize(query.PageSizeValues[0], out var size))
                return new(null, null, RequestInvalid());
            return new(size, null, null);
        }
        if (query.PageSizeValues.Count > 1
            || (query.PageSizeValues.Count == 1 && !TryPageSize(query.PageSizeValues[0], out _)))
            return new(null, null, CursorInvalid());
        return new(query.PageSizeValues.Count == 1 ? int.Parse(query.PageSizeValues[0], System.Globalization.CultureInfo.InvariantCulture) : null,
            query.CursorValues[0], null);
    }

    private static bool TryPageSize(string value, out int result) =>
        int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out result)
        && result is >= 1 and <= 50 && result.ToString(System.Globalization.CultureInfo.InvariantCulture) == value;
    internal static SessionOperationResult<RecipientPackageReferencePageDto> Unavailable() => Failure(
        RecipientPackageReferenceErrorCodes.Unavailable, "Package reference listing is temporarily unavailable.", 503);
    private static SessionOperationResult<RecipientPackageReferencePageDto> RequestInvalid() => Failure(
        RecipientPackageReferenceErrorCodes.RequestInvalid, "Package reference request is invalid.", 400);
    private static SessionOperationResult<RecipientPackageReferencePageDto> CursorInvalid() => Failure(
        RecipientPackageReferenceErrorCodes.CursorInvalid, "Package reference cursor is invalid.", 400);
    internal static SessionOperationResult<RecipientPackageReferencePageDto> CursorInvalidResult() => CursorInvalid();
    private static SessionOperationResult<RecipientPackageReferencePageDto> Failure(string code, string message, int status) =>
        SessionOperationResult<RecipientPackageReferencePageDto>.Failure(code, message, status);
    internal sealed record QueryShape(int? PageSize, string? Cursor, SessionOperationResult<RecipientPackageReferencePageDto>? Error);
}
