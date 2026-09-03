using System.Security.Cryptography;
using Xunit.Sdk;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1C5RecipientManagementArchTests : IDisposable
{
    private readonly C5ObservationCollector observations = new();

    public void Dispose() => observations.ThrowIfAny();

    [Fact]
    public void C527_model_snapshot_tripwires_are_platform_independent_and_lockstep()
    {
        var root=Root(); var snapshot=Path.Combine(root,"src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs");
        var sha=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(snapshot)));
        var owners=new[]{"Tip88B1E3ResolverReadBoundaryTests.cs","Tip88C1B2R3VerifiedCiphertextStagingTests.cs","Tip88C1C2RecipientPackageTests.cs"};
        var count=owners.Count(file=>File.ReadAllText(Path.Combine(root,"tests/TagEkyc.IntegrationTests",file)).Contains(sha,StringComparison.Ordinal));
        Bite(count==3,"C527-THREE-TRIPWIRE-LOCKSTEP",$"sha={sha};count={count}");
    }

    [Fact]
    public void C530_debts_nonclaims_and_product_ready_gates_remain_open()
    {
        var scope=File.ReadAllText(Path.Combine(Root(),"docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c5_managed_recipient_enrollment_activation_readiness_scope_brief.md"));
        var registerStart=scope.IndexOf("## 18. Debt and non-claim register",StringComparison.Ordinal);
        var registerEnd=scope.IndexOf("## 19. Product readiness remaining after C5",registerStart,StringComparison.Ordinal);
        var carriedRegister=registerStart>=0&&registerEnd>registerStart?scope[registerStart..registerEnd]:string.Empty;
        var requiredGlobal=new[]{"C4-AUTH-SCOPE-PROVISIONING-DEBT-01","Production Raw BIO","deployment"};
        var nonclaim="C5-C2-PRELOCK-CLOCK-NONCLAIM-01";
        Bite(requiredGlobal.All(value=>scope.Contains(value,StringComparison.OrdinalIgnoreCase))
            &&carriedRegister.Contains(nonclaim,StringComparison.Ordinal),
            "C530-DEBT-AND-NONCLAIM-ACCOUNTING",
            $"global={string.Join(',',requiredGlobal.Where(v=>!scope.Contains(v,StringComparison.OrdinalIgnoreCase)))};registerNonclaim={carriedRegister.Contains(nonclaim,StringComparison.Ordinal)}");
        var asBuilt=File.ReadAllText(Path.Combine(Root(),"docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c5_managed_recipient_enrollment_activation_readiness_as_built.md"));
        Bite(!asBuilt.Contains("package decrypted",StringComparison.OrdinalIgnoreCase)
            &&!asBuilt.Contains("package downloaded",StringComparison.OrdinalIgnoreCase),
            "C530-NO-DOWNLOAD-DECRYPT-CLAIM","no client-side outcome claim");
        Bite(asBuilt.Contains("Production Raw BIO",StringComparison.OrdinalIgnoreCase)
            &&asBuilt.Contains("deployment",StringComparison.OrdinalIgnoreCase),
            "C530-PRODUCT-BOUNDARY-ACCOUNTING","remaining gates retained");
        var evidence=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"eKyc","review-work","TIP88C1-C5-controlled-build-evidence","C5_MUTATION_PREAUDIT_78.tsv");
        var lines=File.Exists(evidence)?File.ReadAllLines(evidence):[];
        var expectedIds=Enumerable.Range(1,80).Where(value=>value is not 65 and not 78)
            .Select(value=>$"C5M{value:D2}").ToArray();
        static string[] Columns(string line)=>line.Split('\t').Select(value=>value.Trim('"')).ToArray();
        var rows=lines.Skip(1).Select(Columns).ToArray();
        Bite((lines.FirstOrDefault() is { } header?Columns(header):[]).SequenceEqual([
                "MutationId","SurfaceKind","Target","CanonicalAnchor","ExactMutationEdit",
                "OwnerProof","NamedRedBite","MaskingPrecheck","RestoreAuthority","RequirementIds"])
            ==true&&rows.Length==78&&rows.All(row=>row.Length==10&&row.All(value=>!string.IsNullOrWhiteSpace(value)))
            &&rows.Select(row=>row[0]).SequenceEqual(expectedIds,StringComparer.Ordinal),
            "C530-PROOF-CONSTRUCTION-PROTOCOL",$"lines={lines.Length};rows={rows.Length}");
    }

    private static string Root()
    { var path=new DirectoryInfo(AppContext.BaseDirectory); while(path is not null&&!Directory.Exists(Path.Combine(path.FullName,".git")))path=path.Parent; return path?.FullName??throw new InvalidOperationException("repo root missing"); }
    private void Bite(bool condition,string bite,object observed) =>
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
