using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

public sealed class RawExportControlPlaneApplicationTests
{
    private static readonly Guid PrincipalId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid ClientId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid ApiKeyId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid SessionId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid PolicyId = Guid.Parse("10000000-0000-0000-0000-000000000005");
    private static readonly Guid RecipientId = Guid.Parse("10000000-0000-0000-0000-000000000006");
    private static readonly Guid PermitId = Guid.Parse("10000000-0000-0000-0000-000000000007");
    private static readonly Guid DecisionId = Guid.Parse("10000000-0000-0000-0000-000000000008");
    private static readonly Guid JobId = Guid.Parse("10000000-0000-0000-0000-000000000009");
    private static readonly Guid PackageId = Guid.Parse("10000000-0000-0000-0000-000000000010");

    [Fact]
    public async Task Existing_authorization_and_job_repositories_are_the_only_write_boundaries()
    {
        var authorization = new AuthorizationRepository();
        var jobs = new JobRepository();
        var packages = new PackageReader();
        var service = new RawExportControlPlaneApplicationService(authorization, jobs, packages);
        var actor = Actor(
            RawExportControlPlaneApplicationService.AuthorizeScope,
            RawExportControlPlaneApplicationService.JobScope);

        var authorized = await service.AuthorizeAsync(actor,
            new(SessionId, PolicyId, 3, ["ChipDg2Portrait", "LiveSelfieImage"]),
            "authorize-1", default);
        var bound = await service.BindJobAsync(actor, new(PermitId), "job-1", default);
        var read = await service.ReadJobAsync(actor, JobId, default);

        Assert.True(authorized.IsSuccess);
        Assert.Equal(PermitId, authorized.Value!.PermitId);
        Assert.Equal(["ChipDg2Portrait", "LiveSelfieImage"], authorized.Value.AuthorizedRawClasses);
        Assert.Equal(JobId, bound.Value!.JobId);
        Assert.Equal(PackageId, read.Value!.PackageId);
        Assert.Equal(SessionId, read.Value.VerificationSessionId);
        Assert.Equal(1, authorization.Calls);
        Assert.Equal(2, jobs.Calls);
        Assert.Equal(1, packages.Calls);
    }

    [Fact]
    public async Task Scope_identity_and_exact_class_validation_precede_repository_access()
    {
        var authorization = new AuthorizationRepository();
        var jobs = new JobRepository();
        var packages = new PackageReader();
        var service = new RawExportControlPlaneApplicationService(authorization, jobs, packages);

        var forbidden = await service.AuthorizeAsync(
            Actor(), new(SessionId, PolicyId, 1), "a", default);
        var unknownClass = await service.AuthorizeAsync(
            Actor(RawExportControlPlaneApplicationService.AuthorizeScope),
            new(SessionId, PolicyId, 1, ["chipDg2Portrait"]), "b", default);
        var duplicateClass = await service.AuthorizeAsync(
            Actor(RawExportControlPlaneApplicationService.AuthorizeScope),
            new(SessionId, PolicyId, 1, ["ChipDg2Portrait", "ChipDg2Portrait"]), "c", default);
        var missingKey = await service.BindJobAsync(
            Actor(RawExportControlPlaneApplicationService.JobScope), new(PermitId), null, default);

        Assert.Equal(RawExportControlPlaneErrorCodes.Forbidden, forbidden.Error?.Code);
        Assert.Equal(RawExportControlPlaneErrorCodes.RequestInvalid, unknownClass.Error?.Code);
        Assert.Equal(RawExportControlPlaneErrorCodes.RequestInvalid, duplicateClass.Error?.Code);
        Assert.Equal(RawExportControlPlaneErrorCodes.RequestInvalid, missingKey.Error?.Code);
        Assert.Equal(0, authorization.Calls);
        Assert.Equal(0, jobs.Calls);
        Assert.Equal(0, packages.Calls);
    }

    private static AuthenticatedClientContext Actor(params string[] scopes) => new(
        ApiKeyId, ClientId, "key", AuthenticatedCallerCategory.BusinessConsumer,
        new HashSet<string>(scopes), PrincipalId: PrincipalId);

    private sealed class AuthorizationRepository : IRawExportAuthorizationRepository
    {
        public int Calls { get; private set; }

        public Task<RawExportAuthorizationResult> AuthorizeExportAsync(
            AuthorizeRawExportCommand command,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            var expires = DateTimeOffset.UtcNow.AddMinutes(5);
            var decision = new RawExportAuthorizationDecision(
                DecisionId, PrincipalId, ClientId, ApiKeyId, SessionId, PolicyId, 3,
                new byte[32], RawExportRawClassSelectionMode.ExplicitSubset,
                RawExportAuthorizationOutcome.Authorized, null, SessionId, ClientId,
                "subject", VerificationSessionState.Completed, 1, 1, null,
                DateTimeOffset.UtcNow, null, null, "Signing", RecipientId, null,
                null, null, null, null, DateTimeOffset.UtcNow, 300, expires,
                DateTimeOffset.UtcNow);
            var permit = new RawExportAuthorizationPermit(
                PermitId, DecisionId, SessionId, "subject", PolicyId, 3,
                "Signing", RecipientId, expires, 1, DateTimeOffset.UtcNow);
            return Task.FromResult(new RawExportAuthorizationResult(
                decision, [], [], [], permit,
                [new(RawExportRawClass.ChipDg2Portrait, 0), new(RawExportRawClass.LiveSelfieImage, 1)]));
        }
    }

    private sealed class JobRepository : IRawExportJobRepository
    {
        public int Calls { get; private set; }

        public Task<RawExportJobBindResult> BindAsync(BindRawExportJobCommand command, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new RawExportJobBindResult(RawExportJobBindStatus.NewJob, JobId, null));
        }

        public Task<RawExportJobReadResult> ReadAsync(ReadRawExportJobCommand command, CancellationToken cancellationToken = default)
        {
            Calls++;
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new RawExportJobReadResult(
                RawExportJobReadStatus.Found,
                new RawExportJobView(
                    new(JobId, PermitId, DecisionId, PrincipalId, ClientId, ApiKeyId,
                        SessionId, "subject", PolicyId, 3, "Signing", RecipientId,
                        RawExportMode.EncryptedExportPacket, now.AddMinutes(5), now.AddMinutes(5), 1, now),
                    [new(0, RawExportRawClass.ChipDg2Portrait), new(1, RawExportRawClass.LiveSelfieImage)],
                    new(RawExportJobState.PackageSealed, 4, null, null, null, 1),
                    new(RawExportJobEventType.AssemblySealed, null, now))));
        }

        public Task<RawExportJobLeaseResult> AcquireOrReclaimLeaseAsync(AcquireOrReclaimRawExportJobLeaseCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RawExportJobRenewResult> RenewLeaseAsync(RenewRawExportJobLeaseCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RawExportJobAttemptFailureResult> RecordAttemptFailureAsync(RecordRawExportJobAttemptFailureCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RawExportJobTerminalizeResult> TerminalizeAsync(TerminalizeRawExportJobCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class PackageReader : IRawExportJobPackageProjectionReader
    {
        public int Calls { get; private set; }
        public Task<RawExportJobPackageProjection?> ReadByJobAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<RawExportJobPackageProjection?>(
                new(PackageId, "Finalized", DateTimeOffset.UtcNow));
        }
    }
}
