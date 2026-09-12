using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1TransitionCoverageTests(PostgresPersistenceFixture postgres)
{
    private const string Actor = "00000000-0000-4000-8000-000000000001";
    private const string Agent = "40000000-0000-4000-8000-000000000001";
    private const string Client = "91000000-0000-4000-8000-000000000001";
    private const string Session = "90000000-0000-4000-8000-000000000001";
    private const string Capability = "93000000-0000-4000-8000-000000000001";

    [Theory]
    [InlineData("role_policy", "ROLE", "10000000-0000-4000-8000-000000000001")]
    [InlineData("trust_profile", "TRUST", "20000000-0000-4000-8000-000000000001")]
    [InlineData("configuration", "CONFIGURATION", "30000000-0000-4000-8000-000000000001")]
    public async Task CatalogExpectedHead_RejectsStaleWriter_RollbackPublishesNewAuditedRevision(
        string family, string diagnostic, string catalog)
    {
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_catalog_proof");
        await using var db = isolated.CreateDbContext();
        await Seed(db);
        string Call(long head, bool original) => family switch
        {
            "role_policy" => $"tagekyc.capture_runtime_publish_role_policy('{Actor}',gen_random_uuid(),'{catalog}',{head},now()-interval '1 minute',ARRAY['{(original ? "Bind" : "Configuration")}'],decode(repeat('31',32),'hex'),now())",
            "trust_profile" => $"tagekyc.capture_runtime_publish_trust_profile('{Actor}',gen_random_uuid(),'{catalog}',{head},now()-interval '1 minute',now()+interval '1 day','Managed',{(original ? "true" : "false")},true,false,decode(repeat('32',32),'hex'),now())",
            _ => $"tagekyc.capture_runtime_publish_configuration('{Actor}',gen_random_uuid(),'{catalog}',{head},now()-interval '1 minute',now()+interval '1 day',true,{(original ? 60 : 30)},100,60,1000,1000,10000,10000,10000,1024,decode(repeat('33',32),'hex'),now())"
        };
        Assert.Equal("Applied", await Result(db, Call(1, false), "tagekyc_capture_runtime_operator"));
        var conflict = await Assert.ThrowsAsync<PostgresException>(() => Result(db, Call(1, true), "tagekyc_capture_runtime_operator"));
        Assert.Equal($"TIP88C1C6BA_{diagnostic}_HEAD_CONFLICT", conflict.MessageText);
        Assert.Equal(2, await Scalar(db, $"SELECT count(*)::integer FROM tagekyc.capture_runtime_{family}_revisions WHERE \"CatalogId\"='{catalog}'"));
        Assert.Equal(1, await Scalar(db, $"SELECT count(*)::integer FROM tagekyc.capture_runtime_management_operations WHERE \"TargetId\"='{catalog}'"));
        // A rollback restores values by publishing revision 3, never updating revision 1/2.
        Assert.Equal("Applied", await Result(db, Call(2, true), "tagekyc_capture_runtime_operator"));
        Assert.Equal(3, await Scalar(db, $"SELECT \"CurrentRevision\"::integer FROM tagekyc.capture_runtime_{family}_heads WHERE \"CatalogId\"='{catalog}'"));
        Assert.Equal(3, await Scalar(db, $"SELECT count(*)::integer FROM tagekyc.capture_runtime_{family}_revisions WHERE \"CatalogId\"='{catalog}'"));
        Assert.Equal(2, await Scalar(db, $"SELECT count(*)::integer FROM tagekyc.capture_runtime_management_events WHERE \"TargetId\"='{catalog}' AND \"EventType\"='Applied'"));
        var valueColumn = family switch { "role_policy" => "Roles", "trust_profile" => "RetainedRawEnabled", _ => "PlaintextBudgetSeconds" };
        Assert.Equal(1, await Scalar(db, $"SELECT count(*)::integer FROM tagekyc.capture_runtime_{family}_revisions a JOIN tagekyc.capture_runtime_{family}_revisions b ON a.\"CatalogId\"=b.\"CatalogId\" WHERE a.\"CatalogId\"='{catalog}' AND a.\"Revision\"=1 AND b.\"Revision\"=3 AND a.\"{valueColumn}\"=b.\"{valueColumn}\""));
        // Test the immutable-row guard without guessing a business column name.
        await using var mutation = await db.Database.BeginTransactionAsync();
        var immutable = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(
            $"UPDATE tagekyc.capture_runtime_{family}_revisions SET \"Revision\"=\"Revision\" WHERE \"CatalogId\"='{catalog}' AND \"Revision\"=1"));
        Assert.Equal("P0001", immutable.SqlState);
        await mutation.RollbackAsync();
    }

    [Fact]
    public async Task ConfigurationOverride_NarrowingIsAllowed_WideningFailsWithoutChangingBase()
    {
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_override_proof");
        await using var db = isolated.CreateDbContext();
        await Seed(db);
        var id = Guid.NewGuid();
        await db.Database.ExecuteSqlRawAsync($"""
            INSERT INTO tagekyc.capture_runtime_configuration_overrides
            ("ConfigurationOverrideId","CaptureAgentId","BaseConfigurationId","BaseConfigurationRevision","PlaintextBudgetSeconds","Revision","UpdatedAtUtc")
            VALUES ('{id}','{Agent}','30000000-0000-4000-8000-000000000001',1,30,1,now());
            """);
        Assert.Equal("Applied", await Result(db,
            $"tagekyc.capture_runtime_assign_configuration('{Actor}',gen_random_uuid(),'{Agent}','30000000-0000-4000-8000-000000000001',1,1,'{id}',decode(repeat('41',32),'hex'),now())",
            "tagekyc_capture_runtime_operator"));
        Assert.Equal(30, await Scalar(db, """
            SELECT plaintext_budget_seconds FROM tagekyc.capture_runtime_resolve_configuration(
            '40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001',
            '60000000-0000-4000-8000-000000000001',1,now())
            """));
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            var denied = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(
                $"UPDATE tagekyc.capture_runtime_configuration_overrides SET \"PlaintextBudgetSeconds\"=61 WHERE \"ConfigurationOverrideId\"='{id}'"));
            Assert.Equal("TIP88C1C6BA_CONFIGURATION_OVERRIDE_WIDENS_BASE", denied.MessageText);
            await tx.RollbackAsync();
        }
        Assert.Equal(30, await Scalar(db, $"SELECT \"PlaintextBudgetSeconds\" FROM tagekyc.capture_runtime_configuration_overrides WHERE \"ConfigurationOverrideId\"='{id}'"));
        Assert.Equal(60, await Scalar(db, "SELECT \"PlaintextBudgetSeconds\" FROM tagekyc.capture_runtime_configuration_revisions WHERE \"Revision\"=1"));
        // This does not prohibit publishing/assigning a legitimately different base revision.
    }

    [Fact]
    public async Task CapabilityReplace_WrongRevisionAndBoundDeny_SecondRuntimeCannotTakeOver()
    {
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_capability_conflict");
        await using var db = isolated.CreateDbContext();
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await Tip88C1C6BA1AppendAuthorityTests.SeedBindingAsync(db);
            await tx.CommitAsync();
        }
        string Replace(string session, string current, long revision) =>
            $"tagekyc.capture_runtime_issue_or_replace_capability('{Client}','{session}','Replace','{current}',{revision},gen_random_uuid(),gen_random_uuid(),'replaceproof',decode(repeat('51',32),'hex'),1,decode(repeat('52',32),'hex'),now())";
        Assert.Equal("CONFLICT", await Result(db, Replace(Session, Capability, 2), "tagekyc_capture_runtime_application"));
        Assert.Equal(0, await Scalar(db, "SELECT count(*)::integer FROM tagekyc.capture_capability_operations WHERE \"OperationKind\"='Replace'"));
        // A separate ActiveUnbound capability is the positive control for replacement.
        var session2 = Guid.NewGuid(); var capability2 = Guid.NewGuid();
        db.Set<TagEkyc.Infrastructure.Persistence.Entities.VerificationSessionRow>().Add(new()
        {
            Id = session2, ClientApplicationId = Guid.Parse(Client), SubjectRef = "synthetic-subject",
            Profile = "STANDARD_EKYC_PROFILE", Purpose = "SyntheticProof", State = "Pending",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), CreatedAt = DateTimeOffset.UtcNow,
            RequestId = "r", CorrelationId = "c", BindingNonceHash = "challenge"
        });
        await db.SaveChangesAsync();
        Assert.Equal("CREATED", await Result(db, $"tagekyc.capture_runtime_issue_or_replace_capability('{Client}','{session2}','Issue',NULL,NULL,gen_random_uuid(),'{capability2}','issueproofxx',decode(repeat('53',32),'hex'),1,decode(repeat('54',32),'hex'),now())", "tagekyc_capture_runtime_application"));
        Assert.Equal("CONFLICT", await Result(db, Replace(session2.ToString(), capability2.ToString(), 2), "tagekyc_capture_runtime_application"));
        Assert.Equal("CREATED", await Result(db, Replace(session2.ToString(), capability2.ToString(), 1), "tagekyc_capture_runtime_application"));
        Assert.Equal(1, await Scalar(db, "SELECT count(*)::integer FROM tagekyc.capture_capability_operations WHERE \"OperationKind\"='Replace'"));
        // Clone only an independent valid synthetic runtime lineage, not a binding.
        await using (var lineage = await db.Database.BeginTransactionAsync())
        {
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO tagekyc.capture_runtime_registrations SELECT
              '40000000-0000-4000-8000-000000000002',"RuntimeType","TrustProfileId","TrustProfileRevision","ConfigurationId","ConfigurationRevision",NULL,NULL,NULL,'Active',1,now(),NULL,NULL,NULL,NULL
              FROM tagekyc.capture_runtime_registrations WHERE "CaptureAgentId"='40000000-0000-4000-8000-000000000001';
            INSERT INTO tagekyc.capture_runtime_installations VALUES('50000000-0000-4000-8000-000000000002','40000000-0000-4000-8000-000000000002',NULL,NULL,'Pending',1,now(),NULL,NULL,NULL,NULL);
            INSERT INTO tagekyc.capture_runtime_credential_generations VALUES('60000000-0000-4000-8000-000000000002',1,'50000000-0000-4000-8000-000000000002','70000000-0000-4000-8000-000000000002',decode(repeat('04',91),'hex'),'TAG-EKYC-CRT1-ECDSA-P256-SHA256',decode(repeat('22',32),'hex'),'10000000-0000-4000-8000-000000000001',1,now()-interval '1 minute',now()+interval '1 day','Pending',1,NULL,NULL,NULL,NULL);
            UPDATE tagekyc.capture_runtime_installations SET "CurrentCredentialId"='60000000-0000-4000-8000-000000000002',"CurrentCredentialGeneration"=1,"LifecycleState"='Active',"Revision"=2 WHERE "DeviceInstallationId"='50000000-0000-4000-8000-000000000002';
            UPDATE tagekyc.capture_runtime_credential_generations SET "State"='Active',"Revision"=2 WHERE "CredentialId"='60000000-0000-4000-8000-000000000002';
            """);
        await lineage.CommitAsync();
        }
        var loser = await Result(db, $"tagekyc.capture_runtime_bind_capability('40000000-0000-4000-8000-000000000002','50000000-0000-4000-8000-000000000002','60000000-0000-4000-8000-000000000002',1,'{Capability}',true,gen_random_uuid(),decode(repeat('61',32),'hex'),now())", "tagekyc_capture_runtime_application", denyShape: true);
        Assert.Equal("ACCESS_DENIED", loser);
        Assert.Equal(1, await Scalar(db, "SELECT count(*)::integer FROM tagekyc.capture_execution_bindings"));
        Assert.Equal(1, await Scalar(db, "SELECT count(*)::integer FROM tagekyc.capture_capability_operations WHERE \"OperationKind\"='Bind'"));
        var available = await db.Database.SqlQueryRaw<Guid>($"SELECT \"CaptureCapabilityId\" AS \"Value\" FROM tagekyc.capture_capabilities WHERE \"VerificationSessionId\"='{session2}' AND \"State\"='ActiveUnbound'").SingleAsync();
        Assert.Equal("CREATED", await Result(db, $"tagekyc.capture_runtime_bind_capability('40000000-0000-4000-8000-000000000002','50000000-0000-4000-8000-000000000002','60000000-0000-4000-8000-000000000002',1,'{available}',true,gen_random_uuid(),decode(repeat('62',32),'hex'),now())", "tagekyc_capture_runtime_application"));
        Assert.Equal(2, await Scalar(db, "SELECT count(*)::integer FROM tagekyc.capture_execution_bindings"));
    }

    private static async Task Seed(TagEkycDbContext db)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TagEkyc.sln"))) root = root.Parent;
        var text = File.ReadAllText(Path.Combine(root!.FullName, "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a1_postgresql16_runtime_proof_companion.md"));
        var start = text.IndexOf("INSERT INTO tagekyc.platform_operator_credentials", StringComparison.Ordinal);
        var end = text.IndexOf("-- Publish newer heads", start, StringComparison.Ordinal);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(text[start..end].Replace("ARRAY['Configuration']", "ARRAY['Bind']", StringComparison.Ordinal));
        await tx.CommitAsync();
    }
    private static async Task<string> Result(TagEkycDbContext db, string call, string role, bool denyShape = false)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE " + role);
        string result;
        if (denyShape)
        {
            var json = await db.Database.SqlQueryRaw<string>("SELECT to_jsonb(r)::text AS \"Value\" FROM " + call + " r").SingleAsync();
            using var document = JsonDocument.Parse(json);
            result = document.RootElement.GetProperty("result_code").GetString()!;
            Assert.All(document.RootElement.EnumerateObject().Where(p => p.Name != "result_code"), p => Assert.Equal(JsonValueKind.Null, p.Value.ValueKind));
        }
        else result = await db.Database.SqlQueryRaw<string>("SELECT result_code AS \"Value\" FROM " + call).SingleAsync();
        await tx.CommitAsync(); return result;
    }
    private static Task<int> Scalar(TagEkycDbContext db, string query) => db.Database.SqlQueryRaw<int>("SELECT (" + query + ") AS \"Value\"").SingleAsync();
}
