using System.Reflection;
using System.Runtime.CompilerServices;
using TagEkyc.Application.Ports;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.ArchTests;

public sealed class Tip83E2AuditDurabilityBoundaryTests
{
    public static IEnumerable<object[]> AppendOnlyRepositorySurfaces()
    {
        yield return
        [
            typeof(ICaptureArtifactRepository).FullName!,
            typeof(EfCaptureArtifactRepository).FullName!,
            new[] { "AppendAsync", "ListBySessionAsync" },
        ];
        yield return
        [
            typeof(IEvidenceResultRepository).FullName!,
            typeof(EfEvidenceResultRepository).FullName!,
            new[] { "AppendAsync", "ListBySessionAsync" },
        ];
        yield return
        [
            typeof(IVerificationDecisionRepository).FullName!,
            typeof(EfVerificationDecisionRepository).FullName!,
            new[] { "AppendAsync", "ListBySessionAsync", "GetAsync" },
        ];
        yield return
        [
            typeof(IEvidencePackageRepository).FullName!,
            typeof(EfEvidencePackageRepository).FullName!,
            new[] { "AppendAsync", "GetAsync", "GetBySessionAsync" },
        ];
        yield return
        [
            typeof(IInternalEvidenceManifestRepository).FullName!,
            typeof(EfEvidenceManifestRepository).FullName!,
            new[] { "AppendAsync", "GetByPackageAsync" },
        ];
        yield return
        [
            typeof(IAuditEventRepository).FullName!,
            typeof(EfAuditEventRepository).FullName!,
            new[] { "AppendAsync", "ListBySessionAsync" },
        ];
        yield return
        [
            typeof(IRawExportPolicyRepository).FullName!,
            typeof(EfRawExportPolicyRepository).FullName!,
            new[]
            {
                "AbandonDraftAsync",
                "AddVersionAsync",
                "CatalogApproveAsync",
                "GetLatestCatalogApprovedVersionAsync",
                "GetLatestVersionAsync",
                "GetVersionAsync",
                "ListAsync",
            },
        ];
        yield return
        [
            typeof(IRawExportControlPlaneRepository).FullName!,
            typeof(EfRawExportControlPlaneRepository).FullName!,
            new[]
            {
                "AcceptFulfillmentAsync",
                "ActivatePolicyAsync",
                "GrantControlAuthorityAsync",
                "GrantExportPolicyAsync",
                "ResolveExportEligibilityForAuthorizationAsync",
                "RevokeControlAuthorityAsync",
                "RevokeExportPolicyGrantAsync",
                "RevokePolicyAsync",
                "SuspendPolicyAsync",
                "WithdrawFulfillmentAsync",
            },
        ];
    }

    [Theory]
    [MemberData(nameof(AppendOnlyRepositorySurfaces))]
    public void Append_only_repository_surfaces_are_exact_allowlists(
        string interfaceTypeName,
        string implementationTypeName,
        string[] allowedMethods)
    {
        var interfaceType = typeof(IAuditEventRepository).Assembly.GetType(interfaceTypeName, throwOnError: false);
        var implementationType = typeof(EfAuditEventRepository).Assembly.GetType(implementationTypeName, throwOnError: false);

        Assert.NotNull(interfaceType);
        Assert.NotNull(implementationType);
        Assert.True(interfaceType!.IsInterface, $"{interfaceTypeName} must remain an interface.");
        Assert.True(interfaceType.IsAssignableFrom(implementationType), $"{implementationTypeName} must implement {interfaceTypeName}.");

        AssertExactDeclaredMethodSet(interfaceType, allowedMethods);
        AssertExactDeclaredMethodSet(implementationType!, allowedMethods);
    }

    private static void AssertExactDeclaredMethodSet(Type type, IReadOnlyCollection<string> allowedMethods)
    {
        var methodNames = type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Where(method => method.GetCustomAttribute<CompilerGeneratedAttribute>() is null)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var expected = allowedMethods
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(methodNames);
        Assert.Equal(expected, methodNames);
    }
}
