using Microsoft.EntityFrameworkCore;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class RawExportJobClientIsolationTests(PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    public Task InitializeAsync() => postgres.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Same_principal_clients_cannot_read_each_others_job()
    {
        await using var db = postgres.CreateDbContext();
        var principal = Guid.Parse("88c10000-0000-5000-8000-00000000f101");
        var actorA = new AuthenticatedRawExportActor(
            principal,
            Guid.Parse("88c10000-0000-5000-8000-00000000f102"),
            Guid.Parse("88c10000-0000-5000-8000-00000000f103"));
        var actorB = new AuthenticatedRawExportActor(
            principal,
            Guid.Parse("88c10000-0000-5000-8000-00000000f104"),
            Guid.Parse("88c10000-0000-5000-8000-00000000f105"));
        var policyId = Guid.NewGuid();
        var classes = new[] { RawExportRawClass.LiveSelfieImage };

        var sessionA = await Tip88B34AuthorizationEngineTests.SeedCompletedSessionAsync(
            db, actorA.ClientApplicationId, $"subject:client-isolation:{Guid.NewGuid():N}");
        var sessionB = await Tip88B34AuthorizationEngineTests.SeedCompletedSessionAsync(
            db, actorB.ClientApplicationId, $"subject:client-isolation:{Guid.NewGuid():N}");
        await Tip88B34AuthorizationEngineTests.SeedActivePolicyAsync(
            db, policyId, classes, permitTtlSeconds: 300, principalId: principal);
        await Tip88B34AuthorizationEngineTests.SeedEffectiveConsentAsync(
            db, sessionA, policyId, classes, clientApplicationId: actorA.ClientApplicationId);
        await Tip88B34AuthorizationEngineTests.SeedEffectiveConsentAsync(
            db, sessionB, policyId, classes, clientApplicationId: actorB.ClientApplicationId);

        var authorizations = Tip88B34AuthorizationEngineTests.CreateRepository(db);
        var permitA = await authorizations.AuthorizeExportAsync(
            Tip88B34AuthorizationEngineTests.Command(
                sessionA, policyId, $"client-isolation-a-{Guid.NewGuid():N}", classes, actorA));
        var permitB = await authorizations.AuthorizeExportAsync(
            Tip88B34AuthorizationEngineTests.Command(
                sessionB, policyId, $"client-isolation-b-{Guid.NewGuid():N}", classes, actorB));
        Assert.NotNull(permitA.Permit);
        Assert.NotNull(permitB.Permit);

        var jobs = Tip88B4RawExportJobFoundationTests.CreateJobRepository(db);
        var jobA = await jobs.BindAsync(new(
            actorA, permitA.Permit!.PermitId, $"client-isolation-job-a-{Guid.NewGuid():N}"));
        var jobB = await jobs.BindAsync(new(
            actorB, permitB.Permit!.PermitId, $"client-isolation-job-b-{Guid.NewGuid():N}"));

        Assert.Equal(RawExportJobReadStatus.Found, (await jobs.ReadAsync(new(actorA, jobA.JobId))).Status);
        Assert.Equal(RawExportJobReadStatus.Found, (await jobs.ReadAsync(new(actorB, jobB.JobId))).Status);
        Assert.Equal(RawExportJobReadStatus.NotFound, (await jobs.ReadAsync(new(actorA, jobB.JobId))).Status);
        Assert.Equal(RawExportJobReadStatus.NotFound, (await jobs.ReadAsync(new(actorB, jobA.JobId))).Status);

        Assert.Equal(2, await db.RawExportJobIdentities.CountAsync());
    }
}
