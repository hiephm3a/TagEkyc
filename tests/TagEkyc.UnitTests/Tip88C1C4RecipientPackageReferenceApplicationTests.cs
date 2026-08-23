using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C4RecipientPackageReferenceApplicationTests
{
    [Fact]
    public async Task C419_public_contract_and_errors_disclose_no_owner_key_provider_or_count_metadata()
    {
        var properties = typeof(RecipientPackageReferencePageDto).GetProperties().Select(value => value.Name).ToArray();
        Assert.Equal(["Items", "NextCursor"], properties);
        var gateway = new CapturingGateway(); var service = Service(gateway);
        var result = await service.ListAsync(Actor(principal: Guid.Empty), new([], [], []), default);
        Assert.Equal(RecipientPackageReferenceErrorCodes.Forbidden, result.Error?.Code);
        Assert.DoesNotContain("principal", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static RecipientPackageReferenceApplicationService Service(CapturingGateway gateway) => new(gateway);
    private static AuthenticatedClientContext Actor(Guid? principal = null) => new(
        Guid.NewGuid(), Guid.Parse("11111111-1111-1111-1111-111111111111"), "key",
        AuthenticatedCallerCategory.BusinessConsumer,
        new HashSet<string> { RecipientPackageReferenceApplicationService.RequiredScope },
        PrincipalId: principal ?? Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private sealed class CapturingGateway : IRecipientPackageReferenceGateway
    {
        public List<RecipientPackageReferenceRawQuery> Queries { get; } = [];
        public List<AuthenticatedClientContext> Actors { get; } = [];
        public Task<SessionOperationResult<RecipientPackageReferencePageDto>> ListAsync(
            AuthenticatedClientContext actor, RecipientPackageReferenceRawQuery query,
            CancellationToken cancellationToken)
        {
            Actors.Add(actor); Queries.Add(query);
            return Task.FromResult(SessionOperationResult<RecipientPackageReferencePageDto>.Success(new([], null)));
        }
    }
}
