using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Api;
using Microsoft.Extensions.Configuration;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C6BA1StartupTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 0, 0, 0, TimeSpan.Zero);
    [Fact]
    public void SiteQualificationFileProviderParsesExactRecordAndRejectsUnknownFields()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tagekyc-site-qualification-{Guid.NewGuid():N}.json");
        const string key = "TagEkyc:CaptureRuntime:SiteRawIngressTransportQualificationRecordPath";
        try
        {
            var record = $$"""
                {"formatVersion":1,"qualificationId":"q1","siteId":"site1","endpointOrigin":"https://127.0.0.1:8443","deploymentRevision":"d1","status":"PASS","observedAtUtc":"{{Now.AddMinutes(-1):O}}","validUntilUtc":"{{Now.AddMinutes(30):O}}","agentBodySendsWhileBOrR1Held":0,"serverApplicationBodyReadsWhileBOrR1Held":0,"rawPostCount":1,"kestrelContinueRelayedAfterCommit":true,"earlyOrIntermediaryContinueObserved":false,"applicationPrebufferObserved":false,"hiddenRetryObserved":false}
                """;
            File.WriteAllText(path, record);
            var configuration = new ConfigurationManager { [key] = path };
            var provider = new SiteRawIngressTransportQualificationFileProvider(configuration);
            Assert.NotNull(provider.Current);
            Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))),
                provider.Current!.RecordSha256);

            File.WriteAllText(path, record[..^1] + ",\"unknown\":true}");
            Assert.Null(provider.Current);
            File.WriteAllText(path, record.Replace(Now.AddMinutes(30).ToString("O"),
                Now.AddMinutes(60).ToString("O"), StringComparison.Ordinal));
            Assert.Equal(Now.AddMinutes(60), provider.Current!.ValidUntilUtc);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
    [Fact]
    public void StartupCatalogue_IsExactProjectionOfCanonicalMigrationProofManifest()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName,
            "tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs"))) root = root.Parent;
        Assert.NotNull(root);
        var source = File.ReadAllText(Path.Combine(root!.FullName,
            "tests/TagEkyc.IntegrationTests/Tip88C1C6BA1PersistenceTests.cs"));
        var type = typeof(TagEkyc.Infrastructure.CaptureRuntime.CaptureRuntimeStartupDependencyReader)
            .Assembly.GetType("TagEkyc.Infrastructure.CaptureRuntime.CaptureRuntimeStartupCatalogue", throwOnError: true)!;
        foreach (var name in new[] { "Tables", "Functions", "Grants" })
        {
            var match = System.Text.RegularExpressions.Regex.Match(source,
                @"internal static readonly string\[\] " + name + @"\s*=\s*\[(.*?)\];",
                System.Text.RegularExpressions.RegexOptions.Singleline);
            Assert.True(match.Success);
            var expected = System.Text.RegularExpressions.Regex.Matches(match.Groups[1].Value, "\"([^\"]+)\"")
                .Select(m => m.Groups[1].Value).Order(StringComparer.Ordinal).ToArray();
            var actual = Assert.IsType<string[]>(type.GetField(name,
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null))
                .Order(StringComparer.Ordinal).ToArray();
            Assert.Equal(name == "Tables" ? 27 : 47, expected.Length);
            Assert.Equal(actual.Length, actual.Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(expected, actual);
            Assert.DoesNotContain(actual, s => s.Contains("read_cutover_state(text)", StringComparison.Ordinal));
        }
    }
    [Fact]
    public async Task CurrentUnionSql_ResolvesEveryExactDomain_AndDisposesEveryLease()
    {
        var source = new Peppers(3); var reader = new Reader(Row([1, 3, 7]));
        var result = await new CaptureRuntimeStartup(reader, source).SelectAsync(Now, default);
        Assert.Equal(CaptureRuntimeRouteState.Prepared, result.State);
        Assert.Equal(1, reader.Calls); Assert.Equal(9, source.Calls.Count);
        Assert.Equal(new[] { 1, 3, 7 }, source.Calls.Select(c => c.Item1).Distinct().ToArray());
        Assert.All(source.Leases, lease => Assert.True(lease.Disposed));
    }
    [Theory]
    [InlineData("missing")]
    [InlineData("wrong-version")]
    [InlineData("wrong-domain")]
    [InlineData("wrong-length")]
    public async Task SqlRequiredVersionFailure_IsNeverIgnored(string mutation)
    {
        var source = new Peppers(1) { Mutation = mutation };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(
            new Reader(Row([7])), source).SelectAsync(Now, default));
        Assert.Contains(source.Calls, c => c.Item1 == 7);
        Assert.All(source.Leases, lease => Assert.True(lease.Disposed));
    }
    [Fact]
    public async Task ActivatedRequiresActualA3Readiness_PreparedNeverCallsA3()
    {
        var a3 = new A3();
        var evidence = new Evidence();
        var assemblyTopology = new CaptureRuntimeAssemblyTopology("Disabled");
        var qualification = new Qualification();
        var activated = Row([]) with { State = "Activated", ActivatedAtUtc = Now, ActivatedByCredentialId = Guid.NewGuid() };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(
            new Reader(activated), new Peppers(1), activationEvidence: evidence,
            assemblyTopology: assemblyTopology,
            siteTransportQualification: qualification,
            siteTransportSettings: new Settings()).SelectAsync(Now, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(
            new Reader(activated), new Peppers(1), a3, activationEvidence: evidence,
            assemblyTopology: assemblyTopology,
            siteTransportQualification: qualification,
            siteTransportSettings: new Settings()).SelectAsync(Now, default));
        a3.Ready = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(
            new Reader(activated), new Peppers(1), a3, activationEvidence: evidence,
            assemblyTopology: assemblyTopology,
            siteTransportQualification: qualification,
            siteTransportSettings: new Settings()).SelectAsync(Now, default));
        Assert.Equal(CaptureRuntimeRouteState.Activated, (await new CaptureRuntimeStartup(
            new Reader(activated), new Peppers(1), a3, new Admission(), evidence,
            assemblyTopology, qualification, new Settings()).SelectAsync(Now, default)).State);
        var count = a3.Calls;
        await new CaptureRuntimeStartup(new Reader(Row([])), new Peppers(1), a3).SelectAsync(Now, default);
        Assert.Equal(count, a3.Calls);
    }
    [Theory]
    [InlineData("DurableWorker")]
    [InlineData("FixtureProof")]
    [InlineData("Invalid")]
    public async Task ActivatedExcludedAssemblyTopologyFailsBeforePepperOrRoutes(string topology)
    {
        var source = new Peppers(1);
        var activated = Row([]) with { State = "Activated", ActivatedAtUtc = Now,
            ActivatedByCredentialId = Guid.NewGuid() };
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(
            new Reader(activated), source, new A3 { Ready = true }, new Admission(), new Evidence(),
            new CaptureRuntimeAssemblyTopology(topology)).SelectAsync(Now, default));
        Assert.Equal("CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INVALID", failure.Message);
        Assert.Empty(source.Calls);
    }
    [Theory]
    [InlineData("Disabled")]
    [InlineData("DurableWorker")]
    public async Task ActivatedUsesApprovedTopologyValueRatherThanHardCodedDisabled(string topology)
    {
        var activated = Row([]) with { State = "Activated", ActivatedAtUtc = Now,
            ActivatedByCredentialId = Guid.NewGuid() };
        var currentSeal = new Evidence().Current;
        var approvedSeal = new Evidence(currentSeal with {
            ApprovedAssemblyTopology = topology, BuildAssemblyTopology = topology });
        var selection = await new CaptureRuntimeStartup(
            new Reader(activated), new Peppers(1), new A3 { Ready = true }, new Admission(),
            approvedSeal, new CaptureRuntimeAssemblyTopology(topology), new Qualification(), new Settings())
            .SelectAsync(Now, default);
        Assert.Equal(CaptureRuntimeRouteState.Activated, selection.State);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("stale")]
    [InlineData("failed")]
    public async Task ActivatedHostStartsForSiteQualificationButRawIngressPolicyRemainsClosed(string mutation)
    {
        var activated = Row([]) with { State = "Activated", ActivatedAtUtc = Now,
            ActivatedByCredentialId = Guid.NewGuid() };
        ICaptureRuntimeSiteTransportQualificationProvider? qualification = mutation switch
        {
            "missing" => null,
            "stale" => new Qualification(new Qualification().Current! with { ValidUntilUtc = Now }),
            _ => new Qualification(new Qualification().Current! with { Status = "FAIL" })
        };
        var selection = await new CaptureRuntimeStartup(
            new Reader(activated), new Peppers(1), new A3 { Ready = true }, new Admission(),
            new Evidence(), new CaptureRuntimeAssemblyTopology("Disabled"), qualification, new Settings())
            .SelectAsync(Now, default);
        Assert.Equal(CaptureRuntimeRouteState.Activated, selection.State);

        var site = CaptureRuntimeSiteTransportQualificationPolicy.Evaluate(new Evidence().Current,
            new Settings().Current, qualification?.Current, Now);
        Assert.False(site.AllowsRawIngress);
        Assert.Equal(mutation == "stale"
            ? CaptureRuntimeSiteTransportQualificationPolicy.ExpiredCode
            : CaptureRuntimeSiteTransportQualificationPolicy.InvalidCode, site.Code);
    }
    [Theory]
    [InlineData("site")]
    [InlineData("origin")]
    [InlineData("revision")]
    public void SiteQualificationMustMatchLiveDeploymentIdentity(string mutation)
    {
        var settings = new Settings().Current;
        settings = mutation switch
        {
            "site" => settings with { SiteId = "other-site" },
            "origin" => settings with { EndpointOrigin = "https://127.0.0.1:9443" },
            _ => settings with { DeploymentRevision = "other-revision" }
        };
        var result = CaptureRuntimeSiteTransportQualificationPolicy.Evaluate(new Evidence().Current,
            settings, new Qualification().Current, Now);
        Assert.Equal(CaptureRuntimeSiteTransportQualificationState.Invalid, result.State);
        Assert.False(result.AllowsRawIngress);
    }
    [Fact]
    public void SiteQualificationWarnsBeforeExpiryAndFailsClosedAtExpiry()
    {
        var qualification = new Qualification().Current! with { ValidUntilUtc = Now.AddMinutes(4) };
        var warning = CaptureRuntimeSiteTransportQualificationPolicy.Evaluate(new Evidence().Current,
            new Settings().Current, qualification, Now);
        Assert.Equal(CaptureRuntimeSiteTransportQualificationState.Expiring, warning.State);
        Assert.True(warning.AllowsRawIngress);
        Assert.Equal(CaptureRuntimeSiteTransportQualificationPolicy.ExpiringCode, warning.Code);

        var expired = CaptureRuntimeSiteTransportQualificationPolicy.Evaluate(new Evidence().Current,
            new Settings().Current, qualification, Now.AddMinutes(4));
        Assert.Equal(CaptureRuntimeSiteTransportQualificationState.Expired, expired.State);
        Assert.False(expired.AllowsRawIngress);
    }
    [Fact]
    public async Task ZeroOpenSealWithoutSitePolicyCannotActivate()
    {
        var activated = Row([]) with { State = "Activated", ActivatedAtUtc = Now,
            ActivatedByCredentialId = Guid.NewGuid() };
        var invalid = new Evidence(new Evidence().Current with
        {
            ApprovedSiteTransportQualificationRequired = false,
            BuildSiteTransportQualificationRequired = false,
            ApprovedSiteTransportQualificationPolicyVersion = 0,
            BuildSiteTransportQualificationPolicyVersion = 0
        });
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(
            new Reader(activated), new Peppers(1), new A3 { Ready = true }, new Admission(), invalid,
            new CaptureRuntimeAssemblyTopology("Disabled"), new Qualification(), new Settings())
            .SelectAsync(Now, default));
        Assert.Equal("CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INVALID", failure.Message);
    }
    [Fact]
    public async Task ActivatedRejectsMatchingNonShippingTopology()
    {
        var activated = Row([]) with { State = "Activated", ActivatedAtUtc = Now,
            ActivatedByCredentialId = Guid.NewGuid() };
        var currentSeal = new Evidence().Current;
        var invalidSeal = new Evidence(currentSeal with {
            ApprovedAssemblyTopology = "FixtureProof", BuildAssemblyTopology = "FixtureProof" });
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(
            new Reader(activated), new Peppers(1), new A3 { Ready = true }, new Admission(),
            invalidSeal, new CaptureRuntimeAssemblyTopology("FixtureProof")).SelectAsync(Now, default));
        Assert.Equal("CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INVALID", failure.Message);
    }
    [Fact]
    public async Task ActivatedLegacyZeroOpenSealCannotBypassAssemblyScopeBinding()
    {
        var activated = Row([]) with { State = "Activated", ActivatedAtUtc = Now,
            ActivatedByCredentialId = Guid.NewGuid() };
        var currentSeal = new Evidence().Current;
        var legacyZero = new Evidence(currentSeal with { FormatVersion = 1 });
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(
            new Reader(activated), new Peppers(1), new A3 { Ready = true }, new Admission(),
            legacyZero, new CaptureRuntimeAssemblyTopology("Disabled")).SelectAsync(Now, default));
        Assert.Equal("CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INVALID", failure.Message);
    }
    [Theory]
    [InlineData("state")]
    [InlineData("revision")]
    [InlineData("activation")]
    [InlineData("null-version")]
    [InlineData("duplicate")]
    [InlineData("unsorted")]
    public async Task MalformedSentinelOrVersionSet_FailsBeforePepperResolution(string mutation)
    {
        var row = Row([]);
        row = mutation switch {
            "state" => row with { State = "Unknown" }, "revision" => row with { Revision = 0 },
            "activation" => row with { ActivatedAtUtc = Now },
            "null-version" => row with { RequiredPepperVersions = [0] },
            "duplicate" => row with { RequiredPepperVersions = [1, 1] },
            _ => row with { RequiredPepperVersions = [7, 1] }
        };
        var source = new Peppers(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(new Reader(row), source).SelectAsync(Now, default));
        Assert.Empty(source.Calls);
    }
    private static CaptureRuntimeStartupDependencies Row(int[] versions) => new("Managed", "Prepared", 1, Now.AddMinutes(-1), null, null, versions);
    private sealed class Reader(CaptureRuntimeStartupDependencies row) : ICaptureRuntimeStartupDependencyReader
    {
        public int Calls;
        public Task<CaptureRuntimeStartupDependencies> ReadAsync(DateTimeOffset now, CancellationToken ct)
        { Assert.Equal(Now, now); Calls++; return Task.FromResult(row); }
    }
    private sealed class A3 : ICaptureRuntimeA3Readiness
    {
        public bool Ready; public int Calls;
        public Task<bool> IsReadyAsync(CancellationToken ct) { Calls++; return Task.FromResult(Ready); }
    }
    private sealed class Admission : ICaptureRuntimeRawIngressAdmission
    {
        public ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, Stream body, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Startup must never admit a body.");
    }
    private sealed class Evidence : ICaptureRuntimeActivationEvidenceSealProvider
    {
        private const string A = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        private const string B = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
        private const string C = "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";
        private const string D = "DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD";
        private const string E = "EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE";
        public Evidence(CaptureRuntimeActivationEvidenceSeal? seal = null) => Current = seal ??
            new CaptureRuntimeActivationEvidenceSeal(2, 1, A, B, 0, A, B, 0, C, D,
                "Disabled", "Disabled", A, B, E, E, true, true, 1, 1);
        public CaptureRuntimeActivationEvidenceSeal Current { get; }
    }
    private sealed class Qualification : ICaptureRuntimeSiteTransportQualificationProvider
    {
        private const string E = "EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE";
        public Qualification(CaptureRuntimeSiteTransportQualification? current = null) => Current = current ??
            new CaptureRuntimeSiteTransportQualification(1, "test-qualification", "test-site",
                "https://127.0.0.1:8443", "test-deployment-1", "PASS", Now.AddMinutes(-1),
                Now.AddMinutes(30), 0, 0, 1, true, false, false, false, E);
        public CaptureRuntimeSiteTransportQualification? Current { get; }
    }
    private sealed class Settings : ICaptureRuntimeSiteTransportQualificationSettingsProvider
    {
        public CaptureRuntimeSiteTransportQualificationSettings Current { get; } = new(
            "test-site", "https://127.0.0.1:8443", "test-deployment-1", TimeSpan.FromMinutes(5));
    }
    private sealed class Peppers(int current) : ICaptureRuntimeVerifierPepperSource
    {
        public int CurrentVersion => current;
        public string? Mutation;
        public List<(int, CaptureRuntimeVerifierPepperDomain)> Calls = [];
        public List<Lease> Leases = [];
        public ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(int version, CaptureRuntimeVerifierPepperDomain domain, CancellationToken cancellationToken = default)
        {
            Calls.Add((version, domain));
            if (version == 7 && Mutation == "missing") return ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(null);
            var lease = new Lease(version == 7 && Mutation == "wrong-version" ? 99 : version,
                version == 7 && Mutation == "wrong-domain" ? (CaptureRuntimeVerifierPepperDomain)99 : domain,
                version == 7 && Mutation == "wrong-length" ? 31 : 32);
            Leases.Add(lease); return ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(lease);
        }
    }
    private sealed class Lease(int version, CaptureRuntimeVerifierPepperDomain domain, int length) : ICaptureRuntimeVerifierPepperLease
    {
        public int Version => version; public CaptureRuntimeVerifierPepperDomain Domain => domain;
        public ReadOnlyMemory<byte> Key { get; } = new byte[length]; public bool Disposed;
        public void Dispose() => Disposed = true;
    }
}
