using System.Reflection;
using System.Text.RegularExpressions;
using TagEkyc.Contracts.RawExport;
using Xunit.Sdk;

namespace TagEkyc.ContractTests;

public sealed class Tip88C1C5RecipientManagementContractTests : IDisposable
{
    private readonly C5ObservationCollector observations = new();

    public void Dispose() => observations.ThrowIfAny();

    [Fact]
    public void C501_exact_contract_surface_is_seven_requests_and_four_responses()
    {
        var requests = new[] { typeof(EnrollManagedRecipientRequest), typeof(IssueManagedRecipientCredentialRequest),
            typeof(ReplaceManagedRecipientCredentialRequest), typeof(RevokeManagedRecipientCredentialRequest),
            typeof(EnrollRecipientPublicKeyRequest), typeof(RotateRecipientPublicKeyRequest),
            typeof(RevokeRecipientPublicKeyRequest) };
        Bite(requests.Distinct().Count() == 7, "C501-EXACT-HTTP-SURFACE", requests.Length);
        Bite(typeof(ManagedRecipientReadinessDto).GetProperties().Length == 6,
            "C501-EXACT-HTTP-SURFACE", typeof(ManagedRecipientReadinessDto).GetProperties().Length);
        var endpoints = Source("src/TagEkyc.Api/RecipientManagementEndpoints.cs");
        Bite(Regex.Matches(endpoints, "endpoints.MapPost\\(").Count == 7
            && Regex.Matches(endpoints, "endpoints.MapGet\\(").Count == 1
            && endpoints.Contains("PropertyNameCaseInsensitive = false", StringComparison.Ordinal)
            && endpoints.Contains("UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow", StringComparison.Ordinal),
            "C501-EXACT-HTTP-SURFACE", "routes/options");
        var repository = Source("src/TagEkyc.Infrastructure/RawExport/RecipientManagementRepository.cs");
        Bite(repository.Contains("Managed recipient target was not found.", StringComparison.Ordinal),
            "C501-MANAGEMENT-NONDISCLOSURE", "one target-not-found envelope");
    }

    [Fact]
    public void C503_identity_contract_distinguishes_client_and_principal()
    {
        var names = typeof(EnrollManagedRecipientRequest).GetProperties().Select(p => p.Name).ToArray();
        Bite(names.SequenceEqual(["RecipientClientApplicationId", "PrincipalId"]),
            "C503-PERSISTED-PRINCIPAL-NOT-DERIVED", string.Join(',', names));
        var application = Source("src/TagEkyc.Application/RawExport/RecipientManagementApplicationService.cs");
        Bite(application.Contains("request.PrincipalId == Guid.Empty", StringComparison.Ordinal),
            "C503-NONEMPTY-PRINCIPAL-BEFORE-REPOSITORY", "Guid.Empty guard");
        var migration = Source("src/TagEkyc.Infrastructure/Persistence/Migrations/20260823120000_Tip88C1C5ManagedRecipientEnrollment.cs");
        Bite(migration.Contains("\\\"PrincipalId\\\" <> '00000000-0000-0000-0000-000000000000'::uuid", StringComparison.Ordinal),
            "C503-SQL-NONEMPTY-PRINCIPAL-INDEPENDENT", "durable nonempty defense");
        Bite(migration.Contains("\\\"PrincipalId\\\" <> \\\"RecipientClientApplicationId\\\"", StringComparison.Ordinal),
            "C503-IDENTITY-AUTHORITY-REVISION-STATE", "identity shape");
        Bite(Source("src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientKeyRegistrationConfig.cs")
                .Contains("\\\"Revision\\\" > 0", StringComparison.Ordinal),
            "C503-KEY-ROW-STATE-SHAPE", "key state shape");
        var managedStore = Source("src/TagEkyc.Infrastructure/Auth/PostgresHashedApiKeyStore.cs");
        Bite(Regex.IsMatch(managedStore,
                @"row\.AllowedCaptureAgentIdsJson\),\s*row\.PrincipalId\);",
                RegexOptions.CultureInvariant),
            "C503-PERSISTED-PRINCIPAL-NOT-DERIVED", "exact persisted principal projection");
        Bite(Source("src/TagEkyc.Application/AuthenticatedClientContext.cs")
                .Contains("Guid PrincipalId", StringComparison.Ordinal),
            "C503-ACTOR-CURSOR-RECEIPT-PRESERVATION", "principal actor field");
        Bite(Source("src/TagEkyc.Application/RawExport/RecipientPackageReferenceApplicationService.cs")
                .Contains("ClientApplicationId", StringComparison.Ordinal),
            "C503-C2-C4-OWNER-PRESERVATION", "client ownership unchanged");
    }

    private static Type[] RequestTypes() =>
    [
        typeof(EnrollManagedRecipientRequest), typeof(IssueManagedRecipientCredentialRequest),
        typeof(ReplaceManagedRecipientCredentialRequest), typeof(RevokeManagedRecipientCredentialRequest),
        typeof(EnrollRecipientPublicKeyRequest), typeof(RotateRecipientPublicKeyRequest),
        typeof(RevokeRecipientPublicKeyRequest),
    ];

    private static string Source(string relativePath) => File.ReadAllText(Path.Combine(Root(),
        relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Root()
    {
        var path = new DirectoryInfo(AppContext.BaseDirectory);
        while (path is not null && !Directory.Exists(Path.Combine(path.FullName, ".git"))) path = path.Parent;
        return path?.FullName ?? throw new InvalidOperationException("Repository root missing.");
    }

    private void Bite(bool condition, string bite, object? observed) =>
        observations.Observe(condition, bite, observed);

    private sealed class C5ObservationCollector
    {
        private readonly List<(string Bite, string Observed)> failures = [];

        public void Observe(bool condition, string bite, object? observed)
        {
            if (!condition)
                failures.Add((bite, observed?.ToString() ?? "<null>"));
        }

        public void ThrowIfAny()
        {
            if (failures.Count == 0) return;
            var names = string.Join(',', failures.Select(static failure => failure.Bite).Distinct(StringComparer.Ordinal));
            var details = string.Join(" | ", failures.Select(static failure => $"{failure.Bite}: observed={failure.Observed}"));
            throw new XunitException($"OBSERVED_RED:{names}; details={details}");
        }
    }
}
