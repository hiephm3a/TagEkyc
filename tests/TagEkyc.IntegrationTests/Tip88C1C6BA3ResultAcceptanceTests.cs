using System.Text.Json;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Api;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Contracts.InternalAudit.Manifest;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.RawExport;
using static TagEkyc.IntegrationTests.Tip88C1C6BA3ConsentRetentionTests;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA3ResultAcceptanceTests(PostgresPersistenceFixture postgres)
{
    [Fact]
    public void AcceptancePolicyConfiguration_IsClosedAndClientScoped()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"TagEkyc:RawExport:CaptureAcceptancePolicies:Entries:0:ClientApplicationId"] = Client.ToString("D"),
            [$"TagEkyc:RawExport:CaptureAcceptancePolicies:Entries:0:AcceptancePolicyId"] = "synthetic-retained-v1",
            [$"TagEkyc:RawExport:CaptureAcceptancePolicies:Entries:0:AcceptancePolicyVersion"] = "7",
        }).Build();
        var provider = new ConfiguredRawExportCaptureAcceptancePolicyProvider(configuration);

        Assert.Equal(new RawExportCaptureAcceptancePolicy(Client, "synthetic-retained-v1", 7), provider.Find(Client));
        Assert.Null(provider.Find(Guid.NewGuid()));
        Assert.Null(provider.Find(Guid.Empty));

        var malformed = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"TagEkyc:RawExport:CaptureAcceptancePolicies:Entries:0:ClientApplicationId"] = Client.ToString("D"),
            [$"TagEkyc:RawExport:CaptureAcceptancePolicies:Entries:0:AcceptancePolicyId"] = "synthetic-retained-v1",
            [$"TagEkyc:RawExport:CaptureAcceptancePolicies:Entries:0:AcceptancePolicyVersion"] = "7",
            [$"TagEkyc:RawExport:CaptureAcceptancePolicies:Entries:0:Unexpected"] = "forbidden",
        }).Build();
        Assert.Throws<InvalidOperationException>(() =>
            new ConfiguredRawExportCaptureAcceptancePolicyProvider(malformed));
    }

    [Fact]
    public async Task AcceptedEvidence_CommitsAndReplayUsesStoredPolicyVersion()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_result_acceptance");
        await Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        var evidence = await AddNfcEvidence(isolated, scope.Session);

        await using (var db = isolated.CreateDbContext())
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            var writer = new EfRawExportCaptureAcceptanceWriter(db);
            var first = await writer.BindAcceptedEvidenceAsync(scope.RuntimeBinding, scope.Session, evidence.Id,
                new(Client, "synthetic-current-policy", 9), CancellationToken.None);
            Assert.NotNull(first);
            Assert.Equal(evidence.ArtifactId, first.CaptureArtifactId);
            Assert.Equal("ChipDg2Portrait", first.RawClass);

            var replay = await writer.BindAcceptedEvidenceAsync(scope.RuntimeBinding, scope.Session, evidence.Id,
                configuredPolicy: null, CancellationToken.None);
            Assert.Equal(first, replay);
            await transaction.CommitAsync();
        }

        await using var observer = isolated.CreateDbContext();
        var persisted = await observer.Database.SqlQuery<AcceptanceProjection>($"""
            SELECT "CaptureAcceptanceId","AcceptancePolicyId","AcceptancePolicyVersion"
            FROM tagekyc.raw_export_capture_acceptance_events
            WHERE "VerificationSessionId"={scope.Session} AND "RawClass"='ChipDg2Portrait'
            """).SingleAsync();
        Assert.Equal("synthetic-current-policy", persisted.AcceptancePolicyId);
        Assert.Equal(9, persisted.AcceptancePolicyVersion);
    }

    [Fact]
    public async Task AcceptanceWriter_DoesNotAcknowledgeRolledBackTransaction()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_result_acceptance_rollback");
        await Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        var evidence = await AddNfcEvidence(isolated, scope.Session);

        await using (var db = isolated.CreateDbContext())
        await using (await db.Database.BeginTransactionAsync())
        {
            var result = await new EfRawExportCaptureAcceptanceWriter(db).BindAcceptedEvidenceAsync(
                scope.RuntimeBinding, scope.Session, evidence.Id,
                new(Client, "synthetic-current-policy", 9), CancellationToken.None);
            Assert.NotNull(result);
        }

        await using var observer = isolated.CreateDbContext();
        Assert.Equal(0, await observer.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM tagekyc.raw_export_capture_acceptance_events
            WHERE "VerificationSessionId"={scope.Session} AND "RawClass"='ChipDg2Portrait'
            """).SingleAsync());
    }

    [Fact]
    public async Task Completion_SelectsExactlyTwoAvailableSourcesInOwningTransaction()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a3_result_completion");
        await Prepare(isolated);
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await Tip88C1C6BA3RetentionCheckpointTests.MakeRetainedSourceAvailable(isolated, scope, minio);
        var evidence = await AddNfcEvidence(isolated, scope.Session);
        Guid chipAcceptance;
        await using (var acceptanceDb = isolated.CreateDbContext())
        await using (var acceptanceTx = await acceptanceDb.Database.BeginTransactionAsync())
        {
            var accepted = await new EfRawExportCaptureAcceptanceWriter(acceptanceDb).BindAcceptedEvidenceAsync(
                scope.RuntimeBinding, scope.Session, evidence.Id,
                new(Client, "synthetic-current-policy", 9), CancellationToken.None);
            chipAcceptance = Assert.IsType<Guid>(accepted?.CaptureAcceptanceId);
            await acceptanceTx.CommitAsync();
        }
        await Tip88C1C6BA3RetentionCheckpointTests.MakeRetainedSourceAvailable(isolated,
            scope with { Acceptance = chipAcceptance, Artifact = evidence.ArtifactId }, minio, "ChipDg2Portrait");

        await using (var deniedDb = isolated.CreateDbContext())
        await using (await deniedDb.Database.BeginTransactionAsync())
        {
            var denied = await Assert.ThrowsAsync<PostgresException>(() => deniedDb.Database
                .SqlQuery<SelectionProjection>($"""
                    SELECT "Required","RawClass","CaptureAcceptanceId","CaptureRevision"
                    FROM tagekyc.raw_export_select_runtime_sources_on_completion(
                        {scope.Session},{Client},{Guid.NewGuid()})
                    """).ToListAsync());
            Assert.Equal("RAW_EXPORT_COMPLETION_ACCESS_DENIED", denied.MessageText);
        }

        await using var db = isolated.CreateDbContext();
        var expected = await new EfVerificationSessionRepository(db).GetAsync(scope.Session)
            ?? throw new InvalidOperationException("Synthetic session is missing.");
        var write = FinalizationWrite(expected, Principal);
        var result = await new EfVerificationFinalizationBoundary(db).TryFinalizeAsync(write);
        Assert.Equal(VerificationFinalizationWriteStatus.Applied, result.Status);

        await using var observer = isolated.CreateDbContext();
        Assert.Equal("Completed", await observer.Sessions.Where(row => row.Id == scope.Session)
            .Select(row => row.State).SingleAsync());
        var selected = await observer.Database.SqlQuery<string>($"""
            SELECT "RawClass" AS "Value" FROM tagekyc.raw_export_session_capture_selections
            WHERE "VerificationSessionId"={scope.Session} ORDER BY "RawClass"
            """).ToArrayAsync();
        Assert.Equal(new[] { "ChipDg2Portrait", "LiveSelfieImage" }, selected);
    }

    private static async Task<(Guid Id, Guid ArtifactId)> AddNfcEvidence(
        PostgresPersistenceFixture.DisposableCurrentDatabase isolated, Guid sessionId)
    {
        await using var db = isolated.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var artifact = Guid.NewGuid();
        var evidence = Guid.NewGuid();
        db.CaptureArtifacts.Add(new CaptureArtifactRow
        {
            Id = artifact,
            VerificationSessionId = sessionId,
            ArtifactType = "NfcReadArtifact",
            CaptureSource = "Nfc",
            CaptureAgentId = "40000000000040008000000000000001",
            DeviceId = "50000000000040008000000000000001",
            ArtifactHash = "sha256:" + new string('c', 64),
            MetadataHash = "sha256:" + new string('d', 64),
            QualityState = "Accepted",
            RequestId = "synthetic-nfc-artifact",
            CorrelationId = "synthetic-nfc-artifact",
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(5),
        });
        db.EvidenceResults.Add(new EvidenceResultRow
        {
            Id = evidence,
            VerificationSessionId = sessionId,
            ResultType = "NfcValidation",
            InputCaptureArtifactIdsJson = JsonSerializer.Serialize(new[] { artifact.ToString("N") }),
            Result = "Passed",
            ReasonCodesJson = "[]",
            PayloadSignatureStatus = "Verified",
            EngineName = "synthetic-a3",
            EngineVersion = "1",
            RequestId = "synthetic-nfc-evidence",
            CorrelationId = "synthetic-nfc-evidence",
            CreatedAt = now,
        });
        await db.SaveChangesAsync();
        return (evidence, artifact);
    }

    private sealed class AcceptanceProjection
    {
        public Guid CaptureAcceptanceId { get; set; }
        public string AcceptancePolicyId { get; set; } = string.Empty;
        public int AcceptancePolicyVersion { get; set; }
    }

    private sealed class SelectionProjection
    {
        public bool Required { get; set; }
        public string? RawClass { get; set; }
        public Guid? CaptureAcceptanceId { get; set; }
        public int? CaptureRevision { get; set; }
    }

    private static VerificationFinalizationWrite FinalizationWrite(VerificationSession expected, Guid principal)
    {
        var decisionId = Guid.Parse("a3010000-0000-5000-8000-000000000001");
        var packageId = Guid.Parse("a3010000-0000-5000-8000-000000000002");
        var auditId = Guid.Parse("a3010000-0000-5000-8000-000000000003");
        var completedAt = DateTimeOffset.UtcNow;
        var packageHash = new HashRef("sha256:" + new string('a', 64));
        var manifestHash = new HashRef("sha256:" + new string('b', 64));
        var completed = expected.WithCompletion(VerificationResult.Passed, AssuranceLevel.Medium,
            decisionId, packageId, packageHash, manifestHash,
            "synthetic-completion-request", "synthetic-completion-correlation", completedAt);
        return new(expected, completed,
            new(decisionId, expected.Id, VerificationResult.Passed, AssuranceLevel.Medium, null, [],
                [RequiredCheckType.DocumentNfc], ["ALL_REQUIRED_CHECKS_PASSED"], [], completedAt),
            new(packageId, expected.Id, "test", EvidenceCanonicalization.CanonicalizationScheme,
                EvidenceCanonicalization.HashAlgorithm, manifestHash, [], [], decisionId, packageHash,
                SignaturePlaceholderStatus.PlaceholderUnverified, completedAt),
            new(packageId.ToString("N"), expected.Id.ToString("N"), "test",
                EvidenceCanonicalization.CanonicalizationScheme, EvidenceCanonicalization.HashAlgorithm,
                manifestHash.ToString(), packageHash.ToString(), [], [], decisionId.ToString("N"),
                SignaturePlaceholderStatusDto.PlaceholderUnverified, completedAt),
            new(auditId, expected.ClientApplicationId, expected.Id, "BusinessConsumer", principal.ToString("N"),
                "VERIFICATION_COMPLETED", new HashRef("sha256:" + new string('c', 64)), null,
                "synthetic-completion-request", "synthetic-completion-correlation", completedAt),
            principal);
    }
}

// This catalogue join is intentionally independent of the PostgreSQL collection fixture.
public sealed class Tip88C1C6BA3OutcomeOwnershipTests
{
    [Fact]
    public async Task A3_S02_ServerMapperShapeCrossProductFailsClosed()
    {
        var mapper = typeof(RawExportSourceIngressEndpoints).GetMethod("MapRuntimeResult",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(mapper);
        using var services = new ServiceCollection().AddOptions().AddLogging().BuildServiceProvider();
        var sourceId = Guid.Parse("a3020000-0000-5000-8000-000000000001");
        var retryAt = new DateTimeOffset(2026, 9, 16, 0, 0, 0, TimeSpan.Zero);

        async Task<(int Status, JsonElement Body)> Project(CaptureRuntimeRawIngressAdmissionResult admission)
        {
            var context = new DefaultHttpContext { RequestServices = services, TraceIdentifier = "synthetic-shape" };
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;
            var response = Assert.IsAssignableFrom<IResult>(mapper.Invoke(null, [context, admission]));
            await response.ExecuteAsync(context);
            context.Response.Body.Position = 0;
            using var json = await JsonDocument.ParseAsync(context.Response.Body);
            return (context.Response.StatusCode, json.RootElement.Clone());
        }

        static void ExactFields(JsonElement body, params string[] expected) =>
            Assert.Equal(expected.Order(StringComparer.Ordinal),
                body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));

        async Task Reject(CaptureRuntimeRawIngressAdmissionResult admission)
        {
            var (status, body) = await Project(admission);
            Assert.Equal(503, status);
            ExactFields(body, "code", "correlationId");
            Assert.Equal(CaptureRuntimeErrorCodes.NotReady, body.GetProperty("code").GetString());
        }

        foreach (var outcome in Enum.GetValues<CaptureRuntimeRawIngressOutcome>())
        {
            if (outcome is CaptureRuntimeRawIngressOutcome.Available or
                CaptureRuntimeRawIngressOutcome.AlreadyAvailable)
            {
                var valid = new CaptureRuntimeRawIngressAdmissionResult(
                    outcome, sourceId, "Available", "Available", null);
                var (status, body) = await Project(valid);
                Assert.Equal(200, status);
                ExactFields(body, "outcomeCode", "sourceArtifactId", "currentSourceState", "currentDisposition");
                Assert.Equal(sourceId, body.GetProperty("sourceArtifactId").GetGuid());
                Assert.Equal("Available", body.GetProperty("currentSourceState").GetString());
                Assert.Equal("Available", body.GetProperty("currentDisposition").GetString());
                await Reject(valid with { SourceArtifactId = null });
                await Reject(valid with { SourceArtifactId = Guid.Empty });
                await Reject(valid with { CurrentSourceState = null });
                await Reject(valid with { CurrentSourceState = "Pending" });
                await Reject(valid with { CurrentDisposition = null });
                await Reject(valid with { CurrentDisposition = "Pending" });
                await Reject(valid with { RetryNotBeforeUtc = retryAt });
            }
            else if (outcome == CaptureRuntimeRawIngressOutcome.EvaluationInProgress)
            {
                var valid = new CaptureRuntimeRawIngressAdmissionResult(outcome, null, null, null, retryAt);
                var (status, body) = await Project(valid);
                Assert.Equal(409, status);
                ExactFields(body, "outcomeCode", "retryNotBeforeUtc");
                Assert.Equal(retryAt, body.GetProperty("retryNotBeforeUtc").GetDateTimeOffset());
                await Reject(valid with { RetryNotBeforeUtc = null });
                await Reject(valid with { RetryNotBeforeUtc = retryAt.ToOffset(TimeSpan.FromHours(1)) });
                await Reject(valid with { SourceArtifactId = sourceId });
                await Reject(valid with { CurrentSourceState = "Available" });
                await Reject(valid with { CurrentDisposition = "Available" });
            }
            else
            {
                var valid = new CaptureRuntimeRawIngressAdmissionResult(outcome, null, null, null, null);
                var (status, body) = await Project(valid);
                Assert.Contains(status, new[] { 400, 403, 409, 413, 422, 503, 202 });
                ExactFields(body, "outcomeCode");
                await Reject(valid with { SourceArtifactId = sourceId });
                await Reject(valid with { CurrentSourceState = "Available" });
                await Reject(valid with { CurrentDisposition = "Available" });
                await Reject(valid with { RetryNotBeforeUtc = retryAt });
            }
        }
        await Reject(new CaptureRuntimeRawIngressAdmissionResult(
            (CaptureRuntimeRawIngressOutcome)int.MaxValue, null, null, null, null));
    }

    [Fact]
    public async Task A3_S02_All36RowsHaveExactlyOneOwner()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TagEkyc.sln")))
            root = root.Parent;
        Assert.NotNull(root);

        var parent = File.ReadAllText(Path.Combine(root.FullName, "docs", "tips",
            "tip_88c1_secure_raw_source_sealed_assembly",
            "tip_88c1_c6b_a3_broker_composition_dispatch_v0_6.md"));
        var ledger = parent.Split("### 6.2 Complete 36-row ownership/status/shape ledger", StringSplitOptions.None);
        Assert.Equal(2, ledger.Length);
        var section = ledger[1].Split("### 6.3 Coordinated implementation obligations", StringSplitOptions.None);
        Assert.Equal(2, section.Length);

        var rows = section[0].Split('\n')
            .Where(line => line.StartsWith("| O", StringComparison.Ordinal))
            .Select(line => line.Split('|').Select(cell => cell.Trim()).ToArray())
            .ToArray();
        Assert.Equal(36, rows.Length);
        Assert.All(rows, row => Assert.Equal(8, row.Length));
        Assert.All(rows, row => Assert.False(string.IsNullOrWhiteSpace(row[3])));
        Assert.Equal(Enumerable.Range(1, 36).Select(index => $"O{index:00}"), rows.Select(row => row[1]));
        Assert.Equal(36, rows.Select(row => row[2]).Distinct(StringComparer.Ordinal).Count());
        Assert.All(rows, row => Assert.Equal("P" + row[1][1..], row[6]));

        var a1 = rows.Where(row => row[3] == "A1 P0").ToArray();
        var business = rows.Where(row => row[3].StartsWith("A3 ", StringComparison.Ordinal)).ToArray();
        var excluded = rows.Where(row => row[4] == "NO EGRESS").ToArray();
        Assert.Single(a1);
        Assert.Equal("ACCESS_DENIED", a1[0][2]);
        Assert.Equal(21, business.Length);
        Assert.Equal(14, excluded.Length);
        Assert.Equal(36, a1.Length + business.Length + excluded.Length);
        Assert.All(rows, row => Assert.Equal(1,
            (row[3] == "A1 P0" ? 1 : 0) +
            (row[3].StartsWith("A3 ", StringComparison.Ordinal) ? 1 : 0) +
            (row[4] == "NO EGRESS" ? 1 : 0)));

        var outcomeNames = Enum.GetNames<CaptureRuntimeRawIngressOutcome>();
        Assert.Equal(21, outcomeNames.Length);
        var fields = typeof(RawExportSourceIngressCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
        Assert.Equal(21, fields.Length);
        var emittedCodes = outcomeNames.Select(name =>
        {
            var field = Assert.Single(fields, candidate => candidate.Name == name);
            return Assert.IsType<string>(field.GetRawConstantValue());
        }).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(21, emittedCodes.Count);
        Assert.Equal(business.Select(row => row[2]).Order(StringComparer.Ordinal),
            emittedCodes.Order(StringComparer.Ordinal));

        var mapper = typeof(RawExportSourceIngressEndpoints).GetMethod("MapRuntimeResult",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(mapper);
        var artifactId = Guid.Parse("a3020000-0000-5000-8000-000000000001");
        using var services = new ServiceCollection().AddOptions().AddLogging().BuildServiceProvider();
        foreach (var row in business)
        {
            var outcomeName = Assert.Single(fields, field =>
                string.Equals(field.GetRawConstantValue() as string, row[2], StringComparison.Ordinal)).Name;
            var outcome = Enum.Parse<CaptureRuntimeRawIngressOutcome>(outcomeName);
            var admission = outcome switch
            {
                CaptureRuntimeRawIngressOutcome.Available or CaptureRuntimeRawIngressOutcome.AlreadyAvailable =>
                    new CaptureRuntimeRawIngressAdmissionResult(outcome, artifactId, "Available", "Available", null),
                CaptureRuntimeRawIngressOutcome.EvaluationInProgress =>
                    new CaptureRuntimeRawIngressAdmissionResult(outcome, null, null, null,
                        new DateTimeOffset(2026, 9, 16, 0, 0, 0, TimeSpan.Zero)),
                _ => new CaptureRuntimeRawIngressAdmissionResult(outcome, null, null, null, null)
            };
            var context = new DefaultHttpContext();
            context.RequestServices = services;
            context.Response.Body = new MemoryStream();
            var response = Assert.IsAssignableFrom<IResult>(mapper.Invoke(null, [context, admission]));
            await response.ExecuteAsync(context);
            Assert.Equal(int.Parse(row[4].Split(' ')[0]), context.Response.StatusCode);
            context.Response.Body.Position = 0;
            using var body = await JsonDocument.ParseAsync(context.Response.Body);
            Assert.Equal(row[2], body.RootElement.GetProperty("outcomeCode").GetString());
        }
    }
}
