using Microsoft.AspNetCore.Http;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.RawExport;
using Xunit.Sdk;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C5RecipientManagementApplicationTests : IDisposable
{
    private readonly C5ObservationCollector observations = new();

    public void Dispose() => observations.ThrowIfAny();

    [Fact]
    public async Task C505_exact_management_scope_and_operator_category_are_required()
    {
        var gateway = new ProbeGateway(); var service = Service(gateway);
        var request = new EnrollManagedRecipientRequest(Guid.NewGuid(), Guid.NewGuid());
        var wrongCategory = await service.EnrollRecipientAsync(Actor(AuthenticatedCallerCategory.BusinessConsumer,
            RecipientManagementApplicationService.RequiredScope), request, "c505-a", default);
        var missingScope = await service.EnrollRecipientAsync(Actor(AuthenticatedCallerCategory.OperatorAdmin,
            "operator.raw-export.sibling"), request, "c505-b", default);
        Bite(!wrongCategory.IsSuccess && !missingScope.IsSuccess && gateway.Calls == 0,
            "C505-REQUIRED-SCOPE-ROUTING-PRESERVATION", gateway.Calls);
        var superset = await service.EnrollRecipientAsync(Actor(AuthenticatedCallerCategory.OperatorAdmin,
            RecipientManagementApplicationService.RequiredScope, "operator.sibling"),
            new(Guid.NewGuid(), Guid.NewGuid()), "c505-c", default);
        Bite(!superset.IsSuccess && gateway.Calls == 1,
            "C505-ADMIN-SUPERSET-ROUTING", gateway.Calls);

        var managedKey = new ResolvedApiKey(Guid.NewGuid(), Guid.NewGuid(), "c505-managed-key",
            new HashSet<string>(StringComparer.Ordinal)
            {
                RecipientManagementApplicationService.RequiredScope,
                "operator.sibling",
            }, ApiKeyStatus.Active, DateTimeOffset.UtcNow.AddHours(1),
            AuthenticatedCallerCategory.OperatorAdmin, PrincipalId: Guid.NewGuid());
        var managementPolicies = new CountingPolicyProvider();
        var managementAuthenticator = new C5CredentialAwareApiKeyAuthenticator(
            new FixedApiKeyStore(managedKey), managementPolicies);
        var managementContext = ApiKeyContext("c505-presented");
        var managementAuth = await managementAuthenticator.AuthenticateAsync(managementContext,
            RecipientManagementApplicationService.RequiredScope, default);
        Bite(managementAuth.IsSuccess && managementPolicies.Calls == 0,
            "C505-ADMIN-SUPERSET-ROUTING",
            $"management={managementAuth.Error?.Code ?? "success"};policyCalls={managementPolicies.Calls}");

        var siblingPolicies = new CountingPolicyProvider();
        var siblingAuthenticator = new C5CredentialAwareApiKeyAuthenticator(
            new FixedApiKeyStore(managedKey), siblingPolicies);
        var siblingContext = ApiKeyContext("c505-presented");
        var siblingAuth = await siblingAuthenticator.AuthenticateAsync(
            siblingContext, "operator.sibling", default);
        Bite(!siblingAuth.IsSuccess && siblingAuth.Error?.Code == "CLIENT_APPLICATION_DISABLED"
            && siblingPolicies.Calls == 1,
            "C505-REQUIRED-SCOPE-ROUTING-PRESERVATION",
            $"sibling={siblingAuth.Error?.Code ?? "success"};policyCalls={siblingPolicies.Calls}");

        var policy = SourceRegion(
            "src/TagEkyc.Application/Ports/RecipientManagementPorts.cs",
            "public sealed class RecipientCredentialAuthenticationPolicy(",
            "public interface IManagedCredentialMaterialGenerator");
        var policyMethod = Region(
            policy,
            "public async Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(",
            "private static SessionOperationResult<AuthenticatedClientContext> Unauthorized");
        var managementExpression = Region(
            policyMethod,
            "var managementCredential =",
            "if (!recipientCredential && !managementCredential)");
        var programAuthenticator = SourceRegion(
            "src/TagEkyc.Api/Program.cs",
            "public sealed class C5CredentialAwareApiKeyAuthenticator(",
            null);
        var endpointAuthentication = SourceRegion(
            "src/TagEkyc.Api/RecipientManagementEndpoints.cs",
            "private static Task<SessionOperationResult<Application.AuthenticatedClientContext>> AuthenticateAsync(",
            "private static bool IsAuthorizedManager");

        Bite(OccurrenceCount(managementExpression, "AuthenticatedCallerCategory.OperatorAdmin") == 1
            && OccurrenceCount(managementExpression, "apiKey.Scopes.Contains(ManagementScope)") == 1,
            "C505-ADMIN-SUPERSET-ROUTING", "bounded management scope containment");
        Bite(OccurrenceCount(policyMethod, "var recipientCredential =") == 1
            && OccurrenceCount(policyMethod, "var managementCredential =") == 1
            && OccurrenceCount(policyMethod, "if (!recipientCredential && !managementCredential)") == 1
            && OccurrenceCount(policyMethod, "globalPolicies.GetPolicyAsync(apiKey.ClientApplicationId, cancellationToken)") == 1
            && OccurrenceCount(policyMethod, "policy is null || policy.Status == ClientApplicationStatus.Disabled") == 1,
            "C505-RESOLVED-CREDENTIAL-ROUTING", "bounded credential branch topology and policy fallback wiring");
        Bite(OccurrenceCount(endpointAuthentication,
                "context, RecipientManagementApplicationService.RequiredScope, cancellationToken") == 1
            && OccurrenceCount(programAuthenticator,
                "policy.AuthenticateAsync(presented, requiredScope, cancellationToken)") == 1
            && OccurrenceCount(managementExpression,
                "string.Equals(requiredScope, ManagementScope, StringComparison.Ordinal)") == 1,
            "C505-REQUIRED-SCOPE-ROUTING-PRESERVATION", "server-owned requiredScope chain");
        Bite(OccurrenceCount(programAuthenticator, "ILocalDevClientPolicyProvider globalPolicies") == 1,
            "C505-AUTH-ONLY-COMPOSITION-SCOPE", "unchanged global provider instance");
        Bite(Source("src/TagEkyc.Infrastructure/Auth/PostgresHashedApiKeyStore.cs")
                .Contains("RawExportManagedRecipientCredentials", StringComparison.Ordinal),
            "C505-MANAGED-POLICY-ZERO-LOCALDEV-CALL", "persisted authority join");
    }

    private static RecipientManagementApplicationService Service(ProbeGateway gateway, bool validKey = true) =>
        new(gateway, new Profile(validKey));

    private static AuthenticatedClientContext Actor(
        AuthenticatedCallerCategory category = AuthenticatedCallerCategory.OperatorAdmin,
        params string[] scopes) => new(Guid.NewGuid(), Guid.NewGuid(), "c5test0000000000", category,
            new HashSet<string>(scopes.Length == 0 ? [RecipientManagementApplicationService.RequiredScope] : scopes),
            PrincipalId: Guid.NewGuid());

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

    private static string Source(string relativePath) => File.ReadAllText(Path.Combine(Root(),
        relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string SourceRegion(string relativePath, string startAnchor, string? endAnchor) =>
        Region(Source(relativePath), startAnchor, endAnchor);

    private static string Region(string source, string startAnchor, string? endAnchor)
    {
        var start = ExactOneIndex(source, startAnchor);
        if (start < 0) return string.Empty;
        if (endAnchor is null) return source[start..];
        var end = ExactOneIndex(source, endAnchor);
        return end >= start + startAnchor.Length ? source[start..end] : string.Empty;
    }

    private static int ExactOneIndex(string source, string anchor)
    {
        if (string.IsNullOrEmpty(anchor)) return -1;
        var first = source.IndexOf(anchor, StringComparison.Ordinal);
        if (first < 0) return -1;
        return source.IndexOf(anchor, first + 1, StringComparison.Ordinal) < 0 ? first : -1;
    }

    private static int OccurrenceCount(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }
        return count;
    }

    private static DefaultHttpContext ApiKeyContext(string presentedKey)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-TagEkyc-Api-Key"] = presentedKey;
        return context;
    }

    private static string Root()
    {
        var path = new DirectoryInfo(AppContext.BaseDirectory);
        while (path is not null && !Directory.Exists(Path.Combine(path.FullName, ".git"))) path = path.Parent;
        return path?.FullName ?? throw new InvalidOperationException("Repository root missing.");
    }

    private sealed class Profile(bool valid) : IRecipientPublicKeyProfileValidator
    {
        public bool TryValidate(string algorithm, string spkiBase64, string fingerprintHex,
            out ValidatedRecipientPublicKey? key)
        { key = valid ? new([1], new byte[32]) : null; return valid; }
    }

    private sealed class FixedApiKeyStore(ResolvedApiKey key) : IApiKeyStore
    {
        public Task<ResolvedApiKey?> FindByPresentedKeyAsync(
            string presentedApiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<ResolvedApiKey?>(key);
    }

    private sealed class CountingPolicyProvider : ILocalDevClientPolicyProvider
    {
        public int Calls { get; private set; }

        public Task<LocalDevClientPolicy?> GetPolicyAsync(
            Guid clientApplicationId, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<LocalDevClientPolicy?>(null);
        }
    }

    private sealed class ProbeGateway : IRecipientManagementGateway
    {
        public int Calls { get; private set; }
        private Task<SessionOperationResult<RecipientManagementWriteResult<T>>> Hit<T>()
        { Calls++; return Task.FromResult(SessionOperationResult<RecipientManagementWriteResult<T>>.Failure("PROBE", "probe", 503)); }
        public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedRecipientIdentityDto>>> EnrollRecipientAsync(AuthenticatedClientContext a, EnrollManagedRecipientRequest r, string k, CancellationToken c) => Hit<ManagedRecipientIdentityDto>();
        public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> IssueCredentialAsync(AuthenticatedClientContext a, IssueManagedRecipientCredentialRequest r, string k, CancellationToken c) => Hit<ManagedCredentialDto>();
        public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> ReplaceCredentialAsync(AuthenticatedClientContext a, ReplaceManagedRecipientCredentialRequest r, string k, CancellationToken c) => Hit<ManagedCredentialDto>();
        public Task<SessionOperationResult<RecipientManagementWriteResult<ManagedCredentialDto>>> RevokeCredentialAsync(AuthenticatedClientContext a, RevokeManagedRecipientCredentialRequest r, string k, CancellationToken c) => Hit<ManagedCredentialDto>();
        public Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> EnrollKeyAsync(AuthenticatedClientContext a, EnrollRecipientPublicKeyRequest r, ValidatedRecipientPublicKey v, string k, CancellationToken c) => Hit<RecipientPublicKeyOperationResultDto>();
        public Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> RotateKeyAsync(AuthenticatedClientContext a, RotateRecipientPublicKeyRequest r, ValidatedRecipientPublicKey v, string k, CancellationToken c) => Hit<RecipientPublicKeyOperationResultDto>();
        public Task<SessionOperationResult<RecipientManagementWriteResult<RecipientPublicKeyOperationResultDto>>> RevokeKeyAsync(AuthenticatedClientContext a, RevokeRecipientPublicKeyRequest r, string k, CancellationToken c) => Hit<RecipientPublicKeyOperationResultDto>();
        public Task<SessionOperationResult<ManagedRecipientReadinessDto>> ReadReadinessAsync(AuthenticatedClientContext a, Guid r, CancellationToken c)
        { Calls++; return Task.FromResult(SessionOperationResult<ManagedRecipientReadinessDto>.Failure("PROBE", "probe", 503)); }
    }
}
