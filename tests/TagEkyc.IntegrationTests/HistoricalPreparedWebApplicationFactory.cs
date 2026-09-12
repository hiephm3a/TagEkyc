using System.Reflection;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TagEkyc.Api;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;

namespace TagEkyc.IntegrationTests;

// Historical feature tests isolate their existing subject from the separately
// tested A1 database/startup dependencies. Never use this factory for A1 proofs.
// Production still executes the real mandatory startup validator; no product
// configuration default or readiness exemption is introduced here.
internal sealed class HistoricalPreparedWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string OnlineRef = SyntheticReference("online");
    private static readonly string OperatorRef = SyntheticReference("operator");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("TagEkyc:CaptureRuntimeDatabase:OnlineConnectionStringSecretRef", OnlineRef);
        builder.UseSetting("TagEkyc:CaptureRuntimeDatabase:OperatorConnectionStringSecretRef", OperatorRef);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICaptureRuntimeStartupDependencyReader>();
            services.RemoveAll<ICaptureRuntimeVerifierPepperSource>();
            services.AddSingleton<ICaptureRuntimeStartupDependencyReader, PreparedReader>();
            services.AddSingleton<ICaptureRuntimeVerifierPepperSource, SyntheticPeppers>();
            services.TryAddScoped<CaptureRuntimeStartup>();

            // These unfinished C6B services are registered by Program even in
            // legacy in-memory hosts. Their constructor graphs require the
            // broker/pipeline/Postgres that these unrelated tests do not own.
            services.RemoveAll<RawExportSourceIngressApplicationService>();
            services.RemoveAll<IRawExportCaptureAcceptanceResolver>();
            services.AddScoped<RawExportSourceIngressApplicationService>(_ =>
                throw new InvalidOperationException("Historical fixture cannot execute raw ingress."));
            services.AddScoped<IRawExportCaptureAcceptanceResolver>(_ =>
                throw new InvalidOperationException("Historical fixture cannot resolve raw capture acceptance."));

            // In-memory historical hosts have no A1 persistence services. Supply
            // only missing handler dependencies for route discovery; invocation
            // fails rather than pretending any A1 business operation succeeded.
            foreach (var type in typeof(CaptureRuntimeHttpRoutes)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .SelectMany(method => method.GetParameters()).Select(parameter => parameter.ParameterType)
                .Distinct().Where(type => type.Namespace?.StartsWith("TagEkyc.Application", StringComparison.Ordinal) == true))
                services.TryAdd(ServiceDescriptor.Scoped(type, _ =>
                    throw new InvalidOperationException("Historical fixture cannot execute A1 operations.")));
        });
    }

    private static string SyntheticReference(string role)
    {
        var name = $"TAGEKYC_HISTORICAL_A1_{role}_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(name,
            $"Host=127.0.0.1;Port=1;Database=historical_unused;Username=historical_{role};Password=synthetic-only;Timeout=1");
        return $"env:{name}";
    }

    private sealed class PreparedReader : ICaptureRuntimeStartupDependencyReader
    {
        public Task<CaptureRuntimeStartupDependencies> ReadAsync(DateTimeOffset now, CancellationToken ct) =>
            Task.FromResult(new CaptureRuntimeStartupDependencies("Managed", "Prepared", 1,
                now.AddMinutes(-1), null, null, []));
    }

    private sealed class SyntheticPeppers : ICaptureRuntimeVerifierPepperSource
    {
        public int CurrentVersion => 1;
        public ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(int version,
            CaptureRuntimeVerifierPepperDomain domain, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(version == 1 ? new Lease(domain) : null);
    }

    private sealed class Lease(CaptureRuntimeVerifierPepperDomain domain) : ICaptureRuntimeVerifierPepperLease
    {
        private readonly byte[] key = new byte[32];
        public int Version => 1;
        public CaptureRuntimeVerifierPepperDomain Domain => domain;
        public ReadOnlyMemory<byte> Key => key;
        public void Dispose() => CryptographicOperations.ZeroMemory(key);
    }
}
