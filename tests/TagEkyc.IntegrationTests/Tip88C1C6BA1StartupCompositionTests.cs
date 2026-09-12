using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Infrastructure.CaptureRuntime;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1StartupCompositionTests(PostgresPersistenceFixture postgres)
{
    [Fact]
    public async Task RealReader_ThreeRestrictedLogins_PreparedPasses_ActivatedRequiresAdmissionAndReadiness()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_startup");
        await using var admin = isolated.CreateDbContext();
        var suffix = Guid.NewGuid().ToString("N");
        var names = new[] { "a1s_c_" + suffix, "a1s_n_" + suffix, "a1s_o_" + suffix };
        var password = Guid.NewGuid().ToString("N");
        var created = new List<string>();
        try
        {
            foreach (var name in names)
            {
                await admin.Database.ExecuteSqlRawAsync($"CREATE ROLE {name} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT PASSWORD '{password}'");
                created.Add(name);
            }
            await admin.Database.ExecuteSqlRawAsync($"""
                GRANT tagekyc_runtime TO {names[0]};
                GRANT tagekyc_capture_runtime_application,tagekyc_capture_runtime_authenticator TO {names[1]};
                GRANT tagekyc_capture_runtime_operator TO {names[2]};
                GRANT SELECT ON public."__EFMigrationsHistory" TO {names[0]};
                GRANT SELECT ON tagekyc.api_keys,tagekyc.append_idempotency_records TO {names[0]};
                """);
            string Connection(int index) => new NpgsqlConnectionStringBuilder(admin.Database.GetConnectionString())
                { Username = names[index], Password = password, Pooling = false }.ConnectionString;
            await using var ordinary = new TagEkycDbContext(new DbContextOptionsBuilder<TagEkycDbContext>().UseNpgsql(Connection(0)).Options);
            var options = new CaptureRuntimeResolvedDatabaseOptions(Connection(1), Connection(2));
            // Existing production readiness uses privilege-filtered information_schema.
            // These are ordinary pre-A1 tables, never an A1 authority-table grant.
            await new PostgresProductionReadinessValidator(ordinary).ValidateAsync(default);
            await ProbeAsync(ordinary, ["tagekyc_runtime"], "ordinary");
            await using (var onlineProbe = await new CaptureRuntimeDbContextFactory(options).CreateAsync())
                await ProbeAsync(onlineProbe, ["tagekyc_capture_runtime_application", "tagekyc_capture_runtime_authenticator"], "online");
            await using (var operatorProbe = await new CaptureRuntimeOperatorDbContextFactory(options).CreateAsync())
                await ProbeAsync(operatorProbe, ["tagekyc_capture_runtime_operator"], "operator");
            var reader = new CaptureRuntimeStartupDependencyReader(ordinary,
                new CaptureRuntimeDbContextFactory(options), new CaptureRuntimeOperatorDbContextFactory(options));
            var peppers = new Peppers();
            var now = DateTimeOffset.UtcNow;
            var dependencies = await reader.ReadAsync(now, default);
            Assert.Equal("Prepared", dependencies.State);
            var selected = await new CaptureRuntimeStartup(reader, peppers).SelectAsync(now, default);
            Assert.Equal(CaptureRuntimeRouteState.Prepared, selected.State);
            Assert.True(peppers.Calls > 0);
            // Test-only A4 state fixture, isolated database. No activation implementation.
            await admin.Database.ExecuteSqlRawAsync("""
                INSERT INTO tagekyc.platform_operator_credentials VALUES
                  ('feffffff-ffff-4fff-8fff-ffffffffffff','a1startupkey',decode(repeat('11',32),'hex'),1,
                   'fdffffff-ffff-4fff-8fff-ffffffffffff',ARRAY['operator.capture-runtime.manage'],
                   'Active',1,now()-interval '1 hour',now()+interval '1 day',NULL,NULL);
                UPDATE tagekyc.capture_runtime_cutover_state SET "State"='Activated',"Revision"="Revision"+1,
                  "ActivatedAtUtc"=now(),"ActivatedByCredentialId"='feffffff-ffff-4fff-8fff-ffffffffffff' WHERE "Profile"='Managed'
                """);
            now = DateTimeOffset.UtcNow;
            await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(reader, peppers).SelectAsync(now, default));
            await Assert.ThrowsAsync<InvalidOperationException>(() => new CaptureRuntimeStartup(reader, peppers, new Ready()).SelectAsync(now, default));
            selected = await new CaptureRuntimeStartup(reader, peppers, new Ready(), new Admission()).SelectAsync(now, default);
            Assert.Equal(CaptureRuntimeRouteState.Activated, selected.State);
            // Independently invalidate a startup dependency without broadening privilege.
            await admin.Database.ExecuteSqlRawAsync($"REVOKE tagekyc_capture_runtime_authenticator FROM {names[1]}");
            await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadAsync(now, default));
        }
        finally
        {
            foreach (var name in created.AsEnumerable().Reverse())
            {
                await admin.Database.ExecuteSqlRawAsync($"DROP OWNED BY {name}; DROP ROLE {name}");
            }
        }
    }
    private static async Task ProbeAsync(TagEkycDbContext db, string[] roles, string label)
    {
        await db.Database.OpenConnectionAsync();
        var type = typeof(CaptureRuntimeStartupDependencyReader);
        foreach (var method in new[] { "ValidateIdentityAsync", "ValidateCatalogueAsync" })
        {
            try
            {
                object[] args = method == "ValidateIdentityAsync"
                    ? [(NpgsqlConnection)db.Database.GetDbConnection(), roles, CancellationToken.None]
                    : [(NpgsqlConnection)db.Database.GetDbConnection(), CancellationToken.None];
                await (Task)type.GetMethod(method, System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static)!.Invoke(null, args)!;
            }
            catch (Exception error) { throw new InvalidOperationException($"R28 diagnostic: {label}/{method}", error); }
        }
    }
    private sealed class Ready : ICaptureRuntimeA3Readiness
    { public Task<bool> IsReadyAsync(CancellationToken ct) => Task.FromResult(true); }
    private sealed class Admission : ICaptureRuntimeRawIngressAdmission
    {
        public ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(CaptureRuntimeRawIngressAdmissionContext context,
            Stream body, CancellationToken cancellationToken) => throw new InvalidOperationException("Startup cannot admit body.");
    }
    private sealed class Peppers : ICaptureRuntimeVerifierPepperSource
    {
        public int Calls; public int CurrentVersion => 1;
        public ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(int version, CaptureRuntimeVerifierPepperDomain domain,
            CancellationToken cancellationToken = default)
        { Calls++; return ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(new Lease(version, domain)); }
    }
    private sealed class Lease(int version, CaptureRuntimeVerifierPepperDomain domain) : ICaptureRuntimeVerifierPepperLease
    {
        public int Version => version; public CaptureRuntimeVerifierPepperDomain Domain => domain;
        public ReadOnlyMemory<byte> Key { get; } = new byte[32]; public void Dispose() { }
    }
}
