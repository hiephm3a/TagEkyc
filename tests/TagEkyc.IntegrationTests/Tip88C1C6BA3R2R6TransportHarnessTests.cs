using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.CaptureAgent.Client;
using TagEkyc.CaptureAgent.Core;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.RawExport;
using AgentIngressMetadata = TagEkyc.CaptureAgent.Core.RawExportSourceIngressMetadata;

namespace TagEkyc.IntegrationTests;

// Transport prerequisite only. No business proof is credited to this test.
public sealed class Tip88C1C6BA3R2R6TransportHarnessTests
{
    [Fact]
    public async Task SharedLoopbackHttpsHarnessCompletesRealTlsHandshake()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        R2R6LoopbackHttpsHarness.Configure(builder);
        await using var app = builder.Build();
        app.MapGet("/a3-harness-health", () => "a3-tls-ok");
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        Assert.StartsWith("https://", address, StringComparison.Ordinal);
        using var client = R2R6LoopbackHttpsHarness.CreateClient();
        using var response = await client.GetAsync(new Uri(new Uri(address), "/a3-harness-health"))
            .WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("a3-tls-ok", await response.Content.ReadAsStringAsync());
    }

    // A second prerequisite probes the production Agent HTTP stack over the same
    // socket/TLS harness. Synthetic admission only answers the transport smoke;
    // it is not evidence for an R2–R6 business outcome or production auth.
    [Fact]
    public async Task SharedLoopbackHttpsHarnessCarriesProductionAgentClient()
    {
        using var keys = new FixtureKeys();
        var authentication = new SignatureCheckingAuthenticator(keys);
        var admission = new TransportOnlyAdmission();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        R2R6LoopbackHttpsHarness.Configure(builder);
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator>(authentication);
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        using var fixture = new TransportAgentFixture(new Uri(address), keys);
        using var client = new CaptureRuntimeHttpClient(new Uri(address), fixture.Journal,
            keys, fixture.Clock, rawTransport: R2R6LoopbackHttpsHarness.CreateHandler());

        var result = await client.SubmitRawExportSourceAsync(fixture.Metadata, fixture.Lease);

        Assert.Equal(RawExportSourceIngressCodes.AlreadyAvailable, result.OutcomeCode);
        Assert.Equal(admission.SourceId.ToString("D"), result.SourceArtifactId);
        Assert.Equal(1, authentication.Calls);
        Assert.Equal(1, admission.Calls);
    }

    private sealed class TransportOnlyAdmission : ICaptureRuntimeRawIngressAdmission
    {
        internal readonly Guid SourceId = Guid.NewGuid();
        internal int Calls;
        public ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, Stream body, CancellationToken cancellationToken)
        {
            Calls++;
            Assert.True(body.CanRead);
            return ValueTask.FromResult(new CaptureRuntimeRawIngressAdmissionResult(
                CaptureRuntimeRawIngressOutcome.AlreadyAvailable, SourceId, "Available", "Available", null));
        }
    }

    private sealed class SignatureCheckingAuthenticator(FixtureKeys keys) : ICaptureRuntimeRequestAuthenticator
    {
        internal int Calls;
        public Task<SessionOperationResult<AuthenticatedCaptureRuntimeContext>> AuthenticateAsync(
            CaptureRuntimeSignedRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            Assert.True(keys.Verify(request.ExactSignedPreimage.Span, request.Signature));
            return Task.FromResult(SessionOperationResult<AuthenticatedCaptureRuntimeContext>.Success(new(
                Guid.Parse("11111111-1111-4111-8111-111111111111"),
                Guid.Parse("22222222-2222-4222-8222-222222222222"), request.CredentialId,
                request.CredentialGeneration, keys.Thumbprint,
                Guid.Parse("33333333-3333-4333-8333-333333333333"),
                1, 1, 1, 1, request.SignedAtUtc, request.Nonce,
                SHA256.HashData(request.ExactSignedPreimage.Span))));
        }
    }

    internal sealed class TransportAgentFixture : IDisposable
    {
        internal FixtureClock Clock { get; } = new();
        internal FixtureJournal Journal { get; }
        internal RetainedRawBufferLease Lease { get; }
        internal AgentIngressMetadata Metadata { get; }

        internal TransportAgentFixture(Uri origin, FixtureKeys keys,
            Guid? sessionId = null, Guid? artifactId = null,
            Guid? ingressIdempotencyKey = null, RawExportRawClass rawClass = RawExportRawClass.ChipDg2Portrait,
            byte[]? plaintext = null, int maximumRawBytes = 1024)
        {
            Journal = new(origin, keys, Clock, sessionId);
            var bytes = plaintext ?? Enumerable.Range(1, 64).Select(value => (byte)value).ToArray();
            var configuration = new RawExportAgentConfigurationDocument(
                Journal.State.CaptureAgentId!.Value, Guid.NewGuid(), 1,
                Clock.UtcNow.AddMinutes(-1), Clock.UtcNow.AddMinutes(20), true,
                300, 100, 60, maximumRawBytes, maximumRawBytes, 4096,
                maximumRawBytes, 4096, 1024);
            var capturedAt = Clock.UtcNow.AddSeconds(-1);
            Lease = RetainedRawBufferLease.TakeOwnership(bytes, rawClass,
                new(new(maximumRawBytes, maximumRawBytes, 2, maximumRawBytes * 2)),
                configuration, Clock.UtcNow, Clock);
            Metadata = new(1, Journal.SessionId, artifactId ?? Guid.NewGuid(), 1,
                rawClass, ingressIdempotencyKey ?? Guid.NewGuid(), "image/jpeg", bytes.Length,
                CaptureRuntimeWireCodec.Digest(bytes), capturedAt,
                Lease.PlaintextRetentionStartedAtUtc, Lease.PlaintextRetentionExpiresAtUtc,
                Lease.PlaintextRetentionBudgetSeconds);
        }

        public void Dispose() => Lease.Dispose();
    }

    internal sealed class FixtureClock : TimeProvider, IMonotonicTimestampSource
    {
        internal DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
        long IMonotonicTimestampSource.GetTimestamp() => Stopwatch.GetTimestamp();
        TimeSpan IMonotonicTimestampSource.GetElapsedTime(long startTimestamp, long endTimestamp) =>
            Stopwatch.GetElapsedTime(startTimestamp, endTimestamp);
    }

    internal sealed class FixtureKeys : ICaptureRuntimeKeyStore, IDisposable
    {
        private readonly ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        internal byte[] Thumbprint => SHA256.HashData(key.ExportSubjectPublicKeyInfo());
        public CaptureRuntimePublicKey CreateReserved(Guid candidateKeyId) => Open(candidateKeyId, []);
        public CaptureRuntimePublicKey Open(Guid candidateKeyId, ReadOnlySpan<byte> expectedThumbprint)
        {
            var spki = key.ExportSubjectPublicKeyInfo();
            return new(candidateKeyId, spki, SHA256.HashData(spki));
        }
        public byte[] Sign(Guid candidateKeyId, ReadOnlySpan<byte> expectedThumbprint,
            ReadOnlySpan<byte> preimage) => key.SignData(preimage, HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        internal bool Verify(ReadOnlySpan<byte> preimage, ReadOnlySpan<byte> signature) =>
            key.VerifyData(preimage, signature, HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        public void DeleteConfirmedAbandoned(Guid candidateKeyId, ReadOnlySpan<byte> expectedThumbprint) { }
        public void DeleteQueuedAbandoned(Guid candidateKeyId) { }
        public void Dispose() => key.Dispose();
    }

    internal sealed class FixtureJournal : ICaptureRuntimeJournal
    {
        internal Guid SessionId { get; }
        internal CaptureRuntimeJournalState State { get; private set; }
        internal FixtureJournal(Uri origin, FixtureKeys keys, FixtureClock clock, Guid? sessionId)
        {
            SessionId = sessionId ?? Guid.NewGuid();
            var candidate = Guid.NewGuid();
            var publicKey = keys.CreateReserved(candidate);
            State = new(1, origin.ToString().TrimEnd('/'), "synthetic", 1, "BindPending", candidate,
                Guid.Parse("40000000-0000-4000-8000-000000000001"),
                Guid.Parse("50000000-0000-4000-8000-000000000001"),
                Guid.Parse("60000000-0000-4000-8000-000000000001"), 1,
                CaptureRuntimeWireCodec.Base64Url(publicKey.Spki),
                CaptureRuntimeWireCodec.Digest(publicKey.Spki), null, null,
                new("synthetic", SessionId, Guid.NewGuid(), Guid.NewGuid(),
                    clock.UtcNow.AddMinutes(5), clock.UtcNow.AddMinutes(5), "Bound", true, true,
                    Guid.NewGuid(), clock.UtcNow.AddMinutes(10), 1, 1, 1, 1), [], []);
        }
        public ICaptureRuntimeJournalTransaction Acquire() => new Transaction(this);
        private sealed class Transaction(FixtureJournal owner) : ICaptureRuntimeJournalTransaction
        {
            public CaptureRuntimeJournalState Read() => owner.State;
            public void Write(CaptureRuntimeJournalState state, long expectedRevision)
            {
                Assert.Equal(owner.State.Revision, expectedRevision);
                owner.State = state;
            }
            public void Dispose() { }
        }
    }
}
