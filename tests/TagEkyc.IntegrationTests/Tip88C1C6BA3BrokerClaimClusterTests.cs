#if A3_ACCEPTANCE
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.CaptureAgent.Client;
using TagEkyc.CaptureAgent.Core;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;
using AgentIngressMetadata = TagEkyc.CaptureAgent.Core.RawExportSourceIngressMetadata;

namespace TagEkyc.IntegrationTests;

// One shared production arrangement for the BROKER_CLAIM outcome family.
// Synthetic code supplies only an already-authenticated caller and prerequisite
// records/providers. The claim decision, SQL residue, HTTP mapping and Agent
// interpretation are all production code.
[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA3BrokerClaimClusterTests(PostgresPersistenceFixture postgres)
{
    [Theory]
    [InlineData("O09", RawExportSourceIngressCodes.IdempotencyBusy)]
    [InlineData("O10", RawExportSourceIngressCodes.EvaluationInProgress)]
    [InlineData("O11", RawExportSourceIngressCodes.ClaimTokenInvalid)]
    [InlineData("O12", RawExportSourceIngressCodes.ClaimRestartRequired)]
    [InlineData("O13", RawExportSourceIngressCodes.SourceRetentionNotAuthorized)]
    [InlineData("O14", RawExportSourceIngressCodes.HistoricCommitmentKeyUnavailable)]
    [InlineData("O15", RawExportSourceIngressCodes.FingerprintConflict)]
    [InlineData("O16", RawExportSourceIngressCodes.ReservationBusy)]
    public async Task BrokerClaimFinalsTraverseSharedKestrelAgentWithoutReadingBody(
        string row, string expectedCode)
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync(
            "a3_broker_claim_" + row.ToLowerInvariant());
        await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
        var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
        await using var observer = isolated.CreateDbContext();
        var brokerLogin = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(
            observer.Database.GetConnectionString()!);
        await using var brokerSource = NpgsqlDataSource.Create(brokerLogin);
        await using var preflight = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        using var keys = new Tip88C1C6BA3R2R6TransportHarnessTests.FixtureKeys();
        var normalCommitment = preflight.GetRequiredService<IContentCommitmentService>();
        var subject = preflight.GetRequiredService<ISubjectRefTokenService>();
        var normalBroker = Tip88C1C6BA3SyntheticComposition.Broker(
            brokerSource, normalCommitment, subject);
        IRawIngressMetadataBroker requestBroker = normalBroker;
        NpgsqlConnection? lockOwner = null;
        NpgsqlTransaction? lockTransaction = null;
        RecoverableCommitment? unavailable = null;
        DateTimeOffset? persistedRetry = null;
        string[]? concurrentWinnerRows = null;
        if (row is "O10" or "O14")
        {
            unavailable = new(normalCommitment);
            requestBroker = Tip88C1C6BA3SyntheticComposition.Broker(
                brokerSource, unavailable, subject, configuredCommitmentVersion: 2);
        }
        var serverReads = 0;
        var bodyPipeline = new RejectBodyPipeline();
        var admission = new CaptureRuntimeRawIngressAdmissionService(
            new RawExportIngressCapacity(1, 2, 1_048_576, 2_097_152),
            requestBroker, bodyPipeline, 1_048_576);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        R2R6LoopbackHttpsHarness.Configure(builder);
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator, AcceptedRuntimeAuthenticator>();
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        app.Use(async (context, next) =>
        {
            context.Request.Body = new CountingReadStream(
                context.Request.Body, () => Interlocked.Increment(ref serverReads));
            await next();
        });
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        var origin = new Uri(app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single());
        var requestBytes = row is "O11" or "O15"
            ? Enumerable.Range(1, 65).Select(value => (byte)value).ToArray()
            : "synthetic-retainedsource"u8.ToArray();
        using var request = new Tip88C1C6BA3R2R6TransportHarnessTests.TransportAgentFixture(
            origin, keys, scope.Session, scope.Artifact,
            rawClass: TagEkyc.CaptureAgent.Core.RawExportRawClass.LiveSelfieImage,
            plaintext: requestBytes);
        var requestContext = Context(request.Metadata);

        // Every state is produced by the real broker. The seed call is not the
        // assertion under test and uses a distinct alias where the business
        // outcome requires comparison against an existing canonical claim.
        if (row != "O13")
        {
            var seed = row switch
            {
                "O11" => requestContext with {
                    ClaimedPlaintextLength = requestContext.ClaimedPlaintextLength + 1 },
                "O10" or "O14" => requestContext with { IngressIdempotencyKey = Guid.NewGuid() },
                "O15" => requestContext with {
                    IngressIdempotencyKey = Guid.NewGuid(),
                    ClaimedPlaintextDigest = new string('a', 64) },
                _ => requestContext
            };
            Assert.IsType<RawIngressBrokerResult.Handoff>(
                await normalBroker.AdmitAsync(seed, CancellationToken.None));
        }

        if (row == "O09")
        {
            lockOwner = new NpgsqlConnection(observer.Database.GetConnectionString());
            await lockOwner.OpenAsync();
            lockTransaction = await lockOwner.BeginTransactionAsync();
            await using var acquire = new NpgsqlCommand(
                "SELECT pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended($1,0))",
                lockOwner, lockTransaction);
            acquire.Parameters.AddWithValue(AliasLock(requestContext));
            await acquire.ExecuteNonQueryAsync();
        }
        else if (row == "O10")
        {
            var setup = Assert.IsType<RawIngressBrokerResult.Final>(
                await requestBroker.AdmitAsync(requestContext, CancellationToken.None));
            Assert.Equal(RawExportSourceIngressCodes.HistoricCommitmentKeyUnavailable,
                setup.Value.OutcomeCode);
            persistedRetry = await observer.Database.SqlQuery<DateTimeOffset>($"""
                SELECT "CurrentTokenExpiresAtUtc" AS "Value"
                FROM tagekyc.raw_export_source_ingress_claim_aliases
                WHERE "IngressIdempotencyKey"={request.Metadata.IngressIdempotencyKey}
                """).SingleAsync();
        }
        else if (row == "O12")
        {
            lockOwner = new NpgsqlConnection(observer.Database.GetConnectionString());
            await lockOwner.OpenAsync();
            lockTransaction = await lockOwner.BeginTransactionAsync();
            await using (var role = new NpgsqlCommand(
                "SET LOCAL ROLE tagekyc_raw_export_deployer", lockOwner, lockTransaction))
                await role.ExecuteNonQueryAsync();
            await using (var context = new NpgsqlCommand("""
                SELECT pg_catalog.set_config(
                  'tagekyc.raw_export_source_ingress_write_context','alias:UPDATE',true)
                """, lockOwner, lockTransaction))
                await context.ExecuteNonQueryAsync();
            await using var race = new NpgsqlCommand("""
                UPDATE tagekyc.raw_export_source_ingress_claim_aliases
                SET "CurrentClaimEvaluationRevision"="CurrentClaimEvaluationRevision"+1,
                    "CurrentClaimEvaluationFence"="CurrentClaimEvaluationFence"+1
                WHERE "IngressIdempotencyKey"=$1
                """, lockOwner, lockTransaction);
            race.Parameters.AddWithValue(request.Metadata.IngressIdempotencyKey);
            Assert.Equal(1, await race.ExecuteNonQueryAsync());
        }
        else if (row == "O13")
        {
            await Tip88C1C6BA3ConsentRetentionTests.Grant(observer, "SubjectConsentWithdrawer");
            await using var withdrawal = await observer.Database.BeginTransactionAsync();
            await observer.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_catalog.set_config('tagekyc.actor_principal_id',{Tip88C1C6BA3ConsentRetentionTests.Principal.ToString("D")},true)");
            Assert.Equal("Withdrawn", await observer.Database.SqlQuery<string>($"""
                SELECT result_code AS "Value" FROM tagekyc.raw_source_withdraw_consent_reference(
                 {Tip88C1C6BA3ConsentRetentionTests.Principal},{Tip88C1C6BA3ConsentRetentionTests.Client},
                 {scope.Reference},1,'source-v1','synthetic-o13-withdrawal',{Guid.NewGuid()},{new byte[32]})
                """).SingleAsync());
            await withdrawal.CommitAsync();
        }
        var before = await DurableRows(observer);
        var beforeAliases = await observer.RawExportSourceIngressClaimAliases.AsNoTracking().CountAsync();
        var beforeProviderRows = await ProviderRows(observer);
        using var client = new CaptureRuntimeHttpClient(
            origin, request.Journal, keys, request.Clock,
            rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
        try
        {
            var pending = client.SubmitRawExportSourceAsync(request.Metadata, request.Lease);
            if (row == "O12")
            {
                await WaitForBlockedBroker(observer);
                await lockTransaction!.CommitAsync();
                await lockTransaction.DisposeAsync();
                lockTransaction = null;
                concurrentWinnerRows = await DurableRows(observer);
            }
            var result = await pending;
            Assert.Equal(expectedCode, result.OutcomeCode);
            Assert.Null(result.SourceArtifactId);
            Assert.Null(result.CurrentSourceState);
            Assert.Null(result.CurrentDisposition);
            Assert.Equal(row == "O10", result.RetryNotBeforeUtc is not null);
            if (row == "O10") Assert.Equal(persistedRetry, result.RetryNotBeforeUtc);
            Assert.Equal(0, Volatile.Read(ref serverReads));
            Assert.Equal(0, bodyPipeline.Calls);
            Assert.Equal(beforeProviderRows, await ProviderRows(observer));

            var after = await DurableRows(observer);
            if (row is "O14" or "O15")
            {
                Assert.Equal(beforeAliases + 1,
                    await observer.RawExportSourceIngressClaimAliases.AsNoTracking().CountAsync());
                Assert.Equal(before.Where(value => !value.StartsWith("alias:", StringComparison.Ordinal)),
                    after.Where(value => !value.StartsWith("alias:", StringComparison.Ordinal)));
                var alias = await observer.RawExportSourceIngressClaimAliases.AsNoTracking()
                    .Where(value => value.IngressIdempotencyKey == request.Metadata.IngressIdempotencyKey)
                    .SingleAsync();
                Assert.Equal(row == "O15" ? "ConflictTombstone" : "Evaluating", alias.AliasState);
            }
            else if (row == "O16")
            {
                Assert.Equal(beforeAliases,
                    await observer.RawExportSourceIngressClaimAliases.AsNoTracking().CountAsync());
                Assert.Equal(before.Where(value => !value.StartsWith("alias:", StringComparison.Ordinal)),
                    after.Where(value => !value.StartsWith("alias:", StringComparison.Ordinal)));
            }
            else if (row == "O12")
            {
                Assert.NotNull(concurrentWinnerRows);
                Assert.Equal(concurrentWinnerRows, after);
            }
            else
            {
                Assert.Equal(before, after);
            }
            if (row == "O14")
            {
                Assert.Equal([new CommitmentKeySelector("fixture-content-commitment", 1)],
                    unavailable!.Selectors);
                // Recovery must retry the selector locked into the existing
                // claim. The broker's current configuration points at v2;
                // making the provider available must therefore use v1 again,
                // never fall forward to the latest selector or read a second
                // source body.
                await using (var expiry = await observer.Database.BeginTransactionAsync())
                {
                    await observer.Database.ExecuteSqlRawAsync(
                        "SET LOCAL ROLE tagekyc_raw_export_deployer");
                    await observer.Database.ExecuteSqlRawAsync("""
                        SELECT pg_catalog.set_config(
                          'tagekyc.raw_export_source_ingress_write_context','alias:UPDATE',true)
                        """);
                    Assert.Equal(1, await observer.Database.ExecuteSqlInterpolatedAsync($"""
                        UPDATE tagekyc.raw_export_source_ingress_claim_aliases
                        SET "CurrentTokenIssuedAtUtc"=clock_timestamp()-interval '11 seconds',
                            "CurrentTokenExpiresAtUtc"=clock_timestamp()-interval '1 second'
                        WHERE "IngressIdempotencyKey"={request.Metadata.IngressIdempotencyKey}
                        """));
                    await expiry.CommitAsync();
                }
                unavailable.Available = true;
                var recovered = Assert.IsType<RawIngressBrokerResult.Final>(
                    await requestBroker.AdmitAsync(requestContext, CancellationToken.None));
                Assert.Equal(RawExportSourceIngressCodes.ReservationBusy,
                    recovered.Value.OutcomeCode);
                Assert.Equal(
                    [new CommitmentKeySelector("fixture-content-commitment", 1),
                     new CommitmentKeySelector("fixture-content-commitment", 1)],
                    unavailable.Selectors);
                Assert.DoesNotContain(new CommitmentKeySelector("fixture-content-commitment", 2),
                    unavailable.Selectors);
                Assert.Equal(0, Volatile.Read(ref serverReads));
                Assert.Equal(0, bodyPipeline.Calls);
            }
        }
        finally
        {
            if (lockTransaction is not null) await lockTransaction.RollbackAsync();
            if (lockOwner is not null) await lockOwner.DisposeAsync();
        }
    }

    private static async Task WaitForBlockedBroker(TagEkycDbContext observer)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var blocked = await observer.Database.SqlQueryRaw<int>("""
                SELECT count(*)::integer AS "Value"
                FROM pg_catalog.pg_stat_activity
                WHERE datname=pg_catalog.current_database()
                  AND wait_event_type='Lock'
                  AND query LIKE '%raw_export_begin_retained_source_ingress_with_authority%'
                """).SingleAsync();
            if (blocked > 0) return;
            await Task.Delay(25);
        }
        throw new TimeoutException("O12_BROKER_CAS_BARRIER_NOT_REACHED");
    }

    [Fact]
    public async Task BrokerClaimProcessKillMatrixUsesOneChildHarnessAndDurableReplay()
    {
        await using var probe = await BrokerClaimProbe.Compile();
        foreach (var cut in new[] { "B0", "B1", "B3", "B4" })
        {
            await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync(
                "a3_broker_claim_kill_" + cut.ToLowerInvariant());
            await Tip88C1C6BA3ConsentRetentionTests.Prepare(isolated);
            var scope = await Tip88C1C6BA3RetentionCheckpointTests.Seed(isolated, rawIngress: true);
            await using var observer = isolated.CreateDbContext();
            var brokerLogin = await Tip88C1C6BA3SyntheticComposition.BrokerLogin(
                observer.Database.GetConnectionString()!);
            await using var brokerSource = NpgsqlDataSource.Create(brokerLogin);
            await using var preflight = Tip88C1C6BA3SyntheticComposition.PreflightServices();
            using var keys = new Tip88C1C6BA3R2R6TransportHarnessTests.FixtureKeys();
            var subject = preflight.GetRequiredService<ISubjectRefTokenService>();
            var unavailable = new RecoverableCommitment();
            var finalBroker = Tip88C1C6BA3SyntheticComposition.Broker(
                brokerSource, unavailable, subject, configuredCommitmentVersion: 2);
            var serverReads = 0;
            var bodyPipeline = new RejectBodyPipeline();
            var observedBroker = new ObservingBroker(finalBroker);
            var admission = new CaptureRuntimeRawIngressAdmissionService(
                new RawExportIngressCapacity(1, 2, 1_048_576, 2_097_152),
                observedBroker, bodyPipeline, 1_048_576);
            var builder = WebApplication.CreateBuilder(
                new WebApplicationOptions { EnvironmentName = "Testing" });
            R2R6LoopbackHttpsHarness.Configure(builder);
            builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator,
                AcceptedRuntimeAuthenticator>();
            builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
            await using var app = builder.Build();
            app.Use(async (http, next) =>
            {
                http.Request.Body = new CountingReadStream(http.Request.Body,
                    () => Interlocked.Increment(ref serverReads));
                await next();
            });
            app.MapCaptureRuntimeRawIngressEndpoints();
            await app.StartAsync();
            var origin = new Uri(app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single());
            using var request = new Tip88C1C6BA3R2R6TransportHarnessTests.TransportAgentFixture(
                origin, keys, scope.Session, scope.Artifact,
                rawClass: TagEkyc.CaptureAgent.Core.RawExportRawClass.LiveSelfieImage,
                plaintext: "broker-claim-process-kill"u8.ToArray());
            var context = Context(request.Metadata);
            var normalBroker = Tip88C1C6BA3SyntheticComposition.Broker(
                brokerSource, preflight.GetRequiredService<IContentCommitmentService>(), subject);
            Assert.IsType<RawIngressBrokerResult.Handoff>(await normalBroker.AdmitAsync(
                context with { IngressIdempotencyKey = Guid.NewGuid() }, CancellationToken.None));

            if (cut == "B4")
            {
                var seeded = Assert.IsType<RawIngressBrokerResult.Final>(
                    await finalBroker.AdmitAsync(context, CancellationToken.None));
                Assert.Equal(RawExportSourceIngressCodes.HistoricCommitmentKeyUnavailable,
                    seeded.Value.OutcomeCode);
            }

            var before = await DurableRows(observer);
            var beforeProviders = await ProviderRows(observer);
            using var child = probe.Start(new BrokerClaimChildInput(brokerLogin, context, cut));
            var childError = child.StandardError.ReadToEndAsync();
            try
            {
                var barrier = await child.StandardOutput.ReadLineAsync()
                    .WaitAsync(TimeSpan.FromSeconds(40));
                Assert.Equal($"BC-CUT:{child.Id}:{cut}", barrier);
                Assert.NotEqual(Environment.ProcessId, child.Id);
                Assert.False(child.HasExited);

                var atCut = await DurableRows(observer);
                if (cut is "B0" or "B1" or "B4") Assert.Equal(before, atCut);
                else Assert.NotEqual(before, atCut);
                Assert.Equal(beforeProviders, await ProviderRows(observer));

                child.Kill(entireProcessTree: true);
                await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.True(child.HasExited);
                Assert.NotEqual(0, child.ExitCode);
                Assert.Equal(atCut, await DurableRows(observer));
                Assert.Equal(beforeProviders, await ProviderRows(observer));

                using var replay = new CaptureRuntimeHttpClient(
                    origin, request.Journal, keys, request.Clock,
                    rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());
                CaptureAgentRawExportIngressResult result;
                try
                {
                    result = await replay.SubmitRawExportSourceAsync(request.Metadata, request.Lease);
                }
                catch (Exception error)
                {
                    throw new InvalidOperationException(
                        $"BC_REPLAY_FAILED cut={cut} broker={observedBroker.Observation}", error);
                }
                Assert.Equal(cut is "B0" or "B1"
                        ? RawExportSourceIngressCodes.HistoricCommitmentKeyUnavailable
                        : RawExportSourceIngressCodes.EvaluationInProgress,
                    result.OutcomeCode);
                Assert.Null(result.SourceArtifactId);
                Assert.Null(result.CurrentSourceState);
                Assert.Null(result.CurrentDisposition);
                Assert.Equal(cut is "B3" or "B4", result.RetryNotBeforeUtc is not null);
                Assert.Equal(0, Volatile.Read(ref serverReads));
                Assert.Equal(0, bodyPipeline.Calls);
                Assert.Equal(beforeProviders, await ProviderRows(observer));

                var afterReplay = await DurableRows(observer);
                if (cut is "B3" or "B4") Assert.Equal(atCut, afterReplay);
                else
                {
                    Assert.Equal(atCut.Where(value => !value.StartsWith("alias:", StringComparison.Ordinal)),
                        afterReplay.Where(value => !value.StartsWith("alias:", StringComparison.Ordinal)));
                    Assert.Equal(
                        atCut.Count(value => value.StartsWith("alias:", StringComparison.Ordinal)) + 1,
                        afterReplay.Count(value => value.StartsWith("alias:", StringComparison.Ordinal)));
                }
            }
            finally
            {
                if (!child.HasExited)
                {
                    child.Kill(entireProcessTree: true);
                    await child.WaitForExitAsync();
                }
            }
            Assert.Equal("", await childError);
        }
    }

    public sealed record BrokerClaimChildInput(
        string BrokerConnection, CaptureRuntimeRawIngressAdmissionContext Context, string Cut);

    internal static async Task RunBrokerClaimChildAsync()
    {
        var input = JsonSerializer.Deserialize<BrokerClaimChildInput>(
            Environment.GetEnvironmentVariable("TAGEKYC_A3_BROKER_CLAIM_PROBE")!)!;
        if (input.Cut == "B0")
        {
            WriteCut(input.Cut);
            await Task.Delay(Timeout.InfiniteTimeSpan);
            throw new InvalidOperationException("CHILD_MUST_BE_KILLED");
        }

        await using var source = NpgsqlDataSource.Create(input.BrokerConnection);
        await using var services = Tip88C1C6BA3SyntheticComposition.PreflightServices();
        var subject = services.GetRequiredService<ISubjectRefTokenService>();
        var normal = services.GetRequiredService<IContentCommitmentService>();
        IContentCommitmentService commitment = input.Cut == "B1"
            ? new ChildBarrierCommitment(normal, input.Cut)
            : new RecoverableCommitment();
        var broker = Tip88C1C6BA3SyntheticComposition.Broker(
            source, commitment, subject, configuredCommitmentVersion: input.Cut == "B1" ? 1 : 2);
        var result = await broker.AdmitAsync(input.Context, CancellationToken.None);
        if (input.Cut is "B3" or "B4")
        {
            var final = result as RawIngressBrokerResult.Final
                ?? throw new InvalidOperationException("CHILD_EXPECTED_FINAL");
            var expected = input.Cut == "B3"
                ? RawExportSourceIngressCodes.HistoricCommitmentKeyUnavailable
                : RawExportSourceIngressCodes.EvaluationInProgress;
            if (!string.Equals(final.Value.OutcomeCode, expected, StringComparison.Ordinal))
                throw new InvalidOperationException("CHILD_UNEXPECTED_FINAL:" + final.Value.OutcomeCode);
            WriteCut(input.Cut);
            await Task.Delay(Timeout.InfiniteTimeSpan);
            throw new InvalidOperationException("CHILD_MUST_BE_KILLED");
        }
        throw new InvalidOperationException("CHILD_B1_BARRIER_NOT_REACHED");

        static void WriteCut(string cut)
        {
            Console.WriteLine($"BC-CUT:{Environment.ProcessId}:{cut}");
            Console.Out.Flush();
        }
    }

    private sealed class ChildBarrierCommitment(
        IContentCommitmentService inner, string cut) : IContentCommitmentService
    {
        public async ValueTask<ContentCommitmentResult> ComputeAsync(
            CommitmentKeySelector selector, ReadOnlyMemory<byte> lpPayload,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"BC-CUT:{Environment.ProcessId}:{cut}");
            Console.Out.Flush();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return await inner.ComputeAsync(selector, lpPayload, cancellationToken);
        }
    }

    private sealed class ObservingBroker(IRawIngressMetadataBroker inner) : IRawIngressMetadataBroker
    {
        internal string Observation { get; private set; } = "not-called";

        public async Task<RawIngressBrokerResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, CancellationToken cancellationToken)
        {
            try
            {
                var result = await inner.AdmitAsync(context, cancellationToken);
                Observation = result switch
                {
                    RawIngressBrokerResult.Final final => "final:" + final.Value.OutcomeCode,
                    RawIngressBrokerResult.Handoff => "handoff",
                    _ => result.GetType().Name
                };
                return result;
            }
            catch (Exception error)
            {
                Observation = "exception:" + error.GetType().Name + ":" + error.Message;
                throw;
            }
        }
    }

    private sealed class BrokerClaimProbe(
        string directory, string dotnet, string assembly) : IAsyncDisposable
    {
        internal static async Task<BrokerClaimProbe> Compile()
        {
            var runtime = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            var dotnet = Path.GetFullPath(Path.Combine(runtime, "..", "..", "..", "dotnet.exe"));
            Assert.True(File.Exists(dotnet));
            var directory = Directory.CreateTempSubdirectory("tagekyc-a3-broker-claim-").FullName;
            var owner = new BrokerClaimProbe(directory, dotnet,
                typeof(Tip88C1C6BA3BrokerClaimClusterTests).Assembly.Location);
            try
            {
                var sdkOutput = await owner.Command(["--list-sdks"]);
                var sdk = sdkOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => value.Trim()).Last(value => value.Contains('[') && value.EndsWith(']'));
                var compiler = Path.Combine(sdk[(sdk.IndexOf('[') + 1)..^1],
                    sdk[..sdk.IndexOf(' ')], "Roslyn", "bincore", "csc.dll");
                const string code = """
                    using System;
                    using System.IO;
                    using System.Reflection;
                    using System.Runtime.Loader;
                    using System.Threading.Tasks;
                    public static class Probe {
                      public static async Task<int> Main(string[] args) {
                        try {
                          var dir = Path.GetDirectoryName(args[0]);
                          AssemblyLoadContext.Default.Resolving += (_, name) => {
                            var path = Path.Combine(dir, name.Name + ".dll");
                            return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
                          };
                          var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(args[0]);
                          var type = assembly.GetType("TagEkyc.IntegrationTests.Tip88C1C6BA3BrokerClaimClusterTests", true);
                          await (Task)type.GetMethod("RunBrokerClaimChildAsync", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                          return 0;
                        } catch (Exception e) { Console.Error.WriteLine(e); return 91; }
                      }
                    }
                    """;
                var source = Path.Combine(directory, "Probe.cs");
                await File.WriteAllTextAsync(source, code, new System.Text.UTF8Encoding(false));
                var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                    .Split(Path.PathSeparator).Where(value => string.Equals(
                        Path.GetDirectoryName(value)?.TrimEnd(Path.DirectorySeparatorChar),
                        runtime.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase));
                var response = Path.Combine(directory, "compile.rsp");
                await File.WriteAllLinesAsync(response,
                    new[] { "/nologo", "/target:exe", "/langversion:12",
                        "/out:\"" + Path.Combine(directory, "Probe.dll") + "\"" }
                    .Concat(references.Select(value => "/reference:\"" + value + "\""))
                    .Append("\"" + source + "\""), new System.Text.UTF8Encoding(false));
                await owner.Command([compiler, "@" + response]);
                return owner;
            }
            catch
            {
                await owner.DisposeAsync();
                throw;
            }
        }

        internal System.Diagnostics.Process Start(BrokerClaimChildInput input)
        {
            var path = Path.GetDirectoryName(assembly)!;
            var start = Info(["exec", "--runtimeconfig",
                Path.Combine(path, "TagEkyc.IntegrationTests.runtimeconfig.json"),
                "--depsfile", Path.Combine(path, "TagEkyc.IntegrationTests.deps.json"),
                Path.Combine(directory, "Probe.dll"), assembly]);
            start.Environment["TAGEKYC_A3_BROKER_CLAIM_PROBE"] = JsonSerializer.Serialize(input);
            return System.Diagnostics.Process.Start(start)!;
        }

        private System.Diagnostics.ProcessStartInfo Info(string[] arguments)
        {
            var start = new System.Diagnostics.ProcessStartInfo(dotnet)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in arguments) start.ArgumentList.Add(argument);
            return start;
        }

        private async Task<string> Command(string[] arguments)
        {
            using var child = System.Diagnostics.Process.Start(Info(arguments))!;
            var stdout = child.StandardOutput.ReadToEndAsync();
            var stderr = child.StandardError.ReadToEndAsync();
            try { await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30)); }
            finally
            {
                if (!child.HasExited)
                {
                    child.Kill(entireProcessTree: true);
                    await child.WaitForExitAsync();
                }
            }
            Assert.True(child.ExitCode == 0, (await stdout) + (await stderr));
            return await stdout;
        }

        public ValueTask DisposeAsync()
        {
            Directory.Delete(directory, recursive: true);
            return ValueTask.CompletedTask;
        }
    }

    private static CaptureRuntimeRawIngressAdmissionContext Context(AgentIngressMetadata value) =>
        new(Guid.Parse("40000000-0000-4000-8000-000000000001"),
            Guid.Parse("50000000-0000-4000-8000-000000000001"),
            Guid.Parse("60000000-0000-4000-8000-000000000001"), 1,
            Guid.Parse("10000000-0000-4000-8000-000000000001"), 1,
            DateTimeOffset.UtcNow, new byte[32], new byte[32],
            value.AgentConfigurationRevision, value.VerificationSessionId, value.CaptureArtifactId,
            value.CaptureRevision, value.RawClass.ToString(), value.IngressIdempotencyKey,
            value.MediaType, value.ClaimedPlaintextLength, value.ClaimedPlaintextDigest,
            value.CapturedAtUtc, value.PlaintextRetentionStartedAtUtc,
            value.PlaintextRetentionExpiresAtUtc, value.PlaintextRetentionBudgetSeconds);

    private static string AliasLock(CaptureRuntimeRawIngressAdmissionContext value) =>
        "tip88c1b1:alias:" + Tip88C1C6BA3ConsentRetentionTests.Client.ToString("D") + ":" +
        value.CaptureAgentId.ToString("N") + ":" + value.DeviceInstallationId.ToString("N") + ":" +
        value.IngressIdempotencyKey.ToString("D");

    private static async Task<string[]> DurableRows(TagEkycDbContext observer) =>
        await observer.Database.SqlQueryRaw<string>("""
            SELECT family||':'||value AS "Value" FROM (
              SELECT 'snapshot' family,to_jsonb(r)::text value FROM tagekyc.raw_export_authority_snapshots r
              UNION ALL SELECT 'alias',to_jsonb(r)::text FROM tagekyc.raw_export_source_ingress_claim_aliases r
              UNION ALL SELECT 'claim',to_jsonb(r)::text FROM tagekyc.raw_export_source_ingress_claims r
              UNION ALL SELECT 'reservation',to_jsonb(r)::text FROM tagekyc.raw_export_source_reservations r
              UNION ALL SELECT 'head',to_jsonb(r)::text FROM tagekyc.raw_export_source_head r
              UNION ALL SELECT 'attempt',to_jsonb(r)::text FROM tagekyc.raw_export_source_encryption_attempts r
              UNION ALL SELECT 'provider-key',to_jsonb(r)::text FROM tagekyc.raw_export_attempt_key_reservations r
              UNION ALL SELECT 'provider-object',to_jsonb(r)::text FROM tagekyc.raw_export_provisional_objects r
            ) rows ORDER BY family,value
            """).ToArrayAsync();

    private static async Task<string[]> ProviderRows(TagEkycDbContext observer) =>
        await observer.Database.SqlQueryRaw<string>("""
            SELECT family||':'||value AS "Value" FROM (
              SELECT 'provider-key' family,to_jsonb(r)::text value FROM tagekyc.raw_export_attempt_key_reservations r
              UNION ALL SELECT 'provider-object',to_jsonb(r)::text FROM tagekyc.raw_export_provisional_objects r
            ) rows ORDER BY family,value
            """).ToArrayAsync();

    private sealed class AcceptedRuntimeAuthenticator : ICaptureRuntimeRequestAuthenticator
    {
        public Task<SessionOperationResult<AuthenticatedCaptureRuntimeContext>> AuthenticateAsync(
            CaptureRuntimeSignedRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(SessionOperationResult<AuthenticatedCaptureRuntimeContext>.Success(new(
                Guid.Parse("40000000-0000-4000-8000-000000000001"),
                Guid.Parse("50000000-0000-4000-8000-000000000001"),
                Guid.Parse("60000000-0000-4000-8000-000000000001"), 1, new byte[32],
                Guid.Parse("10000000-0000-4000-8000-000000000001"), 1, 1, 1, 1,
                request.SignedAtUtc, request.Nonce,
                SHA256.HashData(request.ExactSignedPreimage.Span))));
    }

    private sealed class RejectBodyPipeline : ICaptureRuntimeRawIngressBodyPipeline
    {
        internal int Calls;
        public Task<CaptureRuntimeRawIngressAdmissionResult> ProcessAsync(
            CaptureRuntimeRawIngressAdmissionContext context, RawIngressBrokerHandoff handoff,
            Stream body, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("BROKER_CLAIM_FINAL_MUST_NOT_START_BODY_PIPELINE");
        }
    }

    private sealed class RecoverableCommitment(
        IContentCommitmentService? availableProvider = null) : IContentCommitmentService
    {
        internal List<CommitmentKeySelector> Selectors { get; } = [];
        internal bool Available;
        public ValueTask<ContentCommitmentResult> ComputeAsync(CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload, CancellationToken cancellationToken)
        {
            Selectors.Add(selector);
            if (!Available)
                return ValueTask.FromResult(
                    ContentCommitmentResult.Failed(ContentCommitmentFailure.ProviderFailure));
            return availableProvider?.ComputeAsync(selector, lpPayload, cancellationToken)
                ?? throw new InvalidOperationException("RECOVERABLE_COMMITMENT_PROVIDER_NOT_CONFIGURED");
        }
    }


    private sealed class CountingReadStream(Stream inner, Action read) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count)
        {
            read();
            return inner.Read(buffer, offset, count);
        }
        public override int Read(Span<byte> buffer)
        {
            read();
            return inner.Read(buffer);
        }
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            read();
            return inner.ReadAsync(buffer, cancellationToken);
        }
        public override Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            read();
            return inner.ReadAsync(buffer, offset, count, cancellationToken);
        }
        public override int ReadByte()
        {
            read();
            return inner.ReadByte();
        }
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
#endif
