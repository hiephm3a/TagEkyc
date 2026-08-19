using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C3RecipientPackageDeliveryApplicationTests
{
    [Fact]
    public async Task C301_persisted_principal_is_required_and_no_fallback_is_admitted()
    {
        var gateway = new FakeGateway();
        var service = new RecipientPackageDeliveryApplicationService(gateway);
        var empty = Actor(principal: Guid.Empty);
        Assert.Equal(RecipientPackageDeliveryErrorCodes.PrincipalRequired,
            (await service.CreateAsync(empty, Guid.NewGuid(), "key", "trace", default)).Error?.Code);
        Assert.Equal(RecipientPackageDeliveryErrorCodes.PrincipalRequired,
            (await service.ReadAsync(empty, Guid.NewGuid(), default)).Error?.Code);
        Assert.Equal(RecipientPackageDeliveryErrorCodes.PrincipalRequired,
            (await service.PrepareContentAsync(empty, Guid.NewGuid(), "trace", default)).Error?.Code);
        Assert.Equal(0, gateway.Calls);

        var persisted = Actor();
        await service.CreateAsync(persisted, Guid.NewGuid(), "key", "trace", default);
        await service.ReadAsync(persisted, Guid.NewGuid(), default);
        await service.PrepareContentAsync(persisted, Guid.NewGuid(), "trace", default);
        Assert.Equal(3, gateway.Calls);
    }

    [Fact]
    public async Task C302_category_scope_and_request_shape_precede_gateway_access()
    {
        var gateway = new FakeGateway();
        var service = new RecipientPackageDeliveryApplicationService(gateway);
        var forbidden = await service.CreateAsync(Actor(scopes: new HashSet<string>()), Guid.NewGuid(), "key", "trace", default);
        var invalid = await service.CreateAsync(Actor(), Guid.Empty, "key", "trace", default);
        var comma = await service.CreateAsync(Actor(), Guid.NewGuid(), "a,b", "trace", default);
        var emptyTrace = await service.CreateAsync(Actor(), Guid.NewGuid(), "key", " ", default);
        Assert.Equal(RecipientPackageDeliveryErrorCodes.Forbidden, forbidden.Error?.Code);
        Assert.Equal(RecipientPackageDeliveryErrorCodes.RequestInvalid, invalid.Error?.Code);
        Assert.Equal(RecipientPackageDeliveryErrorCodes.RequestInvalid, comma.Error?.Code);
        Assert.Equal(RecipientPackageDeliveryErrorCodes.Unavailable, emptyTrace.Error?.Code);
        Assert.Equal(0, gateway.Calls);

        var localDevKeys = new LocalDevApiKeyStore().ApiKeys;
        var existingBusiness = Assert.Single(localDevKeys, key => key.ApiKeyValue == "localdev-business-key");
        Assert.True(existingBusiness.Scopes.SetEquals(
            ["business.session.create", "business.session.read", "session.complete", "session.cancel"]));
        var deliveryIdentity = Assert.Single(localDevKeys,
            key => key.ApiKeyValue == "localdev-recipient-package-delivery-key");
        Assert.True(deliveryIdentity.Scopes.SetEquals([RecipientPackageDeliveryApplicationService.RequiredScope]));
        Assert.NotEqual(existingBusiness.ApiKeyId, deliveryIdentity.ApiKeyId);
        Assert.NotEqual(Guid.Empty, deliveryIdentity.PrincipalId);
    }

    private static AuthenticatedClientContext Actor(
        Guid? principal = null, IReadOnlySet<string>? scopes = null) => new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"), "key",
            AuthenticatedCallerCategory.BusinessConsumer,
            scopes ?? new HashSet<string> { RecipientPackageDeliveryApplicationService.RequiredScope },
            PrincipalId: principal ?? Guid.Parse("55555555-5555-5555-5555-555555555555"));

    private sealed class FakeGateway : IRecipientPackageDeliveryGateway
    {
        public int Calls { get; private set; }
        public Task<SessionOperationResult<RecipientPackageDeliveryCreation>> CreateAsync(AuthenticatedClientContext actor, Guid packageId, string idempotencyKey, byte[] correlationDigest, CancellationToken cancellationToken)
        { Calls++; return Task.FromResult(SessionOperationResult<RecipientPackageDeliveryCreation>.Failure("EXPECTED", "expected", 503)); }
        public Task<SessionOperationResult<RecipientPackageDeliveryDto>> ReadAsync(AuthenticatedClientContext actor, Guid deliveryId, CancellationToken cancellationToken)
        { Calls++; return Task.FromResult(SessionOperationResult<RecipientPackageDeliveryDto>.Failure("EXPECTED", "expected", 503)); }
        public Task<SessionOperationResult<RecipientPackageDeliveryContentLease>> PrepareContentAsync(AuthenticatedClientContext actor, Guid deliveryId, byte[] correlationDigest, CancellationToken cancellationToken)
        { Calls++; return Task.FromResult(SessionOperationResult<RecipientPackageDeliveryContentLease>.Failure("EXPECTED", "expected", 503)); }
    }
}
