using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
// Migration-owner SQL behavior proofs. Authentication/role isolation is exercised
// independently by the HTTP and ACL suites; these calls do not claim CRT1 coverage.
public sealed class Tip88C1C6BA1LifecycleRaceTests(PostgresPersistenceFixture postgres)
{
    private const string Actor = "00000000-0000-4000-8000-000000000001";
    private const string Agent = "40000000-0000-4000-8000-000000000001";
    private const string Installation = "50000000-0000-4000-8000-000000000001";
    private const string Credential = "60000000-0000-4000-8000-000000000001";
    private const string Role = "10000000-0000-4000-8000-000000000001";
    private const string Digest = "decode(repeat('61',32),'hex')";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Lifecycle_ConcurrentExpectedRevisionLoser_AndFrozenReplay(bool terminal)
    {
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_lifecycle_race");
        await using var db = isolated.CreateDbContext();
        var cs = db.Database.GetConnectionString()!;
        await Seed(cs);
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        string Call(bool first, long revision, Guid operation) =>
            $"tagekyc.capture_runtime_{(terminal ? (first ? "revoke" : "retire") : (first ? "suspend" : "reactivate"))}('{Actor}','{operation}','{Agent}',{revision},'{(terminal ? (first ? "OperatorRevocation" : "OperatorRetirement") : (first ? "OperatorSuspension" : "OperatorReactivation"))}',{Digest},now())";
        var pair = await BlockedRace(cs, Call(true, 1, winnerId), Call(false, 1, loserId));
        Assert.Equal("Applied", Code(pair.First));
        Assert.Equal("Conflict", Code(pair.Second));
        Assert.Equal(1L, await Count(cs, "capture_runtime_management_operations"));
        Assert.Equal(1L, await Count(cs, "capture_runtime_management_events"));
        var advanced = await Execute(cs, Call(false, 2, loserId));
        Assert.Equal("Applied", Code(advanced));
        Assert.Equal(terminal ? "Retired" : "Active", advanced.GetProperty("state").GetString());
        var replay = await Execute(cs, Call(true, 1, winnerId));
        Assert.Equal(pair.First.GetRawText(), replay.GetRawText());
        Assert.Equal(2L, await Count(cs, "capture_runtime_management_operations"));
        Assert.Equal(2L, await Count(cs, "capture_runtime_management_events"));
        if (terminal)
        {
            var denied = await Execute(cs, $"tagekyc.capture_runtime_reactivate('{Actor}','{Guid.NewGuid()}','{Agent}',3,'OperatorReactivation',{Digest},now())");
            Assert.NotEqual("Applied", Code(denied));
            Assert.Equal(2L, await Count(cs, "capture_runtime_management_operations"));
            Assert.Equal(1L, await Scalar(cs, "SELECT count(*) FROM tagekyc.capture_runtime_installations WHERE \"LifecycleState\"='Retired'"));
            Assert.Equal(1L, await Scalar(cs, "SELECT count(*) FROM tagekyc.capture_runtime_credential_generations WHERE \"State\"='Retired'"));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CredentialRevoke_ConcurrentRotation_BothSerialOrdersHaveOneWinner(bool revokeFirst)
    {
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_rotation_revoke_race");
        await using var db = isolated.CreateDbContext();
        var cs = db.Database.GetConnectionString()!;
        await Seed(cs);
        var rotation = (await Execute(cs, Authorize(Guid.NewGuid()))).GetProperty("rotation_id").GetGuid();
        var operation = Guid.NewGuid(); var candidate = Guid.NewGuid();
        var complete = Complete(rotation, operation, candidate);
        var revoke = $"tagekyc.capture_runtime_revoke_credential('{Actor}','{Guid.NewGuid()}','{Agent}','{Installation}','{Credential}',1,2,'CredentialCompromise',{Digest},now())";
        var pair = await BlockedRace(cs, revokeFirst ? revoke : complete, revokeFirst ? complete : revoke,
            secondMayThrow: revokeFirst);
        Assert.Equal(revokeFirst ? "Revoked" : "Applied", Code(pair.First));
        Assert.Equal(revokeFirst ? "TIP88C1C6BA_ROTATION_CONFLICT" : "ResourceNotAvailable", Code(pair.Second));
        Assert.Equal(revokeFirst ? 0L : 1L, await Count(cs, "capture_runtime_rotation_completion_operations"));
        Assert.Equal(revokeFirst ? 0L : 1L, await Count(cs, "capture_runtime_rotation_completion_events"));
        Assert.Equal(revokeFirst ? 1L : 2L, await Count(cs, "capture_runtime_credential_generations"));
        Assert.Equal(revokeFirst ? 1L : 2L, await Scalar(cs, "SELECT \"CurrentCredentialGeneration\" FROM tagekyc.capture_runtime_installations"));
        Assert.Equal(0L, await Scalar(cs, "SELECT count(*) FROM tagekyc.capture_runtime_installations i JOIN tagekyc.capture_runtime_credential_generations g ON g.\"CredentialId\"=i.\"CurrentCredentialId\" AND g.\"Generation\"=i.\"CurrentCredentialGeneration\" WHERE (i.\"LifecycleState\"='Active') <> (g.\"State\"='Active')"));
        var replay = await Execute(cs, revokeFirst ? revoke : complete);
        Assert.Equal(revokeFirst ? "Revoked" : "Replay", Code(replay));
        Assert.Equal(revokeFirst ? 0L : 1L, await Count(cs, "capture_runtime_rotation_completion_operations"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IssuanceReplay_OverlapsRevocation_WithoutResurrectionOrFalseNotFound(bool rotation)
    {
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_issue_revoke_race");
        await using var db = isolated.CreateDbContext();
        var cs = db.Database.GetConnectionString()!;
        await Seed(cs);
        var issueId = Guid.NewGuid(); var revokeId = Guid.NewGuid();
        var issue = rotation ? Authorize(issueId) : $"tagekyc.capture_runtime_issue_bootstrap('{Actor}','{issueId}','Managed','20000000-0000-4000-8000-000000000001',1,'{Role}',1,'30000000-0000-4000-8000-000000000001',1,now()+interval '5 minutes',{Digest},'raceboot0001',{Digest},1,{Digest},now())";
        var first = await Execute(cs, issue);
        Assert.Equal(rotation ? "Applied" : "Created", Code(first));
        var id = first.GetProperty(rotation ? "rotation_id" : "bootstrap_issuance_id").GetGuid();
        var revoke = rotation
            ? $"tagekyc.capture_runtime_revoke_rotation('{Actor}','{revokeId}','{id}',1,'OperatorRevocation',{Digest},now())"
            : $"tagekyc.capture_runtime_revoke_bootstrap('{Actor}','{revokeId}','{id}',1,'OperatorRevocation',{Digest},now())";
        await using var a = new NpgsqlConnection(cs); await a.OpenAsync();
        await using var transaction = await a.BeginTransactionAsync();
        var revoked = await Query(a, transaction, revoke);
        Assert.Equal(rotation ? "Applied" : "Revoked", Code(revoked));
        // An issuance id is not exposed until its first transaction commits.
        // The meaningful overlap is a replay of that committed issue with revoke;
        // never leak a test-only uncommitted generated id into a public request.
        var overlappingReplay = await Execute(cs, issue);
        Assert.Equal(rotation ? "Replay" : "ExistingMatchSecretUnavailable", Code(overlappingReplay));
        await transaction.CommitAsync();
        var after = await Execute(cs, issue);
        Assert.Equal(overlappingReplay.GetRawText(), after.GetRawText());
        Assert.Equal(2L, await Count(cs, "capture_runtime_management_operations"));
        Assert.Equal(2L, await Count(cs, "capture_runtime_management_events"));
        var revokeReplay = await Execute(cs, revoke);
        Assert.Equal(rotation ? "Replay" : "Revoked", Code(revokeReplay));
        Assert.Equal("Revoked", revokeReplay.GetProperty("state").GetString());
        Assert.Equal(1L, await Scalar(cs, $"SELECT count(*) FROM tagekyc.{(rotation ? "capture_runtime_rotation_authorizations" : "capture_runtime_bootstrap_issuances")} WHERE \"State\"='Revoked'"));
        if (rotation)
        {
            var error = await Assert.ThrowsAsync<PostgresException>(() => Execute(cs, Complete(id, Guid.NewGuid(), Guid.NewGuid())));
            Assert.Equal("TIP88C1C6BA_ROTATION_DENIED", error.MessageText);
            Assert.Equal(0L, await Count(cs, "capture_runtime_rotation_completion_operations"));
            Assert.Equal(1L, await Count(cs, "capture_runtime_credential_generations"));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RotationExpiry_ConcurrentRevoke_HasOneTerminalPairAndFrozenDenial(bool expiryFirst)
    {
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_rotation_expiry_race");
        await using var db = isolated.CreateDbContext();
        var cs = db.Database.GetConnectionString()!;
        await Seed(cs);
        var rotation = (await Execute(cs, Authorize(Guid.NewGuid()))).GetProperty("rotation_id").GetGuid();
        var complete = Complete(rotation, Guid.NewGuid(), Guid.NewGuid()).Replace(
            ",now())", ",now()+interval '6 minutes')", StringComparison.Ordinal);
        var revoke = $"tagekyc.capture_runtime_revoke_rotation('{Actor}','{Guid.NewGuid()}','{rotation}',1,'OperatorRevocation',{Digest},now()+interval '6 minutes')";
        var pair = await BlockedRace(cs, expiryFirst ? complete : revoke, expiryFirst ? revoke : complete, secondMayThrow: true);
        Assert.Equal(expiryFirst ? "TerminalizedExpiredAndDenied" : "Applied", Code(pair.First));
        Assert.Equal(expiryFirst ? "TIP88C1C6BA_ROTATION_CONFLICT" : "TIP88C1C6BA_ROTATION_DENIED", Code(pair.Second));
        var replay = await Execute(cs, expiryFirst ? complete : revoke);
        Assert.Equal(expiryFirst ? "TerminalizedExpiredAndDenied" : "Replay", Code(replay));
        Assert.Equal(expiryFirst ? 1L : 0L, await Count(cs, "capture_runtime_rotation_completion_operations"));
        Assert.Equal(expiryFirst ? 1L : 0L, await Count(cs, "capture_runtime_rotation_completion_events"));
        Assert.Equal(1L, await Count(cs, "capture_runtime_credential_generations"));
        Assert.Equal(1L, await Scalar(cs, "SELECT \"CurrentCredentialGeneration\" FROM tagekyc.capture_runtime_installations"));
    }

    [Theory]
    [InlineData("role_policy", "ROLE", "10000000-0000-4000-8000-000000000001")]
    [InlineData("trust_profile", "TRUST", "20000000-0000-4000-8000-000000000001")]
    [InlineData("configuration", "CONFIGURATION", "30000000-0000-4000-8000-000000000001")]
    public async Task CatalogPublication_ConcurrentHeadCas_OneAuditedWinner(string family, string diagnostic, string catalog)
    {
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_catalog_race");
        await using var db = isolated.CreateDbContext();
        var cs = db.Database.GetConnectionString()!;
        await Seed(cs);
        string Call(Guid operation) => family switch
        {
            "role_policy" => $"tagekyc.capture_runtime_publish_role_policy('{Actor}','{operation}','{catalog}',1,now()-interval '1 minute',ARRAY['Bind'],{Digest},now())",
            "trust_profile" => $"tagekyc.capture_runtime_publish_trust_profile('{Actor}','{operation}','{catalog}',1,now()-interval '1 minute',now()+interval '1 day','Managed',true,true,false,{Digest},now())",
            _ => $"tagekyc.capture_runtime_publish_configuration('{Actor}','{operation}','{catalog}',1,now()-interval '1 minute',now()+interval '1 day',true,30,100,60,1000,1000,10000,10000,10000,1024,{Digest},now())"
        };
        var winnerCall = Call(Guid.NewGuid());
        var pair = await BlockedRace(cs, winnerCall, Call(Guid.NewGuid()), secondMayThrow: true);
        Assert.Equal("Applied", Code(pair.First));
        Assert.Equal($"TIP88C1C6BA_{diagnostic}_HEAD_CONFLICT", Code(pair.Second));
        Assert.Equal("Replay", Code(await Execute(cs, winnerCall)));
        Assert.Equal(2L, await Count(cs, "capture_runtime_" + family + "_revisions"));
        Assert.Equal(1L, await Count(cs, "capture_runtime_management_operations"));
        Assert.Equal(1L, await Count(cs, "capture_runtime_management_events"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BindConcurrentReplayOrReplace_OneBindingNoSuccessor(bool replace)
    {
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_binding_race");
        await using var db = isolated.CreateDbContext();
        var cs = db.Database.GetConnectionString()!;
        await Seed(cs);
        var session = Guid.NewGuid(); var client = Guid.NewGuid(); var capability = Guid.NewGuid();
        db.Sessions.Add(new()
        {
            Id = session, ClientApplicationId = client, SubjectRef = "synthetic-race-subject",
            Profile = "StandardEkycProfile", Purpose = "SyntheticProof", State = "Pending",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), CreatedAt = DateTimeOffset.UtcNow,
            RequestId = "race-r", CorrelationId = "race-c", BindingNonceHash = "challenge"
        });
        await db.SaveChangesAsync();
        var issue = $"tagekyc.capture_runtime_issue_or_replace_capability('{client}','{session}','Issue',NULL,NULL,'{Guid.NewGuid()}','{capability}','racecap00001',{Digest},1,{Digest},now())";
        Assert.Equal("CREATED", Code(await Execute(cs, issue)));
        var bind = $"tagekyc.capture_runtime_bind_capability('{Agent}','{Installation}','{Credential}',1,'{capability}',true,'{Guid.NewGuid()}',{Digest},now())";
        var competitor = replace ? $"tagekyc.capture_runtime_issue_or_replace_capability('{client}','{session}','Replace','{capability}',1,'{Guid.NewGuid()}','{Guid.NewGuid()}','racecap00002',{Digest},1,{Digest},now())" : bind;
        var pair = await BlockedRace(cs, bind, competitor);
        Assert.Equal("CREATED", Code(pair.First));
        Assert.Equal(replace ? "CONFLICT" : "AVAILABLE", Code(pair.Second));
        var replay = await Execute(cs, bind);
        Assert.Equal("AVAILABLE", Code(replay));
        Assert.Equal(pair.First.GetProperty("binding_id").GetGuid(), replay.GetProperty("binding_id").GetGuid());
        Assert.Equal(1L, await Count(cs, "capture_execution_bindings"));
        Assert.Equal(1L, await Count(cs, "capture_capabilities"));
        Assert.Equal(2L, await Count(cs, "capture_capability_operations"));
        Assert.Equal(2L, await Count(cs, "capture_capability_events"));
    }

    private static string Authorize(Guid operation) => $"tagekyc.capture_runtime_authorize_rotation('{Actor}','{operation}','{Agent}','{Installation}','{Credential}',1,2,now()+interval '5 minutes',{Digest},now())";
    private static string Complete(Guid rotation, Guid operation, Guid candidate) => $"tagekyc.capture_runtime_complete_rotation('{rotation}','{Credential}',1,'{candidate}','{operation}',decode(repeat('05',91),'hex'),decode(repeat('52',32),'hex'),decode(repeat('53',64),'hex'),{Digest},now())";
    private static string Code(JsonElement value) => value.GetProperty("result_code").GetString()!;

    private static async Task<(JsonElement First, JsonElement Second)> BlockedRace(string cs, string first, string second, bool secondMayThrow = false)
    {
        await using var a = new NpgsqlConnection(cs); await a.OpenAsync();
        await using var b = new NpgsqlConnection(cs); await b.OpenAsync();
        await using var observer = new NpgsqlConnection(cs); await observer.OpenAsync();
        await using var ta = await a.BeginTransactionAsync();
        var winner = await Query(a, ta, first);
        var loserTask = RunLoser();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        var blocked = false;
        while (!loserTask.IsCompleted && DateTime.UtcNow < deadline)
        {
            await using var probe = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE pid=@pid AND wait_event_type='Lock')", observer);
            probe.Parameters.AddWithValue("pid", b.ProcessID);
            if ((bool)(await probe.ExecuteScalarAsync())!) { blocked = true; break; }
            await Task.Delay(20);
        }
        // This assertion makes the race non-vacuous: the loser must actually
        // overlap an uncommitted winner, not merely execute after it.
        Assert.True(blocked, "The competing transition did not reach the shared serialization boundary.");
        await ta.CommitAsync();
        return (winner, await loserTask.WaitAsync(TimeSpan.FromSeconds(10)));

        async Task<JsonElement> RunLoser()
        {
            await using var tb = await b.BeginTransactionAsync();
            try { var value = await Query(b, tb, second); await tb.CommitAsync(); return value; }
            catch (PostgresException error) when (secondMayThrow)
            {
                await tb.RollbackAsync();
                return JsonSerializer.SerializeToElement(new { result_code = error.MessageText });
            }
        }
    }
    private static async Task<JsonElement> Execute(string cs, string call)
    {
        await using var c = new NpgsqlConnection(cs); await c.OpenAsync();
        await using var t = await c.BeginTransactionAsync();
        var value = await Query(c, t, call); await t.CommitAsync(); return value;
    }
    private static async Task<JsonElement> Query(NpgsqlConnection c, NpgsqlTransaction t, string call)
    {
        await using var command = new NpgsqlCommand("SELECT to_jsonb(r)::text FROM " + call + " r", c, t) { CommandTimeout = 15 };
        using var json = JsonDocument.Parse((string)(await command.ExecuteScalarAsync())!);
        return json.RootElement.Clone();
    }
    private static Task<long> Count(string cs, string table) => Scalar(cs, "SELECT count(*) FROM tagekyc." + table);
    private static async Task<long> Scalar(string cs, string sql)
    {
        await using var c = new NpgsqlConnection(cs); await c.OpenAsync();
        await using var command = new NpgsqlCommand(sql, c);
        return (long)(await command.ExecuteScalarAsync())!;
    }
    private static async Task Seed(string cs)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln"))) directory = directory.Parent;
        var source = File.ReadAllText(Path.Combine(directory!.FullName, "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a1_postgresql16_runtime_proof_companion.md"));
        var start = source.IndexOf("INSERT INTO tagekyc.platform_operator_credentials", StringComparison.Ordinal);
        var end = source.IndexOf("-- Publish newer heads", start, StringComparison.Ordinal);
        await using var c = new NpgsqlConnection(cs); await c.OpenAsync();
        await using var t = await c.BeginTransactionAsync();
        await using var command = new NpgsqlCommand(source[start..end].Replace("ARRAY['Configuration']", "ARRAY['Bind']", StringComparison.Ordinal) + $"UPDATE tagekyc.capture_runtime_registrations SET \"NextRolePolicyId\"='{Role}',\"NextRolePolicyRevision\"=1 WHERE \"CaptureAgentId\"='{Agent}';", c, t);
        await command.ExecuteNonQueryAsync(); await t.CommitAsync();
    }
}
