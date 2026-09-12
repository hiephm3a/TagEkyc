using System.Text.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using TagEkyc.Application;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.CaptureRuntime;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1RuntimeAppendTransactionTests(PostgresPersistenceFixture postgres)
{
    private static readonly Guid SessionId = Guid.Parse("90000000-0000-4000-8000-000000000001");
    private static readonly Guid ClientId = Guid.Parse("91000000-0000-4000-8000-000000000001");
    private static readonly AuthenticatedCaptureRuntimeContext Actor = new(
        Guid.Parse("40000000-0000-4000-8000-000000000001"),
        Guid.Parse("50000000-0000-4000-8000-000000000001"),
        Guid.Parse("60000000-0000-4000-8000-000000000001"), 1, new byte[32],
        Guid.NewGuid(), 1, 1, 1, 1, DateTimeOffset.UtcNow, new byte[32], new byte[32]);

    [Fact]
    public async Task R24R25_RealHelperPlannerWriter_CommitTogether_AndMismatchDoesNotWrite()
    {
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_append_commit");
        await using var db = isolated.CreateDbContext();
        var binding = await Prepare(db);
        var probe = Create(db);
        var key = Guid.NewGuid();
        var request = Capture(binding);
        var capture = await probe.Service.AppendCaptureArtifactAsync(Actor, binding, request, key, default);
        Assert.True(capture.IsSuccess, capture.Error?.Code);
        var replay = await probe.Service.AppendCaptureArtifactAsync(Actor, binding, request, key, default);
        Assert.True(replay.IsSuccess, replay.Error?.Code);
        Assert.True(replay.Value!.Deduplicated);
        Assert.Equal(capture.Value!.CaptureArtifactId, replay.Value.CaptureArtifactId);
        var evidenceRequest = Evidence(binding, capture.Value.CaptureArtifactId);
        var evidence = await probe.Service.AppendEvidenceResultAsync(Actor, SessionId, evidenceRequest, Guid.NewGuid(), default);
        Assert.True(evidence.IsSuccess, evidence.Error?.Code);
        var callsBefore = probe.WriterCalls;
        var mismatch = await probe.Service.AppendEvidenceResultAsync(Actor, Guid.NewGuid(), evidenceRequest, Guid.NewGuid(), default);
        Assert.Equal(403, mismatch.Error!.StatusCode);
        Assert.Equal("ACCESS_DENIED", mismatch.Error.Code);
        Assert.Equal(callsBefore, probe.WriterCalls);
        Assert.DoesNotContain(SessionId.ToString("N"), JsonSerializer.Serialize(mismatch));
        Assert.Null(db.Database.CurrentTransaction);
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(1, await observer.CaptureArtifacts.CountAsync(x => x.VerificationSessionId == SessionId));
        Assert.Equal(1, await observer.EvidenceResults.CountAsync(x => x.VerificationSessionId == SessionId));
        Assert.Equal(2, await observer.AppendIdempotencyRecords.CountAsync(x => x.VerificationSessionId == SessionId));
        // Reuse preserves both state changes (Created -> InProgress -> ReadyToComplete),
        // two recorded events and the existing deduplicated-replay audit.
        Assert.Equal(5, await observer.AuditEvents.CountAsync(x => x.VerificationSessionId == SessionId));
        Assert.Equal(2, await observer.AuditEvents.CountAsync(x => x.VerificationSessionId == SessionId && x.EventType == "SESSION_STATE_CHANGED"));
    }

    [Fact]
    public async Task R24_FaultAfterActualWrite_RollsBackArtifactIdempotencyAndAudit()
    {
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_append_rollback");
        await using var db = isolated.CreateDbContext();
        var binding = await Prepare(db);
        var probe = Create(db); probe.FailAfterWrite = true;
        await Assert.ThrowsAsync<InjectedPostWriteFault>(() => probe.Service.AppendCaptureArtifactAsync(
            Actor, binding, Capture(binding), Guid.NewGuid(), default));
        Assert.Equal(1, probe.WriterCalls);
        Assert.Null(db.Database.CurrentTransaction);
        await using var observer = isolated.CreateDbContext();
        Assert.Equal(0, await observer.CaptureArtifacts.CountAsync(x => x.VerificationSessionId == SessionId));
        Assert.Equal(0, await observer.AppendIdempotencyRecords.CountAsync(x => x.VerificationSessionId == SessionId));
        Assert.Equal(0, await observer.AuditEvents.CountAsync(x => x.VerificationSessionId == SessionId));
        Assert.Equal(nameof(VerificationSessionState.Created), (await observer.Sessions.SingleAsync(x => x.Id == SessionId)).State);
    }

    [Fact]
    public async Task A1_23_RealCrt1NonceCommits_BAuditFailureRollsBackOnlyAppend()
    {
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_nonce_append_rollback");
        await using var connectionOwner = isolated.CreateDbContext();
        A1SyntheticDbFailureInterceptor.Clear();
        await using var db = new TagEkycDbContext(new DbContextOptionsBuilder<TagEkycDbContext>()
            .UseNpgsql(connectionOwner.Database.GetConnectionString())
            .AddInterceptors(new A1SyntheticDbFailureInterceptor()).Options);
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var binding = await Prepare(db, signer.ExportSubjectPublicKeyInfo());
        var key = Guid.NewGuid(); var request = Capture(binding);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(request);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = bytes.Length;
        context.Request.Path = $"/api/ekyc/capture-runtime/executions/{binding:N}/capture-artifacts";
        context.Request.Headers[CaptureRuntimeCrt1RequestParser.CredentialIdHeader] = Actor.CredentialId.ToString("N");
        context.Request.Headers[CaptureRuntimeCrt1RequestParser.CredentialGenerationHeader] = "1";
        context.Request.Headers[CaptureRuntimeCrt1RequestParser.TimestampHeader] = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
        static string Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        context.Request.Headers[CaptureRuntimeCrt1RequestParser.NonceHeader] = Url(RandomNumberGenerator.GetBytes(32));
        context.Request.Headers[CaptureRuntimeCrt1RequestParser.SignatureHeader] = new string('A', 86);
        var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var operation = $"BindingId={binding:N};IdempotencyKey={key:N}";
        Assert.True(CaptureRuntimeCrt1RequestParser.TryCreate(context.Request, "CaptureObservation", "application/json", bytes.Length, digest, operation, out var unsigned));
        context.Request.Headers[CaptureRuntimeCrt1RequestParser.SignatureHeader] = Url(signer.SignData(unsigned!.ExactSignedPreimage.Span,
            HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        Assert.True(CaptureRuntimeCrt1RequestParser.TryCreate(context.Request, "CaptureObservation", "application/json", bytes.Length, digest, operation, out var signed));
        var connection = db.Database.GetConnectionString()!;
        var authConnection = new NpgsqlConnectionStringBuilder(connection) { Options = "-c role=tagekyc_capture_runtime_authenticator", Pooling = false }.ConnectionString;
        var authenticator = new CaptureRuntimeRequestAuthenticator(new CaptureRuntimeDbContextFactory(new(authConnection, connection)));
        var authenticated = await authenticator.AuthenticateAsync(signed!);
        Assert.True(authenticated.IsSuccess, authenticated.Error?.Code);
        Assert.Null(db.Database.CurrentTransaction);
        async Task<int> Nonces(TagEkycDbContext observer) => await observer.Database.SqlQueryRaw<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.capture_runtime_request_nonces
            WHERE "CredentialId"='{Actor.CredentialId}'
            """).SingleAsync();
        await using (var observer = isolated.CreateDbContext()) Assert.Equal(1, await Nonces(observer));
        await db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION tagekyc.a1_test_append_audit_failure() RETURNS trigger LANGUAGE plpgsql AS
            $body$ BEGIN RAISE EXCEPTION 'A1_SYNTHETIC_APPEND_AUDIT_FAILURE'; END $body$;
            CREATE TRIGGER a1_test_append_audit_failure BEFORE INSERT ON tagekyc.audit_events
            FOR EACH ROW EXECUTE FUNCTION tagekyc.a1_test_append_audit_failure();
            """);
        var probe = Create(db);
        var denied = await probe.Service.AppendCaptureArtifactAsync(authenticated.Value!, binding, request, key, default);
        Assert.False(denied.IsSuccess);
        Assert.Equal("NOT_READY", denied.Error!.Code);
        Assert.Equal(503, denied.Error.StatusCode);
        Assert.Contains("A1_SYNTHETIC_APPEND_AUDIT_FAILURE", A1SyntheticDbFailureInterceptor.Recorded);
        Assert.DoesNotContain("A1_SYNTHETIC_APPEND_AUDIT_FAILURE", JsonSerializer.Serialize(denied));
        Assert.Equal(1, probe.WriterCalls);
        Assert.Null(db.Database.CurrentTransaction);
        await using (var observer = isolated.CreateDbContext())
        {
            Assert.Equal(1, await Nonces(observer));
            Assert.Equal(0, await observer.CaptureArtifacts.CountAsync(x => x.VerificationSessionId == SessionId));
            Assert.Equal(0, await observer.AppendIdempotencyRecords.CountAsync(x => x.VerificationSessionId == SessionId));
            Assert.Equal(0, await observer.AuditEvents.CountAsync(x => x.VerificationSessionId == SessionId));
            Assert.Equal(nameof(VerificationSessionState.Created), (await observer.Sessions.SingleAsync(x => x.Id == SessionId)).State);
        }
        var replay = await authenticator.AuthenticateAsync(signed!);
        Assert.False(replay.IsSuccess);
        Assert.Equal(1, probe.WriterCalls);
    }

    private static async Task<Guid> Prepare(TagEkycDbContext db, byte[]? verifierSpki = null)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var binding = await Tip88C1C6BA1AppendAuthorityTests.SeedBindingAsync(db, verifierSpki: verifierSpki);
        // The SQL authority fixture intentionally needs only identity columns. Supply the
        // existing session-domain fields before exercising the actual evidence planner.
        var row = await db.Sessions.SingleAsync(x => x.Id == SessionId);
        row.Profile = nameof(VerificationProfile.StandardEkycProfile);
        row.State = nameof(VerificationSessionState.Created);
        row.Result = nameof(VerificationResult.NotAvailable);
        row.AssuranceLevel = nameof(AssuranceLevel.None);
        row.RequiredChecksJson = "[\"CaptureQuality\"]";
        row.PolicySnapshotId = PolicySnapshotId.LocalDevS1.Value;
        row.RetentionClass = nameof(RetentionClass.LocalDevEphemeral);
        row.DeletionEligibility = nameof(DeletionEligibility.NotEvaluated);
        row.LegalHoldStatus = nameof(LegalHoldStatus.None);
        row.PurgeBlockReason = nameof(PurgeBlockReason.None);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        db.ChangeTracker.Clear();
        return binding;
    }

    private static Probe Create(TagEkycDbContext db)
    {
        var sessions = new EfVerificationSessionRepository(db);
        var artifacts = new EfCaptureArtifactRepository(db);
        var evidence = new EfEvidenceResultRepository(db);
        var audit = new EfAuditEventRepository(db);
        var policies = new Policies();
        var store = new EfAppendIdempotencyBoundary(db);
        var core = new VerificationEvidenceApplicationService(sessions, artifacts, evidence, audit, policies, store, store);
        var probe = new Probe(db, core);
        probe.Service = new(new EfAppendBusinessTransaction(db), new CaptureRuntimeAppendAuthority(db),
            sessions, policies, artifacts, core, probe);
        return probe;
    }
    private static CaptureRuntimeCaptureArtifactRequest Capture(Guid binding) => new(binding,
        new(CaptureArtifactTypeDto.DeviceCaptureMetadata, CaptureSourceDto.PcAgent, null, "sha256:metadata", "request", "correlation"));
    private static CaptureRuntimeEvidenceResultRequest Evidence(Guid binding, string artifact) => new(binding,
        new(EvidenceResultTypeDto.CaptureQuality, [artifact], VerificationResultDto.Passed, 0.9m, [], null,
            "summary:capture-quality", "sha256:payload", SignaturePlaceholderStatusDto.PlaceholderUnverified,
            "quality", "1", "request", "correlation"));
    private sealed class Policies : ILocalDevClientPolicyProvider
    {
        public async Task<LocalDevClientPolicy?> GetPolicyAsync(Guid clientApplicationId, CancellationToken cancellationToken = default)
        {
            var template = await new LocalDevRuntimePolicySource().GetPolicyAsync(LocalDevRuntimePolicySource.BusinessClientId, cancellationToken);
            return clientApplicationId == ClientId ? template! with { ClientApplicationId = ClientId,
                AllowedPurposes = new HashSet<string> { "SyntheticProof" } } : null;
        }
    }
    private sealed class InjectedPostWriteFault : Exception;
    private sealed class Probe(TagEkycDbContext db, VerificationEvidenceApplicationService core) : IAuthorityNeutralVerificationEvidenceWriter
    {
        public CaptureRuntimeAppendApplicationService Service = null!;
        public int WriterCalls;
        public bool FailAfterWrite;
        public async Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> ApplyCaptureArtifactAsync(
            VerifiedAppendPrincipal principal, AppendCaptureArtifactWrite write, CancellationToken ct)
        {
            var transaction = db.Database.CurrentTransaction;
            Assert.NotNull(transaction); WriterCalls++;
            var result = await core.ApplyCaptureArtifactAsync(principal, write, ct);
            Assert.Same(transaction, db.Database.CurrentTransaction);
            Assert.True(result.IsSuccess, result.Error?.Code);
            if (FailAfterWrite)
            {
                Assert.Equal(1, await db.CaptureArtifacts.CountAsync(x => x.VerificationSessionId == SessionId, ct));
                Assert.Equal(1, await db.AppendIdempotencyRecords.CountAsync(x => x.VerificationSessionId == SessionId, ct));
                Assert.Equal(2, await db.AuditEvents.CountAsync(x => x.VerificationSessionId == SessionId, ct));
                throw new InjectedPostWriteFault();
            }
            return result;
        }
        public async Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> ApplyEvidenceResultAsync(
            VerifiedAppendPrincipal principal, AppendEvidenceResultWrite write, CancellationToken ct)
        {
            var transaction = db.Database.CurrentTransaction;
            Assert.NotNull(transaction); WriterCalls++;
            var result = await core.ApplyEvidenceResultAsync(principal, write, ct);
            Assert.Same(transaction, db.Database.CurrentTransaction);
            return result;
        }
    }
}
