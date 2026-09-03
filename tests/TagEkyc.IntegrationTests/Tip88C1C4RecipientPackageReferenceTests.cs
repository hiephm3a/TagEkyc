using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Api;
using TagEkyc.Api.LocalDev;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C4RecipientPackageReferenceTests(PostgresPersistenceFixture postgres)
{
    private static readonly Guid C4ClientA = Guid.Parse("41000000-0000-0000-0000-000000000001");
    private static readonly Guid C4ClientB = Guid.Parse("41000000-0000-0000-0000-000000000002");
    private static readonly Guid C4Principal1 = Guid.Parse("42000000-0000-0000-0000-000000000001");
    private static readonly Guid C4Principal2 = Guid.Parse("42000000-0000-0000-0000-000000000002");
    private static readonly Guid C4Principal3 = Guid.Parse("42000000-0000-0000-0000-000000000003");

    [Fact]
    public async Task C402_surface_is_one_get_list_route_without_package_lookup_or_download_contract()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiKeyAuthenticator>(new C4Authenticator(Actor(C4ClientA, C4Principal1)));
        builder.Services.AddSingleton<IRecipientPackageReferenceApplicationService>(
            new RecipientPackageReferenceApplicationService(new C401Gateway()));
        await using var app = builder.Build();
        app.MapRecipientPackageReferenceEndpoints();
        await app.StartAsync();

        var routes = app.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.Contains("raw-export/package", StringComparison.Ordinal) == true)
            .ToArray();
        Assert.True(routes.Length == 1, $"C402-ROUTE-CENSUS expected=1 actual={routes.Length}");
        var route = routes[0];
        Assert.True(string.Equals("/api/ekyc/raw-export/package-references", route.RoutePattern.RawText, StringComparison.Ordinal),
            $"C402-ROUTE-PATH actual={route.RoutePattern.RawText ?? "<null>"}");
        var methods = route.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? Array.Empty<string>();
        Assert.True(new[] { "GET" }.SequenceEqual(methods, StringComparer.Ordinal),
            $"C402-ROUTE-METHODS actual=[{string.Join(",", methods)}]");

        AssertExactProperties(typeof(RecipientPackageReferenceDto),
            (nameof(RecipientPackageReferenceDto.PackageId), typeof(Guid)),
            (nameof(RecipientPackageReferenceDto.FinalizedAtUtc), typeof(DateTimeOffset)));
        AssertExactProperties(typeof(RecipientPackageReferencePageDto),
            (nameof(RecipientPackageReferencePageDto.Items), typeof(IReadOnlyList<RecipientPackageReferenceDto>)),
            (nameof(RecipientPackageReferencePageDto.NextCursor), typeof(string)));
        var resultType = typeof(Task<SessionOperationResult<RecipientPackageReferencePageDto>>);
        Type[] parameters = [typeof(AuthenticatedClientContext), typeof(RecipientPackageReferenceRawQuery), typeof(CancellationToken)];
        AssertExactPublicMethod(typeof(IRecipientPackageReferenceApplicationService), "ListAsync", resultType, parameters);
        AssertExactPublicMethod(typeof(IRecipientPackageReferenceGateway), "ListAsync", resultType, parameters);
        AssertExactPublicMethod(typeof(RecipientPackageReferenceApplicationService), "ListAsync", resultType, parameters);
    }

    [Fact]
    public async Task C401_concrete_localdev_identity_is_isolated_non_empty_and_exactly_scoped()
    {
        var store = new LocalDevApiKeyStore();
        var key = Assert.Single(store.ApiKeys,
            value => value.ApiKeyValue == "localdev-recipient-package-reference-key");
        var expectedPrincipalId = Guid.Parse("30000000-0000-0000-0000-000000000011");
        Assert.Equal(Guid.Parse("20000000-0000-0000-0000-000000000011"), key.ApiKeyId);
        Assert.Equal(LocalDevRuntimePolicySource.BusinessClientId, key.ClientApplicationId);
        Assert.Equal(expectedPrincipalId, key.PrincipalId);
        Assert.NotEqual(key.ClientApplicationId, key.PrincipalId);
        Assert.NotEqual(Guid.Empty, key.PrincipalId);
        Assert.Equal("ldev_reference", key.KeyPrefix);
        Assert.True(key.Scopes.SetEquals([RecipientPackageReferenceApplicationService.RequiredScope]));

        var authenticator = new LocalDevApiKeyAuthenticator(
            new LocalDevApiKeyValidator(store, new LocalDevRuntimePolicySource()));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[LocalDevApiKeyAuthenticator.HeaderName] = key.ApiKeyValue;
        var authenticated = await authenticator.AuthenticateAsync(
            httpContext, RecipientPackageReferenceApplicationService.RequiredScope);
        Assert.True(authenticated.IsSuccess);
        Assert.Equal(key.PrincipalId, authenticated.Value!.PrincipalId);
        Assert.NotEqual(authenticated.Value.ClientApplicationId, authenticated.Value.PrincipalId);

        var gateway = new C401Gateway();
        var service = new RecipientPackageReferenceApplicationService(gateway);
        var missingScope = authenticated.Value with { Scopes = new HashSet<string>() };
        var missingScopeResult = await service.ListAsync(missingScope, new([], [], []), default);
        Assert.Equal(RecipientPackageReferenceErrorCodes.Forbidden, missingScopeResult.Error?.Code);
        Assert.Equal(0, gateway.CallCount);

        var wrongCategory = authenticated.Value with { CallerCategory = AuthenticatedCallerCategory.CaptureAgent };
        var wrongCategoryResult = await service.ListAsync(wrongCategory, new([], [], []), default);
        Assert.Equal(RecipientPackageReferenceErrorCodes.Forbidden, wrongCategoryResult.Error?.Code);
        Assert.Equal(0, gateway.CallCount);
    }

    [Fact]
    public async Task C403_cursor_omission_and_present_empty_remain_distinct_at_the_raw_boundary()
    {
        var cursorKey = Convert.ToBase64String(new byte[32]).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var configuration = new Dictionary<string, string?>
        {
            [$"{RecipientPackageReferenceOptions.SectionPath}:Topology"] = "PostgresDurable",
            [$"{RecipientPackageReferenceOptions.SectionPath}:DatabaseConnectionString"] = postgres.ConnectionString,
            [$"{RecipientPackageReferenceOptions.SectionPath}:ActiveCursorKeyId"] = "c4-cursor-01",
            [$"{RecipientPackageReferenceOptions.SectionPath}:ActiveCursorKeyVersion"] = "1",
            [$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyId"] = "c4-cursor-01",
            [$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyVersion"] = "1",
            [$"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:c4-cursor-01:1"] = cursorKey,
        };
        var actor = new AuthenticatedClientContext(Guid.NewGuid(), Guid.NewGuid(), "c403",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { RecipientPackageReferenceApplicationService.RequiredScope },
            PrincipalId: Guid.NewGuid());
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Services.AddSingleton<IApiKeyAuthenticator>(new C4Authenticator(actor));
        builder.Services.AddTagEkycRecipientPackageReference(builder.Configuration);
        await using var app = builder.Build();
        app.MapRecipientPackageReferenceEndpoints();
        await app.StartAsync();
        using var client = app.GetTestClient();

        using var omittedResponse = await client.GetAsync("/api/ekyc/raw-export/package-references?pageSize=25");
        using var emptyResponse = await client.GetAsync("/api/ekyc/raw-export/package-references?pageSize=25&cursor=");
        using var emptyBody = JsonDocument.Parse(await emptyResponse.Content.ReadAsStringAsync());
        var emptyCode = emptyBody.RootElement.GetProperty("error").GetProperty("code").GetString();

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.OK, omittedResponse.StatusCode),
            () => Assert.Equal(HttpStatusCode.BadRequest, emptyResponse.StatusCode),
            () => Assert.Equal(RecipientPackageReferenceErrorCodes.CursorInvalid, emptyCode));
    }

    [Fact]
    public async Task C404_page_size_cardinality_and_continuation_values_are_not_model_bound_away()
    {
        await SeedC4PackagesAsync(60);
        try
        {
            await using var app = await StartC4HostAsync(Actor(C4ClientA, C4Principal1));
            using var client = app.GetTestClient();
            var firstDefault = await ObserveAsync(client, "/api/ekyc/raw-export/package-references");
            var firstOne = await ObserveAsync(client, "/api/ekyc/raw-export/package-references?pageSize=1");
            var firstFifty = await ObserveAsync(client, "/api/ekyc/raw-export/package-references?pageSize=50");
            var firstZero = await ObserveAsync(client, "/api/ekyc/raw-export/package-references?pageSize=0");
            var firstFiftyOne = await ObserveAsync(client, "/api/ekyc/raw-export/package-references?pageSize=51");
            var firstLeadingZero = await ObserveAsync(client, "/api/ekyc/raw-export/package-references?pageSize=05");
            var firstMalformed = await ObserveAsync(client, "/api/ekyc/raw-export/package-references?pageSize=bad");
            var firstDuplicate = await ObserveAsync(client, "/api/ekyc/raw-export/package-references?pageSize=25&pageSize=25");
            var firstTwo = await ObserveAsync(client, "/api/ekyc/raw-export/package-references?pageSize=2");
            var cursor = firstTwo.NextCursor ?? throw new InvalidOperationException("C404 first page did not mint a cursor.");
            var encoded = Uri.EscapeDataString(cursor);
            var continuationOmitted = await ObserveAsync(client, $"/api/ekyc/raw-export/package-references?cursor={encoded}");
            var continuationTwo = await ObserveAsync(client, $"/api/ekyc/raw-export/package-references?cursor={encoded}&pageSize=2");
            var continuationDuplicate = await ObserveAsync(client, $"/api/ekyc/raw-export/package-references?cursor={encoded}&pageSize=2&pageSize=2");
            var continuationMismatch = await ObserveAsync(client, $"/api/ekyc/raw-export/package-references?cursor={encoded}&pageSize=3");
            var continuationZero = await ObserveAsync(client, $"/api/ekyc/raw-export/package-references?cursor={encoded}&pageSize=0");
            var continuationFiftyOne = await ObserveAsync(client, $"/api/ekyc/raw-export/package-references?cursor={encoded}&pageSize=51");
            var continuationLeadingZero = await ObserveAsync(client, $"/api/ekyc/raw-export/package-references?cursor={encoded}&pageSize=02");
            var continuationMalformed = await ObserveAsync(client, $"/api/ekyc/raw-export/package-references?cursor={encoded}&pageSize=bad");

            Assert.Multiple(
                () => Assert.True(firstDefault.IsPage(25, cursor: true), $"firstPageDefault=[{firstDefault}]"),
                () => Assert.True(firstOne.IsPage(1, cursor: true), $"firstPageOne=[{firstOne}]"),
                () => Assert.True(firstFifty.IsPage(50, cursor: true), $"firstPageFifty=[{firstFifty}]"),
                () => Assert.True(firstZero.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.RequestInvalid), $"firstPageZero=[{firstZero}]"),
                () => Assert.True(firstFiftyOne.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.RequestInvalid), $"firstPageFiftyOne=[{firstFiftyOne}]"),
                () => Assert.True(firstLeadingZero.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.RequestInvalid), $"firstPageLeadingZero=[{firstLeadingZero}]"),
                () => Assert.True(firstMalformed.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.RequestInvalid), $"firstPageMalformed=[{firstMalformed}]"),
                () => Assert.True(firstDuplicate.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.RequestInvalid), $"firstPageDuplicate=[{firstDuplicate}]"),
                () => Assert.True(continuationOmitted.IsPage(2), $"continuationOmitted=[{continuationOmitted}]"),
                () => Assert.True(continuationTwo.IsPage(2), $"continuationSupplied=[{continuationTwo}]"),
                () => Assert.True(continuationDuplicate.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"continuationDuplicate=[{continuationDuplicate}]"),
                () => Assert.True(continuationMismatch.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"continuationMismatch=[{continuationMismatch}]"),
                () => Assert.True(continuationZero.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"continuationZero=[{continuationZero}]"),
                () => Assert.True(continuationFiftyOne.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"continuationFiftyOne=[{continuationFiftyOne}]"),
                () => Assert.True(continuationLeadingZero.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"continuationLeadingZero=[{continuationLeadingZero}]"),
                () => Assert.True(continuationMalformed.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"continuationMalformed=[{continuationMalformed}]")
            );
        }
        finally { await CleanupC4PackagesAsync(); }
    }

    [Fact]
    public async Task C422_sibling_principals_share_client_ownership_but_remain_distinct_cursor_actors()
    {
        await SeedC4PackagesAsync(60);
        try
        {
            await using var appA1 = await StartC4HostAsync(Actor(C4ClientA, C4Principal1));
            await using var appA2 = await StartC4HostAsync(Actor(C4ClientA, C4Principal2));
            await using var appB1 = await StartC4HostAsync(Actor(C4ClientB, C4Principal3));
            await using var appB2 = await StartC4HostAsync(Actor(C4ClientB, C4Principal1));
            using var clientA1 = appA1.GetTestClient();
            using var clientA2 = appA2.GetTestClient();
            using var clientB1 = appB1.GetTestClient();
            using var clientB2 = appB2.GetTestClient();
            var a1First = await ObserveAsync(clientA1, "/api/ekyc/raw-export/package-references?pageSize=2");
            var a2First = await ObserveAsync(clientA2, "/api/ekyc/raw-export/package-references?pageSize=2");
            var cursorA1 = a1First.NextCursor ?? throw new InvalidOperationException("C422 A1 did not mint a cursor.");
            var expectedPackage = a1First.PackageIds.FirstOrDefault();
            var encoded = Uri.EscapeDataString(cursorA1);
            var a2Replay = await ObserveAsync(clientA2, $"/api/ekyc/raw-export/package-references?cursor={encoded}");
            var b2Replay = await ObserveAsync(clientB2, $"/api/ekyc/raw-export/package-references?cursor={encoded}");
            var b1Replay = await ObserveAsync(clientB1, $"/api/ekyc/raw-export/package-references?cursor={encoded}");

            Assert.Multiple(
                () => Assert.True(a1First.IsPage(2, cursor: true) && expectedPackage != Guid.Empty, $"A1First=[{a1First}]"),
                () => Assert.True(a2First.IsPage(2, cursor: true) && a2First.PackageIds.Contains(expectedPackage), $"A2OwnFirst=[{a2First}],expected={expectedPackage}"),
                () => Assert.True(a2Replay.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"A2ReplayA1Cursor=[{a2Replay}]"),
                () => Assert.True(b2Replay.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"B2ReplayA1Cursor=[{b2Replay}]"),
                () => Assert.True(b1Replay.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"B1ReplayA1Cursor=[{b1Replay}]")
            );
        }
        finally { await CleanupC4PackagesAsync(); }
    }

    [Fact]
    public async Task C406_cursor_binds_actor_profile_time_and_key_rotation_without_unsigned_fallback()
    {
        await SeedC4PackagesAsync(60);
        try
        {
            var actor = Actor(C4ClientA, C4Principal1);
            var clock = new C4MutableTimeProvider(DateTimeOffset.Parse("2026-08-21T00:00:00Z"));
            await using var app = await StartC4HostAsync(actor, clock);
            using var client = app.GetTestClient();
            var first = await ObserveAsync(client, "/api/ekyc/raw-export/package-references?pageSize=2");
            var token = first.NextCursor ?? throw new InvalidOperationException("C406 did not mint a cursor.");
            var parsed = RecipientPackageReferenceCursorCodec.ParseStructure(token);
            var cursor = parsed.Cursor ?? throw new InvalidOperationException("C406 cursor did not parse.");
            RecipientPackageReferenceCursorCodec.Release(parsed);
            var key = new byte[32];
            var wrongKey = Enumerable.Repeat((byte)1, 32).ToArray();
            var schema = MutateCursorToken(token, payload => payload[4] = 2, key);
            var category = MutateCursorToken(token, payload => payload[6 + payload[5] + 4] = 1, key);
            var profile = MutateCursorToken(token, payload => payload[6 + payload[5] + 5] = 2, key);
            var ttl = MutateCursorToken(token, payload =>
            {
                var issued = BinaryPrimitives.ReadInt64BigEndian(payload.AsSpan(payload.Length - 16, 8));
                BinaryPrimitives.WriteInt64BigEndian(payload.AsSpan(payload.Length - 8, 8), issued + 901);
            }, key);
            var trailing = MutateCursorToken(token, payload => [.. payload, 0x00], key);
            var future = RecipientPackageReferenceCursorCodec.Encode(cursor with
            {
                IssuedAtUnixSeconds = clock.GetUtcNow().ToUnixTimeSeconds() + 1,
                ExpiresAtUnixSeconds = clock.GetUtcNow().ToUnixTimeSeconds() + 901,
            }, key);
            var expired = RecipientPackageReferenceCursorCodec.Encode(cursor with
            {
                IssuedAtUnixSeconds = clock.GetUtcNow().ToUnixTimeSeconds() - 900,
                ExpiresAtUnixSeconds = clock.GetUtcNow().ToUnixTimeSeconds(),
            }, key);
            var unaccepted = RecipientPackageReferenceCursorCodec.Encode(cursor with
            { KeyId = "never-accepted", KeyVersion = 1 }, key);
            var oldToken = RecipientPackageReferenceCursorCodec.Encode(cursor with
            { KeyId = "old-key", KeyVersion = 2 }, key);

            var schemaResult = await ObserveAsync(client, CursorRequest(schema));
            var categoryResult = await ObserveAsync(client, CursorRequest(category));
            var profileResult = await ObserveAsync(client, CursorRequest(profile));
            var ttlResult = await ObserveAsync(client, CursorRequest(ttl));
            var futureResult = await ObserveAsync(client, CursorRequest(future));
            var expiredResult = await ObserveAsync(client, CursorRequest(expired));
            var paddedResult = await ObserveAsync(client, CursorRequest(token + "="));
            var trailingResult = await ObserveAsync(client, CursorRequest(trailing));
            var unacceptedResult = await ObserveAsync(client, CursorRequest(unaccepted));

            var keyText = Convert.ToBase64String(key).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            await using var rotationApp = await StartC4HostAsync(actor, clock, keyText);
            using var rotationClient = rotationApp.GetTestClient();
            var rotationResult = await ObserveAsync(rotationClient, CursorRequest(oldToken));
            await using var unavailableApp = await StartC4HostAsync(actor, clock, "YQ");
            using var unavailableClient = unavailableApp.GetTestClient();
            var unavailableResult = await ObserveAsync(unavailableClient, CursorRequest(oldToken));

            var parsedForMac = RecipientPackageReferenceCursorCodec.ParseStructure(token);
            bool wrongMac;
            try { wrongMac = RecipientPackageReferenceCursorCodec.Verify(parsedForMac, wrongKey); }
            finally { RecipientPackageReferenceCursorCodec.Release(parsedForMac); }

            Assert.Multiple(
                () => Assert.True(first.IsPage(2, cursor: true), $"canonical=[{first}]"),
                () => Assert.True(schemaResult.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"schema=[{schemaResult}]"),
                () => Assert.True(categoryResult.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"category=[{categoryResult}]"),
                () => Assert.True(profileResult.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"queryProfile=[{profileResult}]"),
                () => Assert.True(ttlResult.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"ttl=[{ttlResult}]"),
                () => Assert.True(futureResult.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"issuedFuture=[{futureResult}]"),
                () => Assert.True(expiredResult.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"expired=[{expiredResult}]"),
                () => Assert.True(paddedResult.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"nonCanonicalBase64=[{paddedResult}]"),
                () => Assert.True(trailingResult.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"trailingPayload=[{trailingResult}]"),
                () => Assert.True(unacceptedResult.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid), $"unacceptedKey=[{unacceptedResult}]"),
                () => Assert.True(rotationResult.Status == HttpStatusCode.OK, $"acceptedOldKey=[{rotationResult}]"),
                () => Assert.True(unavailableResult.IsError(HttpStatusCode.ServiceUnavailable, RecipientPackageReferenceErrorCodes.Unavailable), $"acceptedKeyUnavailable=[{unavailableResult}]"),
                () => Assert.False(wrongMac, "wrongMac=[verified=True]")
            );
        }
        finally { await CleanupC4PackagesAsync(); }
    }

    [Fact]
    public void C407_sql_has_owner_leading_finalized_non_null_and_non_empty_predicates()
    {
        var migration = Text("src/TagEkyc.Infrastructure/Persistence/Migrations/20260819180000_Tip88C1C4PackageReferenceDistribution.cs");
        AssertOrdered(migration,
            "p.\"RecipientClientApplicationId\"=p_recipient_client_application_id",
            "p.\"State\"='Finalized'", "p.\"FinalizedAtUtc\" IS NOT NULL",
            "p.\"PackageId\"<>'00000000-0000-0000-0000-000000000000'::uuid");
        Assert.DoesNotContain("PrincipalId", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void C408_descending_total_order_boundary_and_page_plus_one_are_exact()
    {
        var migration = Text("src/TagEkyc.Infrastructure/Persistence/Migrations/20260819180000_Tip88C1C4PackageReferenceDistribution.cs");
        Assert.Contains("p.\"FinalizedAtUtc\"<p_boundary_finalized_at_utc", migration, StringComparison.Ordinal);
        Assert.Contains("p.\"PackageId\"<p_boundary_package_id", migration, StringComparison.Ordinal);
        Assert.Contains("ORDER BY p.\"FinalizedAtUtc\" DESC,p.\"PackageId\" DESC", migration, StringComparison.Ordinal);
        Assert.Contains("LIMIT p_page_size+1", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task C409_foreign_rows_are_excluded_in_sql_and_never_filtered_in_application_memory()
    {
        var migration = Text("src/TagEkyc.Infrastructure/Persistence/Migrations/20260819180000_Tip88C1C4PackageReferenceDistribution.cs");
        var repository = Text("src/TagEkyc.Infrastructure/RawExport/RecipientPackageReferenceRepository.cs");
        await SeedC4PackagesAsync(60, includeForeignPartition: true);
        try
        {
            var clock = new C4MutableTimeProvider(DateTimeOffset.Parse("2026-08-21T00:00:00Z"));
            await using var app = await StartC4HostAsync(Actor(C4ClientA, C4Principal1), clock);
            using var client = app.GetTestClient();
            var foreignBefore = await CountEligibleC4PackagesForClientAsync(C4ClientB);
            var globalOwnerOrder = await ReadC4GlobalOwnerOrderAsync(4);
            var foreignPresent = await ObserveAsync(client, "/api/ekyc/raw-export/package-references?pageSize=2");
            await DeleteC4PackagesForClientAsync(C4ClientB);
            var foreignAfter = await CountEligibleC4PackagesForClientAsync(C4ClientB);
            var foreignAbsent = await ObserveAsync(client, "/api/ekyc/raw-export/package-references?pageSize=2");

            Assert.Multiple(
                () => Assert.Contains("WHERE p.\"RecipientClientApplicationId\"=p_recipient_client_application_id", migration, StringComparison.Ordinal),
                () => Assert.DoesNotContain("RecipientClientApplicationId", repository, StringComparison.Ordinal),
                () => Assert.DoesNotContain("Where(", repository, StringComparison.Ordinal),
                () => Assert.True(foreignBefore > 0, $"foreignBeforeCount={foreignBefore}"),
                () => Assert.Equal(0, foreignAfter),
                () => Assert.True(foreignPresent.ItemCount > 0 && foreignPresent.NextCursor is not null,
                    $"ownPageWhileForeignPresent={foreignPresent}"),
                () => Assert.True(foreignAbsent.ItemCount > 0 && foreignAbsent.NextCursor is not null,
                    $"ownPageWhileForeignAbsent={foreignAbsent}"),
                () => Assert.True(globalOwnerOrder.SequenceEqual(new[] { C4ClientB, C4ClientA, C4ClientB, C4ClientA }),
                    $"globalOwnerOrder={string.Join(',', globalOwnerOrder)}")
            );

            Assert.True(foreignPresent.Status == foreignAbsent.Status
                && foreignPresent.ItemCount == foreignAbsent.ItemCount
                && foreignPresent.PackageIds.SequenceEqual(foreignAbsent.PackageIds)
                && foreignPresent.NextCursor == foreignAbsent.NextCursor
                && foreignPresent.ErrorCode == foreignAbsent.ErrorCode,
                $"foreignPartition=[present:{foreignPresent}][absent:{foreignAbsent}]");
        }
        finally { await CleanupC4PackagesAsync(); }
    }

    [Fact]
    public void C410_reference_visibility_has_no_live_recipient_key_join()
    {
        var migration = Text("src/TagEkyc.Infrastructure/Persistence/Migrations/20260819180000_Tip88C1C4PackageReferenceDistribution.cs");
        var function = Between(migration, "CREATE FUNCTION tagekyc.raw_export_list_recipient_package_references", "ALTER FUNCTION tagekyc.raw_export_list_recipient_package_references");
        Assert.DoesNotContain("recipient_key_registrations", function, StringComparison.Ordinal);
        Assert.DoesNotContain("RecipientKey", function, StringComparison.Ordinal);
    }

    [Fact]
    public Task C411_real_c1_c2_package_flows_through_c4_http_into_real_c3_without_test_substitute() =>
        ExecuteC411RealChainAsync(recipientId =>
            InsertActiveKeyAsync(recipientId, $"c411-{Guid.NewGuid():N}"));

    internal async Task ExecuteC411RealChainAsync(
        Func<Guid, Task> provisionRecipientKeyAsync,
        Func<Guid, Guid, Task>? beforeReferenceListAsync = null,
        bool includeAuthenticatedDelivery = true)
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var providerConfiguration = minio.RecipientPackageConfiguration();
        var c2Options = new RecipientPackageOptions(RecipientPackageTopology.S3CompatibleDurable, providerConfiguration, true);
        S3CompatibleRecipientPackageProvider? store = null;
        try
        {
            var execution = await new Tip88C1C1ResolverAssemblyTests(postgres).ExecuteWithRealC2ProviderAsync(
                async (_, recipientId) =>
                {
                    await provisionRecipientKeyAsync(recipientId);
                    var factory = new RecipientPackageObjectClientFactory(c2Options);
                    store = new S3CompatibleRecipientPackageProvider(c2Options, factory);
                    return new RecipientPackagePreparationProvider(c2Options,
                        new RecipientPackageRepository(new C2RoleConnectionFactory(postgres.ConnectionString)),
                        new RawExportAssemblyRepository(new AssemblyRoleConnectionFactory(postgres.ConnectionString)),
                        new RecipientPackageCryptoService(), store, store, store);
                });
            Assert.Equal(RawExportAssemblyExecutionOutcome.Sealed, execution.Result.Outcome);
            Assert.NotNull(execution.Result.C2PreparationId);
            await using var db = postgres.CreateDbContext();
            var package = await db.RawExportRecipientPackagePreparations.AsNoTracking()
                .SingleAsync(row => row.C2PreparationId == execution.Result.C2PreparationId);
            Assert.Equal("Finalized", package.State);
            if (beforeReferenceListAsync is not null)
                await beforeReferenceListAsync(package.PackageId, package.RecipientClientApplicationId);
            var assembly = await db.RawExportAssemblyIdentities.AsNoTracking()
                .SingleOrDefaultAsync(row => row.JobId == package.JobId);
            var objectProvenance = await ReadC411ObjectProvenanceAsync(store!, package);
            byte[]? expectedEquality = null;
            try
            {
                if (assembly is not null)
                    expectedEquality = RecipientPackageCodec.PackageEqualityFingerprint(
                        assembly.C2PreparationId, assembly.AssemblyId, assembly.AssemblyFingerprint,
                        assembly.ManifestDigest, assembly.AssemblyDigest, assembly.AssemblyAuthenticationValue,
                        assembly.RecipientClientApplicationId, package.RecipientKeyId,
                        package.RecipientKeyVersion, package.RecipientKeyFingerprint,
                        assembly.CompleteAssemblyLength);
                Assert.Multiple(
                    () => Assert.True(objectProvenance.Exists,
                        $"C411-PROVENANCE-OBJECT-EXISTS=[{objectProvenance.Error ?? "true"}]"),
                    () => Assert.True(objectProvenance.LengthMatches,
                        $"C411-PROVENANCE-OBJECT-LENGTH=[expected:{package.EncryptedPackageLength},actual:{objectProvenance.Length}]"),
                    () => Assert.True(objectProvenance.CiphertextDigestMatches,
                        "C411-PROVENANCE-CIPHERTEXT-DIGEST=false"),
                    () => Assert.True(objectProvenance.EnvelopeDigestMatches,
                        "C411-PROVENANCE-ENVELOPE-DIGEST=false"),
                    () => Assert.True(assembly is not null && expectedEquality is not null
                        && CryptographicOperations.FixedTimeEquals(expectedEquality, package.PackageEqualityFingerprint),
                        $"C411-PROVENANCE-C1-EQUALITY=[assembly:{assembly is not null},match:{expectedEquality is not null && CryptographicOperations.FixedTimeEquals(expectedEquality, package.PackageEqualityFingerprint)}]")
                );
            }
            finally
            {
                if (expectedEquality is not null) CryptographicOperations.ZeroMemory(expectedEquality);
            }

            var cursorKey = Convert.ToBase64String(new byte[32]).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var configuration = new Dictionary<string, string?>
            {
                [$"{RecipientPackageReferenceOptions.SectionPath}:Topology"] = "PostgresDurable",
                [$"{RecipientPackageReferenceOptions.SectionPath}:DatabaseConnectionString"] = postgres.ConnectionString,
                [$"{RecipientPackageReferenceOptions.SectionPath}:ActiveCursorKeyId"] = "c4-cursor-01",
                [$"{RecipientPackageReferenceOptions.SectionPath}:ActiveCursorKeyVersion"] = "1",
                [$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyId"] = "c4-cursor-01",
                [$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyVersion"] = "1",
                [$"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:c4-cursor-01:1"] = cursorKey,
            };
            var actor = new AuthenticatedClientContext(Guid.NewGuid(), execution.RecipientClientApplicationId, "c411",
                AuthenticatedCallerCategory.BusinessConsumer,
                new HashSet<string> { RecipientPackageReferenceApplicationService.RequiredScope }, PrincipalId: Guid.NewGuid());
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer(); builder.Configuration.AddInMemoryCollection(configuration);
            builder.Services.AddSingleton<IApiKeyAuthenticator>(new C4Authenticator(actor));
            builder.Services.AddTagEkycRecipientPackageReference(builder.Configuration);
            await using var app = builder.Build(); app.MapRecipientPackageReferenceEndpoints(); await app.StartAsync();
            using var client = app.GetTestClient();
            var response = await client.GetAsync("/api/ekyc/raw-export/package-references?pageSize=25");
            response.EnsureSuccessStatusCode();
            var page = await response.Content.ReadFromJsonAsync<RecipientPackageReferencePageDto>();
            Assert.Contains(page!.Items, item => item.PackageId == package.PackageId);
            if (!includeAuthenticatedDelivery) return;

            var deliveryOptions = new RecipientPackageDeliveryOptions(RecipientPackageDeliveryTopology.S3CompatibleDurable,
                providerConfiguration, minio.RecipientPackageDeliveryReaderCredential(), postgres.ConnectionString, true);
            using var deliveryFactory = new RecipientPackageDeliveryObjectClientFactory(deliveryOptions);
            using var deliveryReader = new S3CompatibleRecipientPackageDeliveryReader(deliveryOptions, deliveryFactory);
            using var pool = new RecipientPackageDeliverySpoolPool();
            var coordinator = new RecipientPackageDeliveryCoordinator(deliveryOptions,
                new RecipientPackageDeliveryRepository(new DeliveryRoleConnectionFactory(postgres.ConnectionString)), deliveryReader, pool);
            var deliveryApplication = new RecipientPackageDeliveryApplicationService(coordinator);
            var deliveryActor = actor with { Scopes = new HashSet<string> { RecipientPackageDeliveryApplicationService.RequiredScope } };
            var before = await CountC3DeliveriesForPackageAsync(package.PackageId);
            var created = await deliveryApplication.CreateAsync(
                deliveryActor, package.PackageId, $"c411-authorized-{Guid.NewGuid():N}", "c411-authorized", default);
            var afterAuthorized = await CountC3DeliveriesForPackageAsync(package.PackageId);
            var unauthorizedActor = deliveryActor with { Scopes = new HashSet<string>() };
            var denied = await deliveryApplication.CreateAsync(
                unauthorizedActor, package.PackageId, $"c411-unauthorized-{Guid.NewGuid():N}", "c411-unauthorized", default);
            var afterDenied = await CountC3DeliveriesForPackageAsync(package.PackageId);
            Assert.Multiple(
                () => Assert.True(created.IsSuccess, $"C411-C3-AUTHORIZED=[{created.Error?.Code ?? "success"}]"),
                () => Assert.Equal(package.PackageId, created.Value?.Delivery.PackageId),
                () => Assert.Equal(before + 1, afterAuthorized),
                () => Assert.False(denied.IsSuccess, "C411-C3-UNAUTHORIZED unexpectedly created a delivery."),
                () => Assert.Equal(RecipientPackageDeliveryErrorCodes.Forbidden, denied.Error?.Code),
                () => Assert.Equal(afterAuthorized, afterDenied));
        }
        finally { store?.Dispose(); }
    }

    [Fact]
    public async Task C412_reference_listing_has_no_provider_dependency_or_persistence_mutation_surface()
    {
        var unresolvedTokens = new List<string>();
        var dependencies = C4RuntimeDependencies(unresolvedTokens).ToArray();
        Assert.True(unresolvedTokens.Count == 0,
            $"C412-UNRESOLVED-IL-TOKENS=[{string.Join(" | ", unresolvedTokens)}]");
        var forbiddenDependencies = dependencies.Where(IsForbiddenC4ProviderDependency)
            .Select(value => value.FullName ?? value.Name).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        Assert.True(forbiddenDependencies.Length == 0,
            $"C412-PROVIDER-DEPENDENCY=[{string.Join(",", forbiddenDependencies)}]");

        await SeedC4PackagesAsync(60);
        try
        {
            var before = await C4StablePersistenceFingerprintAsync();
            await using var app = await StartC4HostAsync(Actor(C4ClientA, C4Principal1));
            using var client = app.GetTestClient();
            var result = await ObserveAsync(client, "/api/ekyc/raw-export/package-references?pageSize=25");
            var after = await C4StablePersistenceFingerprintAsync();
            Assert.True(result.IsPage(25, cursor: true), $"C412-LIST=[{result}]");
            Assert.True(string.Equals(before, after, StringComparison.Ordinal),
                $"C412-PERSISTENCE-FINGERPRINT before={before} after={after}");
        }
        finally { await CleanupC4PackagesAsync(); }
    }

    [Fact]
    public async Task C413_exact_single_sql_signature_projection_guard_and_overload_surface()
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT count(*)=1 AND bool_and(
              pg_get_function_identity_arguments(p.oid)='p_recipient_client_application_id uuid, p_boundary_finalized_at_utc timestamp with time zone, p_boundary_package_id uuid, p_page_size integer'
              AND pg_get_function_result(p.oid)='TABLE("PackageId" uuid, "FinalizedAtUtc" timestamp with time zone)')
            FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname IN ('raw_export_list_recipient_package_references')
            """, connection);
        var catalogExact = (bool)(await command.ExecuteScalarAsync())!;
        Assert.True(catalogExact, $"C413-CATALOG-SIGNATURE-PROJECTION-SURFACE observed={catalogExact}");
        await using var invalid = new NpgsqlCommand(
            "SELECT * FROM tagekyc.raw_export_list_recipient_package_references('00000000-0000-0000-0000-000000000000',NULL,NULL,25)", connection);
        Exception? argumentFailure = null;
        try { await invalid.ExecuteNonQueryAsync(); }
        catch (Exception caught) { argumentFailure = caught; }
        Assert.True(argumentFailure?.GetType() == typeof(PostgresException),
            $"C413-ARGUMENT-GUARD-EXCEPTION expected={typeof(PostgresException).FullName} actual={argumentFailure?.GetType().FullName ?? "<none>"}");
        var error = (PostgresException)argumentFailure!;
        Assert.True(string.Equals("P0001", error.SqlState, StringComparison.Ordinal),
            $"C413-ARGUMENT-GUARD-SQLSTATE expected=P0001 actual={error.SqlState}");
        Assert.True(string.Equals("RAW_EXPORT_PACKAGE_REFERENCE_ARGUMENT_INVALID", error.MessageText, StringComparison.Ordinal),
            $"C413-ARGUMENT-GUARD-MESSAGE expected=RAW_EXPORT_PACKAGE_REFERENCE_ARGUMENT_INVALID actual={error.MessageText}");
    }

    [Fact]
    public async Task C414_exact_role_login_membership_function_acl_and_zero_table_privilege()
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT
              (SELECT count(*)=2 AND bool_and(r.rolinherit AND NOT r.rolsuper AND NOT r.rolcreatedb AND NOT r.rolcreaterole
                AND NOT r.rolreplication AND NOT r.rolbypassrls AND r.rolcanlogin=(r.rolname='tagekyc_raw_export_package_reference_login'))
               FROM pg_roles r WHERE r.rolname IN ('tagekyc_raw_export_package_reference','tagekyc_raw_export_package_reference_login'))
              AND (SELECT count(*)=1 AND bool_and(
                    a.rolname='tagekyc_raw_export_package_reference_login'
                    AND b.rolname='tagekyc_raw_export_package_reference'
                    AND NOT m.admin_option AND m.inherit_option AND NOT m.set_option)
               FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.member JOIN pg_roles b ON b.oid=m.roleid
               WHERE a.rolname IN ('tagekyc_raw_export_package_reference','tagekyc_raw_export_package_reference_login')
                  OR b.rolname IN ('tagekyc_raw_export_package_reference','tagekyc_raw_export_package_reference_login'))
              AND (SELECT count(*)=1 AND bool_and(
                    pg_get_userbyid(a.grantee)='tagekyc_raw_export_package_reference'
                    AND a.privilege_type='EXECUTE' AND NOT a.is_grantable AND a.grantor=p.proowner)
               FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
               CROSS JOIN LATERAL aclexplode(COALESCE(p.proacl,acldefault('f',p.proowner))) a
               WHERE n.nspname='tagekyc' AND p.proname='raw_export_list_recipient_package_references'
                 AND oidvectortypes(p.proargtypes)='uuid, timestamp with time zone, uuid, integer'
                 AND a.grantee<>p.proowner)
              AND has_schema_privilege('tagekyc_raw_export_package_reference','tagekyc','USAGE')
              AND NOT has_table_privilege('tagekyc_raw_export_package_reference','tagekyc.raw_export_recipient_package_preparations','SELECT,INSERT,UPDATE,DELETE')
            """, connection);
        var exactPosture = (bool)(await command.ExecuteScalarAsync())!;
        Assert.True(exactPosture, $"C414-ROLE-LOGIN-MEMBERSHIP-ACL-POSTURE observed={exactPosture}");
        await AssertC4OutboundMembershipRejectedAsync();
    }

    [Fact]
    public async Task C415_indexed_configuration_topology_and_typed_readiness_precedence_are_closed()
    {
        var disabled = RecipientPackageReferenceOptions.Resolve(new ConfigurationBuilder().Build());
        Assert.True(disabled.Topology == RecipientPackageReferenceTopology.Disabled,
            $"C415-DISABLED-TOPOLOGY expected={RecipientPackageReferenceTopology.Disabled} actual={disabled.Topology}");
        await new RecipientPackageReferenceReadinessValidator(disabled, new ServiceCollection().BuildServiceProvider()).ValidateAsync(default);
        var invalidConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { [$"{RecipientPackageReferenceOptions.SectionPath}:Topology"] = "PostgresDurable" }).Build();
        var invalid = RecipientPackageReferenceOptions.Resolve(invalidConfig);
        Assert.True(invalid.Topology == RecipientPackageReferenceTopology.Invalid,
            $"C415-INVALID-TOPOLOGY expected={RecipientPackageReferenceTopology.Invalid} actual={invalid.Topology}");
        Exception? invalidFailure = null;
        try
        {
            await new RecipientPackageReferenceReadinessValidator(invalid, new ServiceCollection().BuildServiceProvider()).ValidateAsync(default);
        }
        catch (Exception caught) { invalidFailure = caught; }
        Assert.True(invalidFailure?.GetType() == typeof(RecipientPackageReferenceReadinessException),
            $"C415-INVALID-READINESS-REJECTION expected={typeof(RecipientPackageReferenceReadinessException).FullName} actual={invalidFailure?.GetType().FullName ?? "<none>"}");
        var error = (RecipientPackageReferenceReadinessException)invalidFailure!;
        Assert.True(string.Equals(RecipientPackageReferenceReadinessValidator.Codes[0], error.Code, StringComparison.Ordinal),
            $"C415-INVALID-READINESS-CODE expected={RecipientPackageReferenceReadinessValidator.Codes[0]} actual={error.Code}");

        var key = Convert.ToBase64String(new byte[32]).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var durableConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{RecipientPackageReferenceOptions.SectionPath}:Topology"] = "PostgresDurable",
            [$"{RecipientPackageReferenceOptions.SectionPath}:DatabaseConnectionString"] = postgres.ConnectionString,
            [$"{RecipientPackageReferenceOptions.SectionPath}:ActiveCursorKeyId"] = "c4-cursor-01",
            [$"{RecipientPackageReferenceOptions.SectionPath}:ActiveCursorKeyVersion"] = "1",
            [$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyId"] = "c4-cursor-01",
            [$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyVersion"] = "1",
            [$"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:c4-cursor-01:1"] = key,
        }).Build();
        using var durableServices = new ServiceCollection().AddTagEkycRecipientPackageReference(durableConfig).BuildServiceProvider();
        var durable = durableServices.GetRequiredService<RecipientPackageReferenceOptions>();
        Assert.True(durable.Topology == RecipientPackageReferenceTopology.PostgresDurable,
            $"C415-DURABLE-TOPOLOGY expected={RecipientPackageReferenceTopology.PostgresDurable} actual={durable.Topology}");
        await using (var diagnosticConnection = await OpenAsync())
        await using (var diagnostic = new NpgsqlCommand("""
            SELECT
              (SELECT count(*)=2 FROM pg_roles WHERE rolname IN ('tagekyc_raw_export_package_reference','tagekyc_raw_export_package_reference_login')) role_ok,
              (SELECT count(*)=1 FROM pg_auth_members m JOIN pg_roles a ON a.oid=m.member JOIN pg_roles b ON b.oid=m.roleid
                WHERE a.rolname IN ('tagekyc_raw_export_package_reference','tagekyc_raw_export_package_reference_login')
                   OR b.rolname IN ('tagekyc_raw_export_package_reference','tagekyc_raw_export_package_reference_login')) member_ok,
              (SELECT count(*)=1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                WHERE n.nspname='tagekyc' AND p.proname IN ('raw_export_list_recipient_package_references')
                  AND oidvectortypes(p.proargtypes)='uuid, timestamp with time zone, uuid, integer'
                  AND pg_get_userbyid(p.proowner)='tagekyc_raw_export_deployer' AND p.prosecdef
                  AND p.proconfig=ARRAY['search_path=pg_catalog']::text[]
                  AND (SELECT count(*)=1 AND bool_and(
                        pg_get_userbyid(a.grantee)='tagekyc_raw_export_package_reference'
                        AND a.privilege_type='EXECUTE' AND NOT a.is_grantable AND a.grantor=p.proowner)
                       FROM aclexplode(COALESCE(p.proacl,acldefault('f',p.proowner))) a
                       WHERE a.grantee<>p.proowner)) function_ok,
              (SELECT count(*)=1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='tagekyc' AND c.relname IN ('ix_raw_export_recipient_package_reference_list')) index_ok,
              has_schema_privilege('tagekyc_raw_export_package_reference','tagekyc','USAGE') schema_ok,
              NOT has_table_privilege('tagekyc_raw_export_package_reference','tagekyc.raw_export_recipient_package_preparations','SELECT,INSERT,UPDATE,DELETE') table_ok,
              (SELECT pg_get_indexdef(c.oid) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='tagekyc' AND c.relname='ix_raw_export_recipient_package_reference_list') index_def
            """, diagnosticConnection))
        await using (var reader = await diagnostic.ExecuteReaderAsync())
        {
            var hasCatalogRow = await reader.ReadAsync();
            Assert.True(hasCatalogRow, $"C415-CATALOG-ROW observed={hasCatalogRow}");
            for (var column = 0; column < 6; column++)
                Assert.True(reader.GetBoolean(column), $"{reader.GetName(column)} failed; index={reader.GetString(6)}");
            var expectedIndex = "CREATE INDEX ix_raw_export_recipient_package_reference_list ON tagekyc.raw_export_recipient_package_preparations USING btree (\"RecipientClientApplicationId\", \"FinalizedAtUtc\" DESC, \"PackageId\" DESC) WHERE (((\"State\")::text = 'Finalized'::text) AND (\"FinalizedAtUtc\" IS NOT NULL))";
            var actualIndex = reader.GetString(6);
            Assert.True(string.Equals(expectedIndex, actualIndex, StringComparison.Ordinal),
                $"C415-INDEX-DEFINITION expected={expectedIndex} actual={actualIndex}");
        }
        await durableServices.GetRequiredService<RecipientPackageReferenceReadinessValidator>().ValidateAsync(default);

        await AssertC4CatalogPostureRejectedAsync("c4_function_posture", """
            ALTER FUNCTION tagekyc.raw_export_list_recipient_package_references(uuid,timestamp with time zone,uuid,integer)
              SECURITY INVOKER
            """, RecipientPackageReferenceReadinessValidator.Codes[2]);
        await AssertC4CatalogPostureRejectedAsync("c4_missing_function_execute", """
            REVOKE EXECUTE ON FUNCTION tagekyc.raw_export_list_recipient_package_references(uuid,timestamp with time zone,uuid,integer)
              FROM tagekyc_raw_export_package_reference
            """, RecipientPackageReferenceReadinessValidator.Codes[2]);
        await AssertC4CatalogPostureRejectedAsync("c4_table_privilege", """
            GRANT SELECT ON TABLE tagekyc.raw_export_recipient_package_preparations
              TO tagekyc_raw_export_package_reference
            """, RecipientPackageReferenceReadinessValidator.Codes[2]);
        await AssertC4CatalogPostureRejectedAsync("c4_index_shape", """
            DROP INDEX tagekyc.ix_raw_export_recipient_package_reference_list;
            CREATE INDEX ix_raw_export_recipient_package_reference_list
              ON tagekyc.raw_export_recipient_package_preparations
              ("RecipientClientApplicationId", "PackageId" DESC, "FinalizedAtUtc" DESC)
              WHERE "State" = 'Finalized' AND "FinalizedAtUtc" IS NOT NULL;
            ALTER INDEX tagekyc.ix_raw_export_recipient_package_reference_list
              OWNER TO tagekyc_raw_export_deployer
            """, RecipientPackageReferenceReadinessValidator.Codes[2]);

        var ownedIdentifiers = new[]
        {
            "tagekyc_raw_export_package_reference",
            "tagekyc_raw_export_package_reference_login",
            "raw_export_list_recipient_package_references",
            "ix_raw_export_recipient_package_reference_list",
        };
        Assert.True(C4IdentifiersFitPostgres(ownedIdentifiers), "C415-IDENTIFIER-LENGTH-CANONICAL");
        Assert.False(C4IdentifiersFitPostgres(ownedIdentifiers.Append(new string('x', 64))),
            "C415-IDENTIFIER-LENGTH-64-BYTE-NEGATIVE");
    }

    [Fact]
    public void C416_partial_index_additive_model_and_three_raw_worktree_tripwires_are_exact()
    {
        using var db = postgres.CreateDbContext();
        var entity = db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model
            .FindEntityType("TagEkyc.Infrastructure.Persistence.Entities.RawExportRecipientPackagePreparationRow")!;
        var matchingIndexes = entity.GetIndexes()
            .Where(value => value.GetDatabaseName() == "ix_raw_export_recipient_package_reference_list").ToArray();
        Assert.True(matchingIndexes.Length == 1,
            $"C416-INDEX-COUNT expected=1 actual={matchingIndexes.Length}");
        var index = matchingIndexes[0];
        var actualProperties = index.Properties.Select(value => value.Name).ToArray();
        Assert.True(new[] { "RecipientClientApplicationId", "FinalizedAtUtc", "PackageId" }.SequenceEqual(actualProperties, StringComparer.Ordinal),
            $"C416-INDEX-PROPERTIES actual=[{string.Join(",", actualProperties)}]");
        var actualDirections = index.IsDescending?.ToArray() ?? Array.Empty<bool>();
        Assert.True(new[] { false, true, true }.SequenceEqual(actualDirections),
            $"C416-INDEX-DIRECTIONS actual=[{string.Join(",", actualDirections)}]");
        var actualFilter = index.GetFilter();
        Assert.True(string.Equals("\"State\" = 'Finalized' AND \"FinalizedAtUtc\" IS NOT NULL", actualFilter, StringComparison.Ordinal),
            $"C416-INDEX-FILTER actual={actualFilter ?? "<null>"}");
        var snapshot = PathOf("src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs");
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(snapshot)));
        var tripwires = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["tests/TagEkyc.IntegrationTests/Tip88C1C2RecipientPackageTests.cs"] =
                "Assert\\.Equal\\(\\\"(?<sha>[A-F0-9]{64})\\\", snapshotHash\\)",
            ["tests/TagEkyc.IntegrationTests/Tip88C1B2R3VerifiedCiphertextStagingTests.cs"] =
                "ExpectedSnapshotSha256\\s*=\\s*\\\"(?<sha>[A-F0-9]{64})\\\"",
            ["tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs"] =
                "ExpectedModelSnapshotSha256\\s*=\\s*\\\"(?<sha>[A-F0-9]{64})\\\"",
        };
        Assert.True(tripwires.Count == 3, $"C416-TRIPWIRE-COUNT expected=3 actual={tripwires.Count}");
        foreach (var (path, pattern) in tripwires)
        {
            var match = Regex.Match(Text(path), pattern, RegexOptions.CultureInvariant);
            Assert.True(match.Success, $"C416-TRIPWIRE-MISSING=[{path}]");
            Assert.True(string.Equals(hash, match.Groups["sha"].Value, StringComparison.Ordinal),
                $"C416-TRIPWIRE-SHA=[{path}] expected={hash} actual={match.Groups["sha"].Value}");
        }

        const string lfCatalog = "CREATE FUNCTION exact_name(uuid)\nSECURITY DEFINER\nSET search_path=pg_catalog";
        var crlfCatalog = lfCatalog.ReplaceLineEndings("\r\n");
        Assert.True(C4NormalizedCatalogTextEquals(lfCatalog, crlfCatalog),
            "C416-LINE-ENDINGS-B1-TWO-SIDED-NORMALIZATION");
        Assert.False(C4OneSidedCatalogTextEquals(lfCatalog, crlfCatalog),
            "C416-LINE-ENDINGS-B2-ONE-SIDED-MUST-DIFFER");
        var semanticDifference = crlfCatalog.Replace("SECURITY DEFINER", "SECURITY INVOKER", StringComparison.Ordinal);
        Assert.False(C4NormalizedCatalogTextEquals(lfCatalog, semanticDifference),
            "C416-LINE-ENDINGS-B3-SEMANTIC-DIFFERENCE");
    }

    [Fact]
    public async Task C417_disposable_apply_down_reapply_does_not_mutate_shared_database()
    {
        string[] sharedBefore;
        await using (var shared = postgres.CreateDbContext())
            sharedBefore = (await shared.Database.GetAppliedMigrationsAsync()).ToArray();

        var isolated = await CreateC4DisposableDatabaseAsync("c4down");
        var previous = "20260819120000_Tip88C1C3AuthenticatedPackageDelivery";
        try
        {
            Assert.True(await C4DatabaseExistsAsync(isolated.AdminConnectionString, isolated.DatabaseName),
                $"C417-DATABASE-CREATE=[{isolated.DatabaseName}]");
            await using (var db = new TagEkycDbContext(new DbContextOptionsBuilder<TagEkycDbContext>()
                .UseNpgsql(isolated.ConnectionString).Options))
            {
                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync();
                var afterApply = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
                Assert.True(afterApply.Contains("20260819180000_Tip88C1C4PackageReferenceDistribution", StringComparer.Ordinal),
                    $"C417-APPLY-LATEST migrations=[{string.Join(",", afterApply)}]");
                await migrator.MigrateAsync(previous);
                var afterDown = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
                Assert.True(!afterDown.Contains("20260819180000_Tip88C1C4PackageReferenceDistribution", StringComparer.Ordinal),
                    $"C417-DOWN-TO-C3 migrations=[{string.Join(",", afterDown)}]");
                await migrator.MigrateAsync();
                var afterReapply = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
                Assert.True(afterReapply.Contains("20260819180000_Tip88C1C4PackageReferenceDistribution", StringComparer.Ordinal),
                    $"C417-REAPPLY-LATEST migrations=[{string.Join(",", afterReapply)}]");
            }

            string[] sharedAfter;
            await using (var shared = postgres.CreateDbContext())
                sharedAfter = (await shared.Database.GetAppliedMigrationsAsync()).ToArray();
            Assert.True(sharedBefore.SequenceEqual(sharedAfter, StringComparer.Ordinal),
                $"C417-SHARED-MIGRATION-STATE before=[{string.Join(",", sharedBefore)}] after=[{string.Join(",", sharedAfter)}]");

            await isolated.DisposeAsync();
            Assert.False(await C4DatabaseExistsAsync(isolated.AdminConnectionString, isolated.DatabaseName),
                $"C417-POST-DISPOSE-DATABASE-ABSENCE=[{isolated.DatabaseName}]");
        }
        finally
        {
            await ForceDropC4DatabaseAsync(isolated.AdminConnectionString, isolated.DatabaseName);
        }
    }

    [Fact]
    public async Task C418_four_census_families_are_exact_and_c4_catalog_has_no_wildcard()
    {
        string[] discoveredRoles;
        string[] discoveredLogins;
        await using (var connection = await OpenAsync())
        {
            discoveredRoles = await DiscoverStringsAsync(connection, """
                SELECT rolname FROM pg_roles
                WHERE rolname LIKE 'tagekyc_raw_export_package_%'
                  AND rolname NOT LIKE '%_login'
                ORDER BY rolname
                """);
            discoveredLogins = await DiscoverStringsAsync(connection, """
                SELECT rolname FROM pg_roles
                WHERE rolname LIKE 'tagekyc_raw_export_package_%_login'
                ORDER BY rolname
                """);
        }
        var expectedRoles = new[]
        {
            "tagekyc_raw_export_package_delivery", "tagekyc_raw_export_package_lifecycle",
            "tagekyc_raw_export_package_preparer", "tagekyc_raw_export_package_reconciler",
            "tagekyc_raw_export_package_reference",
        };
        var expectedLogins = expectedRoles.Select(value => $"{value}_login").OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var credentialInitializer = Between(Text("tests/TagEkyc.IntegrationTests/DurableObjectMinioFixture.cs"),
            "recipientPackageCredentials =", ".ToDictionary(name => name");
        var discoveredCredentials = Regex.Matches(credentialInitializer, "\"(?<name>[a-z-]+)\"")
            .Select(match => match.Groups["name"].Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var expectedCredentials = new[] { "delivery-reader", "lifecycle", "posture", "reconciler", "writer" };
        var discoveredDatabaseCapabilities = Enum.GetNames<RecipientPackageDatabaseCapability>()
            .Select(value => $"c2:{value}")
            .Append($"c3:{typeof(IRecipientPackageDeliveryConnectionFactory).Name}")
            .Append($"c4:{typeof(IRecipientPackageReferenceConnectionFactory).Name}")
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var expectedDatabaseCapabilities = new[]
        {
            "c2:Lifecycle", "c2:Preparer", "c2:Reconciler",
            "c3:IRecipientPackageDeliveryConnectionFactory", "c4:IRecipientPackageReferenceConnectionFactory",
        };

        AssertExactOwnedSet("C418-S3-CREDENTIALS", expectedCredentials, discoveredCredentials);
        AssertExactOwnedSet("C418-DB-CAPABILITIES", expectedDatabaseCapabilities, discoveredDatabaseCapabilities);
        AssertExactOwnedSet("C418-SQL-ROLES", expectedRoles, discoveredRoles);
        AssertExactOwnedSet("C418-SQL-LOGINS", expectedLogins, discoveredLogins);
        Assert.False(C4OwnedSetEquals(expectedCredentials, discoveredCredentials.Append("sibling-reader")), "C418-S3-SIBLING-CONTROL");
        Assert.False(C4OwnedSetEquals(expectedDatabaseCapabilities, discoveredDatabaseCapabilities.Append("c5:SiblingFactory")), "C418-DB-SIBLING-CONTROL");
        Assert.False(C4OwnedSetEquals(expectedRoles, discoveredRoles.Append("tagekyc_raw_export_package_sibling")), "C418-ROLE-SIBLING-CONTROL");
        Assert.False(C4OwnedSetEquals(expectedLogins, discoveredLogins.Append("tagekyc_raw_export_package_sibling_login")), "C418-LOGIN-SIBLING-CONTROL");
        Assert.False(C4OwnedSetEquals(expectedRoles,
            expectedRoles.Where(value => value != "tagekyc_raw_export_package_reference")
                .Append("tagekyc_raw_export_package_delivery")), "C418-ROLE-ALIAS-CONTROL");
        Assert.False(C4OwnedSetEquals(expectedLogins,
            expectedLogins.Where(value => value != "tagekyc_raw_export_package_reference_login")
                .Append("tagekyc_raw_export_package_delivery_login")), "C418-LOGIN-ALIAS-CONTROL");
        await AssertC4RealCatalogSiblingOwnershipAsync();
        var readinessSource = Text("src/TagEkyc.Infrastructure/RawExport/RecipientPackageReferenceReadinessValidator.cs");
        var openEndedOwnership = new Regex(
            @"\b(?:LIKE|ILIKE)\b|\bSIMILAR\s+TO\b|!~\*?|(?<![!<>=])~\*?|\b(?:left|right|substring|position|starts_with|strpos)\s*\(",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Match(readinessSource);
        Assert.True(!openEndedOwnership.Success,
            $"C418-OPEN-ENDED-OWNERSHIP-PREDICATE observed={openEndedOwnership.Value}");
    }

    [Fact]
    public async Task C420_disabled_invalid_and_dependency_failure_never_become_empty_success()
    {
        var configuration = new ConfigurationBuilder().Build();
        using var services = new ServiceCollection().AddTagEkycRecipientPackageReference(configuration).BuildServiceProvider();
        var gateway = services.GetRequiredService<IRecipientPackageReferenceGateway>();
        var actor = new AuthenticatedClientContext(Guid.NewGuid(), Guid.NewGuid(), "key", AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { RecipientPackageReferenceApplicationService.RequiredScope }, PrincipalId: Guid.NewGuid());
        var result = await gateway.ListAsync(actor, new([], [], []), default);
        Assert.True(!result.IsSuccess, $"C420-DISABLED-RESULT success={result.IsSuccess}");
        Assert.True(string.Equals(RecipientPackageReferenceErrorCodes.Unavailable, result.Error?.Code, StringComparison.Ordinal),
            $"C420-DISABLED-CODE expected={RecipientPackageReferenceErrorCodes.Unavailable} actual={result.Error?.Code ?? "<null>"}");
        var disabledFactory = services.GetService<IRecipientPackageReferenceConnectionFactory>();
        Assert.True(disabledFactory is null,
            $"C420-DISABLED-CONNECTION-FACTORY actual={disabledFactory?.GetType().FullName ?? "<null>"}");

        var valid = C4ValidConfiguration(postgres.ConnectionString);
        await AssertInvalidC4ConfigurationShortCircuitsAsync(C4With(valid,
            ($"{RecipientPackageReferenceOptions.SectionPath}:ActiveCursorKeyId", "C4-CURSOR-01"),
            ($"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyId", "C4-CURSOR-01"),
            ($"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:C4-CURSOR-01:1", valid[$"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:c4-cursor-01:1"]),
            ($"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:c4-cursor-01:1", null)), "CASE-ALIAS", actor);
        await AssertInvalidC4ConfigurationShortCircuitsAsync(C4With(valid,
            ($"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:1:KeyId", "old-key"),
            ($"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:1:KeyVersion", "2"),
            ($"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:2:KeyId", "old-key"),
            ($"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:2:KeyVersion", "2"),
            ($"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:old-key:2", valid[$"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:c4-cursor-01:1"]),
            ($"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:extra-key:3", valid[$"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:c4-cursor-01:1"])), "DUPLICATE-IDENTITY", actor);
        await AssertInvalidC4ConfigurationShortCircuitsAsync(C4With(valid,
            ($"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyId", null),
            ($"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyVersion", null),
            ($"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:1:KeyId", "c4-cursor-01"),
            ($"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:1:KeyVersion", "1")), "NON-CONTIGUOUS", actor);
        await AssertInvalidC4ConfigurationShortCircuitsAsync(C4With(valid,
            ($"{RecipientPackageReferenceOptions.SectionPath}:Unexpected", "value")), "UNKNOWN-CHILD", actor);
        await AssertInvalidC4ConfigurationShortCircuitsAsync(C4With(valid,
            ($"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:1:KeyId", "old-key"),
            ($"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:1:KeyVersion", "2"),
            ($"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:other-key:3", valid[$"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:c4-cursor-01:1"])), "ACCEPTED-WITHOUT-MATERIAL", actor);
        await AssertInvalidC4ConfigurationShortCircuitsAsync(C4With(valid,
            ($"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:old-key:2", valid[$"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:c4-cursor-01:1"])), "MATERIAL-WITHOUT-ACCEPTED", actor);

        await SeedC4PackagesAsync(60);
        try
        {
            var clock = new C4MutableTimeProvider(DateTimeOffset.Parse("2026-08-21T00:00:00Z"));
            var key = Convert.ToBase64String(new byte[32]).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var durableActor = Actor(C4ClientA, C4Principal1);
            await using var mintApp = await StartC4HostAsync(
                durableActor, clock, oldKeyMaterial: key, activeOldKey: true);
            using var mintClient = mintApp.GetTestClient();
            var minted = await ObserveAsync(mintClient, "/api/ekyc/raw-export/package-references?pageSize=2");
            var oldCursor = minted.NextCursor ?? throw new InvalidOperationException("C420 did not mint an old-key cursor.");
            var parsedOld = RecipientPackageReferenceCursorCodec.ParseStructure(oldCursor);
            var oldValue = parsedOld.Cursor ?? throw new InvalidOperationException("C420 old-key cursor did not parse.");
            var unacceptedCursor = RecipientPackageReferenceCursorCodec.Encode(
                oldValue with { KeyId = "never-accepted", KeyVersion = 1 }, new byte[32]);
            RecipientPackageReferenceCursorCodec.Release(parsedOld);

            await using var keyAApp = await StartC4HostAsync(
                durableActor, clock, oldKeyMaterial: "YQ");
            using var keyAClient = keyAApp.GetTestClient();
            var keyA = await ObserveAsync(keyAClient, CursorRequest(oldCursor));

            await using var keyBApp = await StartC4HostAsync(
                durableActor, clock, oldKeyMaterial: key, activeKeyMaterial: "YQ");
            using var keyBClient = keyBApp.GetTestClient();
            var keyB = await ObserveAsync(keyBClient, CursorRequest(oldCursor));

            var recording = new C4RecordingConfigurationSource();
            await using var arbitraryConfigApp = await StartC4HostAsync(
                durableActor, clock, oldKeyMaterial: key, recordingSource: recording);
            using var arbitraryConfigClient = arbitraryConfigApp.GetTestClient();
            recording.ClearReads();
            using var arbitraryScope = arbitraryConfigApp.Services.CreateScope();
            using var unexpectedLease = await arbitraryScope.ServiceProvider
                .GetRequiredService<RecipientPackageReferenceCursorKeyService>()
                .ResolveAsync(new("never-accepted", 1), default);
            var unaccepted = await ObserveAsync(arbitraryConfigClient, CursorRequest(unacceptedCursor));
            var forbiddenConfigKey = $"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:never-accepted:1";

            Assert.Multiple(
                () => Assert.True(minted.IsPage(2, cursor: true), $"C420-MINT=[{minted}]"),
                () => Assert.True(keyA.IsError(HttpStatusCode.ServiceUnavailable, RecipientPackageReferenceErrorCodes.Unavailable)
                    && keyA.ItemCount == 0 && keyA.NextCursor is null, $"C420-KEYA=[{keyA}]"),
                () => Assert.True(keyB.IsError(HttpStatusCode.ServiceUnavailable, RecipientPackageReferenceErrorCodes.Unavailable)
                    && keyB.ItemCount == 0 && keyB.NextCursor is null, $"C420-KEYB=[{keyB}]"),
                () => Assert.True(unaccepted.IsError(HttpStatusCode.BadRequest, RecipientPackageReferenceErrorCodes.CursorInvalid),
                    $"C420-UNACCEPTED=[{unaccepted}]"),
                () => Assert.True(unexpectedLease is null,
                    $"C420-UNACCEPTED-LEASE actual={unexpectedLease?.GetType().FullName ?? "<null>"}"),
                () => Assert.False(recording.WasRead(forbiddenConfigKey),
                    $"C420-ARBITRARY-CONFIG-LOOKUP=[{forbiddenConfigKey}]")
            );
        }
        finally { await CleanupC4PackagesAsync(); }
    }

    [Fact]
    public void C424_allowlist_proof_census_diff_and_test_resource_lifecycle_closeout_are_machine_checkable()
    {
        var files = new[]
        {
            "tests/TagEkyc.UnitTests/Tip88C1C4RecipientPackageReferenceCodecTests.cs",
            "tests/TagEkyc.UnitTests/Tip88C1C4RecipientPackageReferenceApplicationTests.cs",
            "tests/TagEkyc.ArchTests/Tip88C1C4RecipientPackageReferenceArchTests.cs",
            "tests/TagEkyc.IntegrationTests/Tip88C1C4RecipientPackageReferenceTests.cs",
        };
        var ids = files.SelectMany(file => Regex.Matches(Text(file), @"public\s+(?:async\s+)?(?:Task|void)\s+(C4(?:0[1-9]|1[0-9]|2[0-4]))_")
            .Select(match => match.Groups[1].Value)).ToArray();
        var distinctIds = ids.Distinct(StringComparer.Ordinal).Count();
        Assert.True(distinctIds == 24, $"C424-PROOF-DISTINCT expected=24 actual={distinctIds} ids=[{string.Join(",", ids)}]");
        Assert.True(ids.Length == 24, $"C424-PROOF-DECLARATIONS expected=24 actual={ids.Length} ids=[{string.Join(",", ids)}]");
        var fixtureSource = Text("tests/TagEkyc.IntegrationTests/PostgresPersistenceFixture.cs");
        Assert.True(fixtureSource.Contains("--remove-orphans", StringComparison.Ordinal),
            $"C424-DOCKER-CLEANUP-FLAG observed={fixtureSource.Contains("--remove-orphans", StringComparison.Ordinal)}");
    }

    private async Task<WebApplication> StartC4HostAsync(
        AuthenticatedClientContext actor,
        TimeProvider? timeProvider = null,
        string? oldKeyMaterial = null,
        string? activeKeyMaterial = null,
        bool activeOldKey = false,
        C4RecordingConfigurationSource? recordingSource = null)
    {
        var cursorKey = Convert.ToBase64String(new byte[32]).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        activeKeyMaterial ??= cursorKey;
        var configuration = new Dictionary<string, string?>
        {
            [$"{RecipientPackageReferenceOptions.SectionPath}:Topology"] = "PostgresDurable",
            [$"{RecipientPackageReferenceOptions.SectionPath}:DatabaseConnectionString"] = postgres.ConnectionString,
            [$"{RecipientPackageReferenceOptions.SectionPath}:ActiveCursorKeyId"] = activeOldKey ? "old-key" : "c4-cursor-01",
            [$"{RecipientPackageReferenceOptions.SectionPath}:ActiveCursorKeyVersion"] = activeOldKey ? "2" : "1",
            [$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyId"] = activeOldKey ? "old-key" : "c4-cursor-01",
            [$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyVersion"] = activeOldKey ? "2" : "1",
            [$"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:{(activeOldKey ? "old-key:2" : "c4-cursor-01:1")}"] =
                activeOldKey ? oldKeyMaterial ?? cursorKey : activeKeyMaterial,
        };
        if (!activeOldKey && oldKeyMaterial is not null)
        {
            configuration[$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:1:KeyId"] = "old-key";
            configuration[$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:1:KeyVersion"] = "2";
            configuration[$"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:old-key:2"] = oldKeyMaterial;
        }
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        if (recordingSource is null)
            builder.Configuration.AddInMemoryCollection(configuration);
        else
        {
            recordingSource.Seed(configuration);
            ((IConfigurationBuilder)builder.Configuration).Add(recordingSource);
        }
        builder.Services.AddSingleton<IApiKeyAuthenticator>(new C4Authenticator(actor));
        if (timeProvider is not null) builder.Services.AddSingleton(timeProvider);
        builder.Services.AddTagEkycRecipientPackageReference(builder.Configuration);
        var app = builder.Build();
        app.MapRecipientPackageReferenceEndpoints();
        await app.StartAsync();
        return app;
    }

    private static string CursorRequest(string token) =>
        $"/api/ekyc/raw-export/package-references?cursor={Uri.EscapeDataString(token)}";

    private static string MutateCursorToken(string token, Action<byte[]> mutation, byte[] key) =>
        MutateCursorToken(token, payload => { mutation(payload); return payload; }, key);

    private static string MutateCursorToken(string token, Func<byte[], byte[]> mutation, byte[] key)
    {
        var payload = DecodeBase64Url(token[..token.IndexOf('.')]);
        payload = mutation(payload);
        var label = Encoding.ASCII.GetBytes(RecipientPackageReferenceCursorCodec.MacLabel);
        var preimage = new byte[label.Length + 1 + payload.Length];
        label.CopyTo(preimage, 0);
        payload.CopyTo(preimage, label.Length + 1);
        var mac = HMACSHA256.HashData(key, preimage);
        return Base64Url(payload) + "." + Base64Url(mac);
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 += (base64.Length % 4) switch { 2 => "==", 3 => "=", 0 => "", _ => "!" };
        return Convert.FromBase64String(base64);
    }

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class C4MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private static AuthenticatedClientContext Actor(Guid clientApplicationId, Guid principalId) => new(
        Guid.Parse("43000000-0000-0000-0000-000000000001"), clientApplicationId, "c4-rri04",
        AuthenticatedCallerCategory.BusinessConsumer,
        new HashSet<string> { RecipientPackageReferenceApplicationService.RequiredScope },
        PrincipalId: principalId);

    private static async Task<C4HttpObservation> ObserveAsync(HttpClient client, string requestUri)
    {
        using var response = await client.GetAsync(requestUri);
        var content = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var page = JsonSerializer.Deserialize<RecipientPackageReferencePageDto>(content,
                new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidOperationException("C4 page response was empty.");
            return new(response.StatusCode, page.Items.Count, page.NextCursor,
                page.Items.Select(item => item.PackageId).ToArray(), null);
        }

        using var document = JsonDocument.Parse(content);
        var code = document.RootElement.GetProperty("error").GetProperty("code").GetString();
        return new(response.StatusCode, 0, null, [], code);
    }

    private async Task SeedC4PackagesAsync(int count, bool includeForeignPartition = false)
    {
        await CleanupC4PackagesAsync();
        var lineage = await new Tip88C1C1ResolverAssemblyTests(postgres).CreateC2RecipientPackageLineageAsync();
        using var rsa = RSA.Create(3072);
        var spki = rsa.ExportSubjectPublicKeyInfo();
        var fingerprint = SHA256.HashData(spki);
        var keyId = $"c4-rri04-{Guid.NewGuid():N}";
        var baseTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        try
        {
            await using var connection = await OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            foreach (var clientId in new[] { C4ClientA, C4ClientB })
            {
                await using var key = new NpgsqlCommand("""
                    INSERT INTO tagekyc.raw_export_recipient_key_registrations(
                      "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion","PublicKeyAlgorithm",
                      "PublicKeySpki","PublicKeyFingerprint","ValidFromUtc","ValidUntilUtc","State","Revision","RegisteredAtUtc")
                    VALUES(@client,@key,1,'RSA-OAEP-256',@spki,@fingerprint,@validFrom,@validUntil,'Active',1,@registered)
                    """, connection, transaction);
                key.Parameters.AddWithValue("client", clientId);
                key.Parameters.AddWithValue("key", keyId);
                key.Parameters.AddWithValue("spki", spki);
                key.Parameters.AddWithValue("fingerprint", fingerprint);
                key.Parameters.AddWithValue("validFrom", baseTime.AddHours(-1));
                key.Parameters.AddWithValue("validUntil", baseTime.AddHours(2));
                key.Parameters.AddWithValue("registered", baseTime.AddHours(-1));
                await key.ExecuteNonQueryAsync();
            }

            var packages = Enumerable.Range(0, count)
                .Select(index => (ClientId: C4ClientA, FinalizedAtUtc: baseTime.AddSeconds(index)))
                .ToList();
            if (includeForeignPartition)
            {
                packages.Add((C4ClientB, baseTime.AddSeconds(count)));
                packages.Add((C4ClientB, baseTime.AddSeconds(count - 2).AddMilliseconds(500)));
            }

            foreach (var seed in packages)
            {
                var packageId = Guid.NewGuid();
                await using var package = new NpgsqlCommand("""
                    INSERT INTO tagekyc.raw_export_recipient_package_preparations(
                      "C2PreparationId","PackageId","PackageEqualityFingerprint","AssemblyId","JobId","AttemptId","FencingToken",
                      "AssemblyFingerprint","ManifestDigest","AssemblyDigest","AssemblyAuthenticationValue","CompleteAssemblyLength",
                      "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion","RecipientKeyFingerprint","RecipientPublicKeySpki",
                      "RecipientKeyRevision","RecipientKeyValidFromUtc","RecipientKeyValidUntilUtc","PackageProfile",
                      "ProviderOperationTokenDigest","ProviderKind","ProviderConfigurationId","ProviderEndpointFingerprint","BucketName",
                      "ObjectKey","ObjectBindingDigest","EnvelopeDigest","EncryptedPackageLength","PackageCiphertextDigest",
                      "ConditionalCreateEvidenceDigest","ProviderReceiptDigest","State","Revision","SnapshotFrozenAtUtc",
                      "PutStartedAtUtc","PreparedAtUtc","FinalizedAtUtc")
                    VALUES(
                      @preparation,@package,@equality,@assembly,@job,@attempt,@fence,
                      @assemblyFingerprint,@manifest,@assemblyDigest,@authentication,1,
                      @client,@key,1,@keyFingerprint,@spki,1,@validFrom,@validUntil,'tip-88c1-c2-package-profile-v1',
                      @operation,'s3-compatible-single-part-v1','c4-rri04-provider',@endpoint,'c4-rri04',
                      @objectKey,@binding,@envelope,1,@ciphertext,@createEvidence,@receipt,'Finalized',1,@frozen,@putStarted,@prepared,@finalized)
                    """, connection, transaction);
                package.Parameters.AddWithValue("preparation", Guid.NewGuid());
                package.Parameters.AddWithValue("package", packageId);
                package.Parameters.AddWithValue("equality", RandomNumberGenerator.GetBytes(32));
                package.Parameters.AddWithValue("assembly", Guid.NewGuid());
                package.Parameters.AddWithValue("job", lineage.JobId);
                package.Parameters.AddWithValue("attempt", lineage.AttemptId);
                package.Parameters.AddWithValue("fence", lineage.FencingToken);
                package.Parameters.AddWithValue("assemblyFingerprint", RandomNumberGenerator.GetBytes(32));
                package.Parameters.AddWithValue("manifest", RandomNumberGenerator.GetBytes(32));
                package.Parameters.AddWithValue("assemblyDigest", RandomNumberGenerator.GetBytes(32));
                package.Parameters.AddWithValue("authentication", RandomNumberGenerator.GetBytes(32));
                package.Parameters.AddWithValue("client", seed.ClientId);
                package.Parameters.AddWithValue("key", keyId);
                package.Parameters.AddWithValue("keyFingerprint", fingerprint);
                package.Parameters.AddWithValue("spki", spki);
                package.Parameters.AddWithValue("validFrom", baseTime.AddHours(-1));
                package.Parameters.AddWithValue("validUntil", baseTime.AddHours(2));
                package.Parameters.AddWithValue("operation", RandomNumberGenerator.GetBytes(32));
                package.Parameters.AddWithValue("endpoint", RandomNumberGenerator.GetBytes(32));
                package.Parameters.AddWithValue("objectKey", $"raw-export/c2-package/v1/{packageId:N}");
                package.Parameters.AddWithValue("binding", RandomNumberGenerator.GetBytes(32));
                package.Parameters.AddWithValue("envelope", RandomNumberGenerator.GetBytes(32));
                package.Parameters.AddWithValue("ciphertext", RandomNumberGenerator.GetBytes(32));
                package.Parameters.AddWithValue("createEvidence", RandomNumberGenerator.GetBytes(32));
                package.Parameters.AddWithValue("receipt", RandomNumberGenerator.GetBytes(32));
                package.Parameters.AddWithValue("frozen", baseTime.AddMinutes(-2));
                package.Parameters.AddWithValue("putStarted", baseTime.AddMinutes(-1));
                package.Parameters.AddWithValue("prepared", baseTime.AddSeconds(-1));
                package.Parameters.AddWithValue("finalized", seed.FinalizedAtUtc);
                await package.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }
        catch
        {
            await CleanupC4PackagesAsync();
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(spki);
            CryptographicOperations.ZeroMemory(fingerprint);
        }
    }

    private async Task CleanupC4PackagesAsync()
    {
        await using var connection = await OpenAsync();
        await using var cleanup = new NpgsqlCommand("""
            DELETE FROM tagekyc.raw_export_recipient_package_events e
            USING tagekyc.raw_export_recipient_package_preparations p
            WHERE e."C2PreparationId"=p."C2PreparationId"
              AND p."RecipientClientApplicationId" IN (@clientA,@clientB);
            DELETE FROM tagekyc.raw_export_recipient_package_preparations
            WHERE "RecipientClientApplicationId" IN (@clientA,@clientB);
            DELETE FROM tagekyc.raw_export_recipient_key_registrations
            WHERE "RecipientClientApplicationId" IN (@clientA,@clientB);
            """, connection);
        cleanup.Parameters.AddWithValue("clientA", C4ClientA);
        cleanup.Parameters.AddWithValue("clientB", C4ClientB);
        await cleanup.ExecuteNonQueryAsync();
    }

    private async Task DeleteC4PackagesForClientAsync(Guid clientApplicationId)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            "DELETE FROM tagekyc.raw_export_recipient_package_preparations WHERE \"RecipientClientApplicationId\"=@client", connection);
        command.Parameters.AddWithValue("client", clientApplicationId);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> CountEligibleC4PackagesForClientAsync(Guid clientApplicationId)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT count(*)::int
            FROM tagekyc.raw_export_recipient_package_preparations
            WHERE "RecipientClientApplicationId"=@client
              AND "State"='Finalized'
              AND "FinalizedAtUtc" IS NOT NULL
              AND "PackageId"<>'00000000-0000-0000-0000-000000000000'::uuid
            """, connection);
        command.Parameters.AddWithValue("client", clientApplicationId);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private async Task<int> CountC3DeliveriesForPackageAsync(Guid packageId)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT count(*)::int FROM tagekyc.raw_export_recipient_package_deliveries WHERE \"PackageId\"=@package", connection);
        command.Parameters.AddWithValue("package", packageId);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private async Task<string> C4StablePersistenceFingerprintAsync()
    {
        string[] expectedTables =
        [
            "raw_export_recipient_key_registrations",
            "raw_export_recipient_package_deliveries",
            "raw_export_recipient_package_delivery_events",
            "raw_export_recipient_package_events",
            "raw_export_recipient_package_preparations",
        ];
        await using var connection = await OpenAsync();
        await using (var census = new NpgsqlCommand("""
            SELECT tablename
            FROM pg_catalog.pg_tables
            WHERE schemaname='tagekyc'
              AND (tablename='raw_export_recipient_key_registrations'
                   OR tablename LIKE 'raw_export_recipient_package%')
            ORDER BY tablename
            """, connection))
        await using (var reader = await census.ExecuteReaderAsync())
        {
            var actual = new List<string>();
            while (await reader.ReadAsync()) actual.Add(reader.GetString(0));
            Assert.Equal(expectedTables, actual);
            Assert.DoesNotContain(actual, table => table.Contains("reference", StringComparison.OrdinalIgnoreCase)
                || table.Contains("issuance", StringComparison.OrdinalIgnoreCase)
                || table.Contains("audit", StringComparison.OrdinalIgnoreCase));
        }

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var table in expectedTables)
        {
            var tableBytes = Encoding.UTF8.GetBytes(table);
            hash.AppendData(BitConverter.GetBytes(tableBytes.Length));
            hash.AppendData(tableBytes);
            await using var command = new NpgsqlCommand(
                $"SELECT row_to_json(t)::text FROM tagekyc.\"{table}\" t ORDER BY row_to_json(t)::text", connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = Encoding.UTF8.GetBytes(reader.GetString(0));
                hash.AppendData(BitConverter.GetBytes(row.Length));
                hash.AppendData(row);
            }
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private async Task<IReadOnlyList<Guid>> ReadC4GlobalOwnerOrderAsync(int count)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT "RecipientClientApplicationId"
            FROM tagekyc.raw_export_recipient_package_preparations
            WHERE "RecipientClientApplicationId" IN (@clientA,@clientB)
              AND "State"='Finalized'
              AND "FinalizedAtUtc" IS NOT NULL
              AND "PackageId"<>'00000000-0000-0000-0000-000000000000'::uuid
            ORDER BY "FinalizedAtUtc" DESC,"PackageId" DESC
            LIMIT @count
            """, connection);
        command.Parameters.AddWithValue("clientA", C4ClientA);
        command.Parameters.AddWithValue("clientB", C4ClientB);
        command.Parameters.AddWithValue("count", count);
        var result = new List<Guid>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(reader.GetGuid(0));
        return result;
    }

    private static async Task<C411ObjectProvenance> ReadC411ObjectProvenanceAsync(
        S3CompatibleRecipientPackageProvider store,
        TagEkyc.Infrastructure.Persistence.Entities.RawExportRecipientPackagePreparationRow package)
    {
        byte[]? bytes = null;
        byte[]? ciphertextDigest = null;
        byte[]? envelopeDigest = null;
        try
        {
            var locator = new RecipientPackageLocator(
                package.ProviderKind, package.ProviderConfigurationId,
                package.ProviderEndpointFingerprint, package.BucketName,
                package.ObjectKey, package.ObjectBindingDigest);
            await using var source = await store.OpenReadAsync(locator, CancellationToken.None);
            await using var copy = new MemoryStream();
            await source.CopyToAsync(copy);
            bytes = copy.ToArray();
            ciphertextDigest = SHA256.HashData(bytes);
            using var envelope = new MemoryStream(bytes, writable: false);
            var envelopeValid = RecipientPackageCodec.TryReadEnvelopeDigest(envelope, out envelopeDigest);
            return new(
                true,
                bytes.LongLength,
                package.EncryptedPackageLength == bytes.LongLength,
                package.PackageCiphertextDigest is { Length: 32 }
                    && CryptographicOperations.FixedTimeEquals(ciphertextDigest, package.PackageCiphertextDigest),
                envelopeValid && package.EnvelopeDigest is { Length: 32 }
                    && CryptographicOperations.FixedTimeEquals(envelopeDigest, package.EnvelopeDigest),
                null);
        }
        catch (Exception exception)
        {
            return new(false, null, false, false, false,
                $"{exception.GetType().Name}:{exception.Message}");
        }
        finally
        {
            if (bytes is not null) CryptographicOperations.ZeroMemory(bytes);
            if (ciphertextDigest is not null) CryptographicOperations.ZeroMemory(ciphertextDigest);
            if (envelopeDigest is not null) CryptographicOperations.ZeroMemory(envelopeDigest);
        }
    }

    private sealed record C411ObjectProvenance(
        bool Exists,
        long? Length,
        bool LengthMatches,
        bool CiphertextDigestMatches,
        bool EnvelopeDigestMatches,
        string? Error);

    private sealed record C4HttpObservation(
        HttpStatusCode Status,
        int ItemCount,
        string? NextCursor,
        IReadOnlyList<Guid> PackageIds,
        string? ErrorCode)
    {
        public bool IsPage(int count, bool? cursor = null) =>
            Status == HttpStatusCode.OK && ItemCount == count
            && (cursor is null || (NextCursor is not null) == cursor.Value);

        public bool IsError(HttpStatusCode status, string code) =>
            Status == status && string.Equals(ErrorCode, code, StringComparison.Ordinal)
            && ItemCount == 0 && NextCursor is null;

        public override string ToString() =>
            $"status={(int)Status},count={ItemCount},cursor={NextCursor is not null},code={ErrorCode ?? "<null>"}";
    }

    private async Task<NpgsqlConnection> OpenAsync()
    { var connection = new NpgsqlConnection(postgres.ConnectionString); await connection.OpenAsync(); return connection; }
    private static void AssertOrdered(string value, params string[] items)
    { var offset = -1; foreach (var item in items) { var next = value.IndexOf(item, offset + 1, StringComparison.Ordinal); Assert.True(next > offset, item); offset = next; } }
    private static string Between(string value, string start, string end)
    { var first=value.IndexOf(start,StringComparison.Ordinal); var last=value.IndexOf(end,first+start.Length,StringComparison.Ordinal); return value[first..last]; }
    private static string Text(string path) => File.ReadAllText(PathOf(path));

    private async Task AssertC4CatalogPostureRejectedAsync(string purpose, string mutationSql, string expectedCode)
    {
        var source = new NpgsqlConnectionStringBuilder(postgres.ConnectionString);
        var sourceDatabase = source.Database ?? throw new InvalidOperationException("C415 source database is missing.");
        var databaseName = $"tagekyc_{purpose}_{Guid.NewGuid():N}";
        var admin = new NpgsqlConnectionStringBuilder(postgres.ConnectionString) { Database = "postgres", Pooling = false };
        var isolated = new NpgsqlConnectionStringBuilder(postgres.ConnectionString) { Database = databaseName, Pooling = false };
        NpgsqlConnection.ClearAllPools();
        await using (var connection = new NpgsqlConnection(admin.ConnectionString))
        {
            await connection.OpenAsync();
            await using var create = new NpgsqlCommand(
                $"CREATE DATABASE \"{databaseName}\" TEMPLATE \"{sourceDatabase.Replace("\"", "\"\"")}\"", connection);
            await create.ExecuteNonQueryAsync();
        }
        try
        {
            await using (var connection = new NpgsqlConnection(isolated.ConnectionString))
            {
                await connection.OpenAsync();
                await using var mutation = new NpgsqlCommand(mutationSql, connection);
                await mutation.ExecuteNonQueryAsync();
            }
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(C4ValidConfiguration(isolated.ConnectionString)).Build();
            using var services = new ServiceCollection().AddTagEkycRecipientPackageReference(configuration).BuildServiceProvider();
            Exception? readinessFailure = null;
            try
            {
                await services.GetRequiredService<RecipientPackageReferenceReadinessValidator>().ValidateAsync(default);
            }
            catch (Exception caught) { readinessFailure = caught; }
            Assert.True(readinessFailure?.GetType() == typeof(RecipientPackageReferenceReadinessException),
                $"C415-{purpose}-READINESS-REJECTION expected={typeof(RecipientPackageReferenceReadinessException).FullName} actual={readinessFailure?.GetType().FullName ?? "<none>"}");
            var error = (RecipientPackageReferenceReadinessException)readinessFailure!;
            Assert.True(string.Equals(expectedCode, error.Code, StringComparison.Ordinal),
                $"C415-{purpose}-CODE expected={expectedCode} actual={error.Code}");
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var connection = new NpgsqlConnection(admin.ConnectionString);
            await connection.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private async Task AssertC4OutboundMembershipRejectedAsync()
    {
        var probeRole = $"c4_acl_probe_{Guid.NewGuid():N}";
        var probeCreated = false;
        try
        {
            await using (var connection = await OpenAsync())
            {
                await using (var create = new NpgsqlCommand(
                    $"CREATE ROLE \"{probeRole}\" NOLOGIN INHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS", connection))
                    await create.ExecuteNonQueryAsync();
                probeCreated = true;
                await using var grant = new NpgsqlCommand(
                    $"GRANT \"{probeRole}\" TO tagekyc_raw_export_package_reference", connection);
                await grant.ExecuteNonQueryAsync();
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(C4ValidConfiguration(postgres.ConnectionString)).Build();
            using var services = new ServiceCollection()
                .AddTagEkycRecipientPackageReference(configuration).BuildServiceProvider();
            Exception? readinessFailure = null;
            try
            {
                await services.GetRequiredService<RecipientPackageReferenceReadinessValidator>().ValidateAsync(default);
            }
            catch (Exception caught) { readinessFailure = caught; }
            Assert.True(readinessFailure?.GetType() == typeof(RecipientPackageReferenceReadinessException),
                $"C414-OUTBOUND-MEMBERSHIP-READINESS-REJECTION expected={typeof(RecipientPackageReferenceReadinessException).FullName} actual={readinessFailure?.GetType().FullName ?? "<none>"}");
            var error = (RecipientPackageReferenceReadinessException)readinessFailure!;
            Assert.True(string.Equals(RecipientPackageReferenceReadinessValidator.Codes[1], error.Code, StringComparison.Ordinal),
                $"C414-OUTBOUND-MEMBERSHIP-CODE expected={RecipientPackageReferenceReadinessValidator.Codes[1]} actual={error.Code}");
        }
        finally
        {
            if (probeCreated)
            {
                await using var cleanupConnection = await OpenAsync();
                await using var cleanup = new NpgsqlCommand($"""
                    REVOKE "{probeRole}" FROM tagekyc_raw_export_package_reference;
                    DROP ROLE "{probeRole}";
                    """, cleanupConnection);
                await cleanup.ExecuteNonQueryAsync();
            }
        }

        await using var verification = await OpenAsync();
        await using var absent = new NpgsqlCommand(
            "SELECT count(*)=0 FROM pg_roles WHERE rolname=@name", verification);
        absent.Parameters.AddWithValue("name", probeRole);
        var probeAbsent = (bool)(await absent.ExecuteScalarAsync())!;
        Assert.True(probeAbsent, $"C414-OUTBOUND-MEMBERSHIP-PROBE-CLEANUP observed={probeAbsent}");
    }

    private async Task AssertC4RealCatalogSiblingOwnershipAsync()
    {
        var isolated = await CreateC4DisposableDatabaseAsync("c4_catalog_sibling");
        try
        {
            await using (var connection = new NpgsqlConnection(isolated.ConnectionString))
            {
                await connection.OpenAsync();
                await using (var mutation = new NpgsqlCommand("""
                    CREATE FUNCTION tagekyc.raw_export_list_recipient_package_references_shadow(
                      uuid,timestamp with time zone,uuid,integer)
                    RETURNS TABLE("PackageId" uuid,"FinalizedAtUtc" timestamp with time zone)
                    LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog
                    AS 'SELECT NULL::uuid,NULL::timestamp with time zone WHERE false';
                    ALTER FUNCTION tagekyc.raw_export_list_recipient_package_references_shadow(
                      uuid,timestamp with time zone,uuid,integer) OWNER TO tagekyc_raw_export_deployer;
                    REVOKE ALL ON FUNCTION tagekyc.raw_export_list_recipient_package_references_shadow(
                      uuid,timestamp with time zone,uuid,integer) FROM PUBLIC;
                    GRANT EXECUTE ON FUNCTION tagekyc.raw_export_list_recipient_package_references_shadow(
                      uuid,timestamp with time zone,uuid,integer) TO tagekyc_raw_export_package_reference;
                    CREATE INDEX ix_raw_export_recipient_package_reference_list_shadow
                      ON tagekyc.raw_export_recipient_package_preparations
                      ("RecipientClientApplicationId", "FinalizedAtUtc" DESC, "PackageId" DESC)
                      WHERE "State"='Finalized' AND "FinalizedAtUtc" IS NOT NULL;
                    ALTER INDEX tagekyc.ix_raw_export_recipient_package_reference_list_shadow
                      OWNER TO tagekyc_raw_export_deployer;
                    """, connection))
                    await mutation.ExecuteNonQueryAsync();

                await using (var existence = new NpgsqlCommand("""
                    SELECT
                      (SELECT count(*)=1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                       WHERE n.nspname='tagekyc' AND p.proname='raw_export_list_recipient_package_references_shadow')
                      AND
                      (SELECT count(*)=1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                       WHERE n.nspname='tagekyc' AND c.relname='ix_raw_export_recipient_package_reference_list_shadow'
                         AND c.relkind='i')
                    """, connection))
                    Assert.True((bool)(await existence.ExecuteScalarAsync())!, "C418-CATALOG-SIBLING-PRECONDITION");

                var ownedFunctions = await DiscoverStringsAsync(connection, """
                    SELECT p.proname FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                    WHERE n.nspname='tagekyc'
                      AND p.proname IN ('raw_export_list_recipient_package_references')
                    ORDER BY p.proname
                    """);
                var ownedIndexes = await DiscoverStringsAsync(connection, """
                    SELECT c.relname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname='tagekyc' AND c.relkind='i'
                      AND c.relname IN ('ix_raw_export_recipient_package_reference_list')
                    ORDER BY c.relname
                    """);
                AssertExactOwnedSet("C418-CATALOG-FUNCTION-OWNERSHIP",
                    ["raw_export_list_recipient_package_references"], ownedFunctions);
                AssertExactOwnedSet("C418-CATALOG-INDEX-OWNERSHIP",
                    ["ix_raw_export_recipient_package_reference_list"], ownedIndexes);
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(C4ValidConfiguration(isolated.ConnectionString)).Build();
            using var services = new ServiceCollection().AddTagEkycRecipientPackageReference(configuration).BuildServiceProvider();
            RecipientPackageReferenceReadinessException? failure = null;
            try
            {
                await services.GetRequiredService<RecipientPackageReferenceReadinessValidator>().ValidateAsync(default);
            }
            catch (RecipientPackageReferenceReadinessException exception)
            {
                failure = exception;
            }
            Assert.True(failure is null,
                $"C418-CATALOG-SIBLING-CONTROL readiness={failure?.Code ?? "PASS"}");
        }
        finally
        {
            await isolated.DisposeAsync();
        }
    }

    private async Task<C4DisposableDatabase> CreateC4DisposableDatabaseAsync(string purpose)
    {
        var source = new NpgsqlConnectionStringBuilder(postgres.ConnectionString);
        var sourceDatabase = source.Database ?? throw new InvalidOperationException("C4 source database is missing.");
        var databaseName = $"tagekyc_{purpose}_{Guid.NewGuid():N}";
        var admin = new NpgsqlConnectionStringBuilder(postgres.ConnectionString) { Database = "postgres", Pooling = false };
        var isolated = new NpgsqlConnectionStringBuilder(postgres.ConnectionString) { Database = databaseName, Pooling = false };
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();
        await using var create = new NpgsqlCommand(
            $"CREATE DATABASE \"{databaseName}\" TEMPLATE \"{sourceDatabase.Replace("\"", "\"\"")}\"", connection);
        await create.ExecuteNonQueryAsync();
        return new C4DisposableDatabase(admin.ConnectionString, isolated.ConnectionString, databaseName);
    }

    private static async Task<bool> C4DatabaseExistsAsync(string adminConnectionString, string databaseName)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*)=1 FROM pg_database WHERE datname=@name", connection);
        command.Parameters.AddWithValue("name", databaseName);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static async Task ForceDropC4DatabaseAsync(string adminConnectionString, string databaseName)
    {
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class C4DisposableDatabase(
        string adminConnectionString,
        string connectionString,
        string databaseName) : IAsyncDisposable
    {
        private bool disposed;
        public string AdminConnectionString { get; } = adminConnectionString;
        public string ConnectionString { get; } = connectionString;
        public string DatabaseName { get; } = databaseName;

        public async ValueTask DisposeAsync()
        {
            if (disposed) return;
            disposed = true;
            await ForceDropC4DatabaseAsync(AdminConnectionString, DatabaseName);
        }
    }

    private static Dictionary<string, string?> C4ValidConfiguration(string connectionString)
    {
        var material = Convert.ToBase64String(new byte[32]).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return new(StringComparer.Ordinal)
        {
            [$"{RecipientPackageReferenceOptions.SectionPath}:Topology"] = "PostgresDurable",
            [$"{RecipientPackageReferenceOptions.SectionPath}:DatabaseConnectionString"] = connectionString,
            [$"{RecipientPackageReferenceOptions.SectionPath}:ActiveCursorKeyId"] = "c4-cursor-01",
            [$"{RecipientPackageReferenceOptions.SectionPath}:ActiveCursorKeyVersion"] = "1",
            [$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyId"] = "c4-cursor-01",
            [$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyVersion"] = "1",
            [$"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:c4-cursor-01:1"] = material,
        };
    }

    private static Dictionary<string, string?> C4With(
        Dictionary<string, string?> source,
        params (string Key, string? Value)[] changes)
    {
        var result = new Dictionary<string, string?>(source, StringComparer.Ordinal);
        foreach (var (key, value) in changes)
        {
            if (value is null) result.Remove(key);
            else result[key] = value;
        }
        return result;
    }

    private static async Task AssertInvalidC4ConfigurationShortCircuitsAsync(
        Dictionary<string, string?> values,
        string discriminator,
        AuthenticatedClientContext actor)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var options = RecipientPackageReferenceOptions.Resolve(configuration);
        Assert.True(options.Topology == RecipientPackageReferenceTopology.Invalid && !options.IsSyntacticallyValid,
            $"C420-{discriminator}-TOPOLOGY=[{options.Topology},{options.IsSyntacticallyValid}]");
        using var services = new ServiceCollection().AddTagEkycRecipientPackageReference(configuration).BuildServiceProvider();
        var factory = services.GetService<IRecipientPackageReferenceConnectionFactory>();
        Assert.True(factory is null, $"C420-{discriminator}-CONNECTION-FACTORY actual={factory?.GetType().FullName ?? "<null>"}");
        var keyService = services.GetService<RecipientPackageReferenceCursorKeyService>();
        Assert.True(keyService is null, $"C420-{discriminator}-KEY-SERVICE actual={keyService?.GetType().FullName ?? "<null>"}");
        var result = await services.GetRequiredService<IRecipientPackageReferenceGateway>()
            .ListAsync(actor, new([], [], []), default);
        Assert.True(!result.IsSuccess && result.Value is null
            && result.Error?.Code == RecipientPackageReferenceErrorCodes.Unavailable,
            $"C420-{discriminator}-SHORT-CIRCUIT=[success={result.IsSuccess},value={result.Value is not null},code={result.Error?.Code}]");
    }

    private static bool C4IdentifiersFitPostgres(IEnumerable<string> identifiers) =>
        identifiers.All(value => Encoding.UTF8.GetByteCount(value) <= 63);

    private static bool C4NormalizedCatalogTextEquals(string expected, string actual) =>
        string.Equals(expected.ReplaceLineEndings("\n"), actual.ReplaceLineEndings("\n"), StringComparison.Ordinal);

    private static bool C4OneSidedCatalogTextEquals(string expected, string actual) =>
        string.Equals(expected.ReplaceLineEndings("\n"), actual, StringComparison.Ordinal);

    private static async Task<string[]> DiscoverStringsAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync()) values.Add(reader.GetString(0));
        return values.ToArray();
    }

    private static bool C4OwnedSetEquals(IEnumerable<string> expected, IEnumerable<string> actual) =>
        expected.OrderBy(value => value, StringComparer.Ordinal)
            .SequenceEqual(actual.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal);

    private static void AssertExactOwnedSet(string discriminator, IEnumerable<string> expected, IEnumerable<string> actual)
    {
        var expectedSet = expected.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var actualSet = actual.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        Assert.True(expectedSet.SequenceEqual(actualSet, StringComparer.Ordinal),
            $"{discriminator} expected=[{string.Join(",", expectedSet)}] actual=[{string.Join(",", actualSet)}]");
    }

    private static void AssertExactProperties(Type type, params (string Name, Type Type)[] expected)
    {
        var expectedProperties = expected.OrderBy(value => value.Name, StringComparer.Ordinal).ToArray();
        var actualProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => (property.Name, property.PropertyType)).OrderBy(value => value.Name, StringComparer.Ordinal).ToArray();
        Assert.True(expectedProperties.SequenceEqual(actualProperties),
            $"C402-PUBLIC-PROPERTIES-{type.Name} expected=[{string.Join(",", expectedProperties.Select(value => value.Name))}] actual=[{string.Join(",", actualProperties.Select(value => value.Name))}]");
    }

    private static void AssertExactPublicMethod(Type type, string name, Type result, params Type[] parameters)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(candidate => !candidate.IsSpecialName).ToArray();
        Assert.True(methods.Length == 1,
            $"C402-PUBLIC-METHOD-CENSUS-{type.Name} expected=1 actual={methods.Length} names=[{string.Join(",", methods.Select(value => value.Name))}]");
        var method = methods[0];
        Assert.True(string.Equals(name, method.Name, StringComparison.Ordinal),
            $"C402-PUBLIC-METHOD-NAME-{type.Name} expected={name} actual={method.Name}");
        Assert.True(method.ReturnType == result,
            $"C402-PUBLIC-METHOD-RETURN-{type.Name} expected={result} actual={method.ReturnType}");
        var actualParameters = method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        Assert.True(parameters.SequenceEqual(actualParameters),
            $"C402-PUBLIC-METHOD-PARAMETERS-{type.Name} expected=[{string.Join(",", parameters.Select(value => value.Name))}] actual=[{string.Join(",", actualParameters.Select(value => value.Name))}]");
    }

    private static IEnumerable<Type> C4RuntimeDependencies(
        ICollection<string> unresolvedTokens,
        Func<Module, int, Type[]?, Type[]?, MemberInfo?>? resolver = null)
    {
        var roots = new[]
        {
            typeof(RecipientPackageReferenceEndpoints).Assembly,
            typeof(RecipientPackageReferenceDto).Assembly,
            typeof(IRecipientPackageReferenceApplicationService).Assembly,
            typeof(RecipientPackageReferenceOptions).Assembly,
        }.Distinct();
        var c4Types = roots.SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.FullName?.Contains("RecipientPackageReference", StringComparison.Ordinal) == true)
            .ToArray();
        foreach (var type in c4Types)
        {
            foreach (var dependency in ExpandType(type.BaseType)) yield return dependency;
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                foreach (var dependency in ExpandType(field.FieldType)) yield return dependency;
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                foreach (var dependency in ExpandType(property.PropertyType)) yield return dependency;
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Cast<MethodBase>()
                         .Concat(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)))
            {
                if (method is MethodInfo info)
                    foreach (var dependency in ExpandType(info.ReturnType)) yield return dependency;
                foreach (var parameter in method.GetParameters())
                    foreach (var dependency in ExpandType(parameter.ParameterType)) yield return dependency;
                foreach (var member in ReferencedMembers(method, unresolvedTokens, resolver))
                {
                    if (member is Type referencedType)
                        foreach (var dependency in ExpandType(referencedType)) yield return dependency;
                    foreach (var dependency in ExpandType(member.DeclaringType)) yield return dependency;
                    if (member is FieldInfo referencedField)
                        foreach (var dependency in ExpandType(referencedField.FieldType)) yield return dependency;
                    if (member is MethodInfo referencedMethod)
                    {
                        foreach (var dependency in ExpandType(referencedMethod.ReturnType)) yield return dependency;
                        foreach (var parameter in referencedMethod.GetParameters())
                            foreach (var dependency in ExpandType(parameter.ParameterType)) yield return dependency;
                    }
                }
            }
        }
    }

    private static IEnumerable<Type> ExpandType(Type? type)
    {
        if (type is null) yield break;
        yield return type;
        if (type.HasElementType)
            foreach (var nested in ExpandType(type.GetElementType())) yield return nested;
        if (type.IsGenericType)
            foreach (var argument in type.GetGenericArguments())
                foreach (var nested in ExpandType(argument)) yield return nested;
    }

    private static bool IsForbiddenC4ProviderDependency(Type type)
    {
        var name = type.FullName ?? type.Name;
        return name.StartsWith("Amazon.", StringComparison.Ordinal)
            || name.Contains("S3CompatibleRecipientPackage", StringComparison.Ordinal)
            || name.Contains("RecipientPackageObjectClient", StringComparison.Ordinal)
            || name.Contains("RecipientPackagePreparationProvider", StringComparison.Ordinal);
    }

    private static IEnumerable<MemberInfo> ReferencedMembers(
        MethodBase method,
        ICollection<string> unresolvedTokens,
        Func<Module, int, Type[]?, Type[]?, MemberInfo?>? resolver)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il is null) yield break;
        var oneByte = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (OpCode)field.GetValue(null)!).Where(code => code.Size == 1)
            .ToDictionary(code => unchecked((byte)code.Value));
        var twoByte = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (OpCode)field.GetValue(null)!).Where(code => code.Size == 2)
            .ToDictionary(code => unchecked((byte)(code.Value & 0xff)));
        for (var offset = 0; offset < il.Length;)
        {
            var first = il[offset++];
            var code = first == 0xfe ? twoByte[il[offset++]] : oneByte[first];
            var operandStart = offset;
            var operandSize = code.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI or OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineMethod
                    or OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType
                    or OperandType.ShortInlineR => 4,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + (BitConverter.ToInt32(il, offset) * 4),
                _ => throw new InvalidOperationException($"Unsupported IL operand {code.OperandType}.")
            };
            if (code.OperandType is OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineTok or OperandType.InlineType)
            {
                var token = BitConverter.ToInt32(il, operandStart);
                MemberInfo? member = null;
                try
                {
                    var typeArguments = method.DeclaringType?.GetGenericArguments();
                    var methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;
                    member = resolver is null
                        ? method.Module.ResolveMember(token, typeArguments, methodArguments)
                        : resolver(method.Module, token, typeArguments, methodArguments);
                }
                catch (Exception exception) when (exception is ArgumentException or BadImageFormatException)
                {
                    unresolvedTokens.Add(
                        $"{method.DeclaringType?.FullName ?? "<global>"}.{method.Name}@IL_{operandStart:X4}:0x{token:X8}:{exception.GetType().Name}");
                }
                if (member is not null) yield return member;
            }
            offset += operandSize;
        }
    }
    private static string PathOf(string relative)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TagEkyc.sln"))) root = root.Parent;
        return Path.Combine(root?.FullName ?? throw new InvalidOperationException("Repository root not found."), relative.Replace('/', Path.DirectorySeparatorChar));
    }

    private async Task InsertActiveKeyAsync(Guid recipientId, string keyId)
    {
        using var rsa = RSA.Create(3072);
        var spki = rsa.ExportSubjectPublicKeyInfo(); var fingerprint = SHA256.HashData(spki);
        try
        {
            await ExecuteAsync("UPDATE tagekyc.raw_export_recipient_key_registrations SET \"State\"='Revoked',\"Revision\"=\"Revision\"+1,\"RevokedAtUtc\"=clock_timestamp(),\"RevocationReason\"='C4_TEST_REVOKE' WHERE \"RecipientClientApplicationId\"=@id AND \"State\"='Active'", ("id", recipientId));
            await ExecuteAsync("INSERT INTO tagekyc.raw_export_recipient_key_registrations(\"RecipientClientApplicationId\",\"RecipientKeyId\",\"RecipientKeyVersion\",\"PublicKeyAlgorithm\",\"PublicKeySpki\",\"PublicKeyFingerprint\",\"ValidFromUtc\",\"ValidUntilUtc\",\"State\",\"Revision\",\"RegisteredAtUtc\") VALUES(@recipient,@key,1,'RSA-OAEP-256',@spki,@fp,clock_timestamp()-interval '1 minute',clock_timestamp()+interval '1 hour','Active',1,clock_timestamp())",
                ("recipient", recipientId), ("key", keyId), ("spki", spki), ("fp", fingerprint));
        }
        finally { CryptographicOperations.ZeroMemory(spki); CryptographicOperations.ZeroMemory(fingerprint); }
    }
    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] values)
    {
        await using var connection = await OpenAsync(); await using var command = new NpgsqlCommand(sql, connection);
        foreach (var value in values) command.Parameters.AddWithValue(value.Name, value.Value);
        await command.ExecuteNonQueryAsync();
    }
    private sealed class C4Authenticator(AuthenticatedClientContext actor) : IApiKeyAuthenticator
    {
        public Task<TagEkyc.Application.VerificationSessions.SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(
            HttpContext httpContext, string? requiredScope = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(TagEkyc.Application.VerificationSessions.SessionOperationResult<AuthenticatedClientContext>.Success(actor));
    }

    private sealed class C4RecordingConfigurationSource : IConfigurationSource
    {
        private readonly C4RecordingConfigurationProvider provider = new();
        public IConfigurationProvider Build(IConfigurationBuilder builder) => provider;
        internal void Seed(IReadOnlyDictionary<string, string?> values) => provider.Seed(values);
        internal void ClearReads() => provider.ClearReads();
        internal bool WasRead(string key) => provider.WasRead(key);
    }

    private sealed class C4RecordingConfigurationProvider : ConfigurationProvider
    {
        private readonly HashSet<string> reads = new(StringComparer.OrdinalIgnoreCase);
        internal void Seed(IReadOnlyDictionary<string, string?> values) =>
            Data = values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        public override bool TryGet(string key, out string? value)
        {
            lock (reads) reads.Add(key);
            return base.TryGet(key, out value);
        }
        internal void ClearReads() { lock (reads) reads.Clear(); }
        internal bool WasRead(string key) { lock (reads) return reads.Contains(key); }
    }
    private sealed class C401Gateway : IRecipientPackageReferenceGateway
    {
        public int CallCount { get; private set; }

        public Task<TagEkyc.Application.VerificationSessions.SessionOperationResult<RecipientPackageReferencePageDto>> ListAsync(
            AuthenticatedClientContext actor,
            RecipientPackageReferenceRawQuery query,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(
                TagEkyc.Application.VerificationSessions.SessionOperationResult<RecipientPackageReferencePageDto>
                    .Success(new([], null)));
        }
    }
    private sealed class C2RoleConnectionFactory(string connectionString) : IRecipientPackageConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(RecipientPackageDatabaseCapability capability, CancellationToken token)
        {
            var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(token);
            var role = capability switch { RecipientPackageDatabaseCapability.Preparer => "tagekyc_raw_export_package_preparer", RecipientPackageDatabaseCapability.Reconciler => "tagekyc_raw_export_package_reconciler", _ => "tagekyc_raw_export_package_lifecycle" };
            await new NpgsqlCommand($"SET ROLE {role}", connection).ExecuteNonQueryAsync(token); return connection;
        }
    }
    private sealed class AssemblyRoleConnectionFactory(string connectionString) : IRawExportAssemblyConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(RawExportAssemblyDatabaseCapability capability, CancellationToken token)
        {
            var role = capability == RawExportAssemblyDatabaseCapability.Resolver ? "tagekyc_raw_export_assembly_resolver" : "tagekyc_raw_export_assembly_sealer";
            var builder = new NpgsqlConnectionStringBuilder(connectionString) { Options = $"-c role={role}", Pooling = false };
            var connection = new NpgsqlConnection(builder.ConnectionString); await connection.OpenAsync(token); return connection;
        }
    }
    private sealed class DeliveryRoleConnectionFactory(string connectionString) : IRecipientPackageDeliveryConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(CancellationToken token)
        {
            var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync(token);
            await new NpgsqlCommand("SET ROLE tagekyc_raw_export_package_delivery", connection).ExecuteNonQueryAsync(token); return connection;
        }
    }
}
