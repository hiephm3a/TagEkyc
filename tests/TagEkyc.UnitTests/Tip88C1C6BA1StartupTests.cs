using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C6BA1StartupTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 0, 0, 0, TimeSpan.Zero);
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
        var activated = Row([]) with { State = "Activated", ActivatedAtUtc = Now, ActivatedByCredentialId = Guid.NewGuid() };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(new Reader(activated), new Peppers(1)).SelectAsync(Now, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(new Reader(activated), new Peppers(1), a3).SelectAsync(Now, default));
        a3.Ready = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(new Reader(activated), new Peppers(1), a3).SelectAsync(Now, default));
        Assert.Equal(CaptureRuntimeRouteState.Activated, (await new CaptureRuntimeStartup(new Reader(activated), new Peppers(1), a3, new Admission()).SelectAsync(Now, default)).State);
        var count = a3.Calls;
        await new CaptureRuntimeStartup(new Reader(Row([])), new Peppers(1), a3).SelectAsync(Now, default);
        Assert.Equal(count, a3.Calls);
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
