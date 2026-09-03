using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1B2BetaExistingCandidateTests(
    PostgresPersistenceFixture postgres) : IAsyncLifetime
{
    private const string ChallengeHash =
        "sha256:c1b2-beta-session-challenge";

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task BETA_exact_existing_match_and_digest_only_conflict_create_no_second_source()
    {
        var fixture = await SeedCandidateAsync();
        await using var provider = CreateProvider();
        var sourceId = await CompleteNewAsync(provider, fixture.Command);

        var exact = await BeginExistingAsync(fixture, Guid.NewGuid());
        await using (var scope = provider.CreateAsyncScope())
        {
            var result = await scope.ServiceProvider
                .GetRequiredService<IRawExportSourceClaimComparisonBroker>()
                .CompleteExistingCandidateAsync(exact, CancellationToken.None);
            Assert.Equal(RawExportSourceClaimComparisonOutcome.ExistingMatch, result.Outcome);
            Assert.Equal(sourceId, result.SourceArtifactId);
        }
        Assert.Equal(("Bound", "Active"), await ReadAliasStateAsync(exact.IngressIdempotencyKey));

        var changedDigest = exact with
        {
            ClaimedPlaintextDigest = Convert.FromHexString(
                "0102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20"),
        };
        await using (var scope = provider.CreateAsyncScope())
        {
            var result = await scope.ServiceProvider
                .GetRequiredService<IRawExportSourceClaimComparisonBroker>()
                .CompleteExistingCandidateAsync(changedDigest, CancellationToken.None);
            Assert.Equal(RawExportSourceClaimComparisonOutcome.FingerprintConflict, result.Outcome);
            Assert.Null(result.SourceArtifactId);
        }
        Assert.Equal(("Bound", "Conflict"), await ReadAliasStateAsync(exact.IngressIdempotencyKey));

        var conflict = await BeginExistingAsync(fixture, Guid.NewGuid());
        await using (var scope = provider.CreateAsyncScope())
        {
            var result = await scope.ServiceProvider
                .GetRequiredService<IRawExportSourceClaimComparisonBroker>()
                .CompleteExistingCandidateAsync(
                    conflict with { ClaimedPlaintextDigest = changedDigest.ClaimedPlaintextDigest },
                    CancellationToken.None);
            Assert.Equal(RawExportSourceClaimComparisonOutcome.FingerprintConflict, result.Outcome);
            Assert.Null(result.SourceArtifactId);
        }
        Assert.Equal(
            ("ConflictTombstone", "Conflict"),
            await ReadAliasStateAsync(conflict.IngressIdempotencyKey));

        await using (var scope = provider.CreateAsyncScope())
        {
            var replay = await scope.ServiceProvider
                .GetRequiredService<IRawExportSourceClaimComparisonBroker>()
                .CompleteExistingCandidateAsync(conflict, CancellationToken.None);
            Assert.Equal(RawExportSourceClaimComparisonOutcome.ClaimTokenInvalid, replay.Outcome);
        }

        await AssertOneCanonicalSourceAsync(sourceId);
    }

    [Fact]
    public async Task BETA_historic_commitment_key_unavailable_preserves_existing_graph()
    {
        var fixture = await SeedCandidateAsync();
        await using var normalProvider = CreateProvider();
        var sourceId = await CompleteNewAsync(normalProvider, fixture.Command);
        var existing = await BeginExistingAsync(fixture, Guid.NewGuid());

        await using var unavailableProvider = CreateProvider(
            new AlwaysUnavailableContentCommitmentService());
        await using var scope = unavailableProvider.CreateAsyncScope();
        var result = await scope.ServiceProvider
            .GetRequiredService<IRawExportSourceClaimComparisonBroker>()
            .CompleteExistingCandidateAsync(existing, CancellationToken.None);

        Assert.Equal(
            RawExportSourceClaimComparisonOutcome.HistoricCommitmentKeyUnavailable,
            result.Outcome);
        Assert.Null(result.SourceArtifactId);
        Assert.Equal(("Evaluating", "Active"), await ReadAliasStateAsync(existing.IngressIdempotencyKey));
        await AssertOneCanonicalSourceAsync(sourceId);
    }

    [Fact]
    public async Task BETA_fresh_authority_barrier_precedes_historic_key_unavailable()
    {
        var fixture = await SeedCandidateAsync();
        await using var normalProvider = CreateProvider();
        var sourceId = await CompleteNewAsync(normalProvider, fixture.Command);
        var existing = await BeginExistingAsync(fixture, Guid.NewGuid());
        var pausing = new PausingUnavailableContentCommitmentService();
        await using var unavailableProvider = CreateProvider(pausing);
        await using var scope = unavailableProvider.CreateAsyncScope();
        var broker = scope.ServiceProvider
            .GetRequiredService<IRawExportSourceClaimComparisonBroker>();

        var completion = broker.CompleteExistingCandidateAsync(
            existing,
            CancellationToken.None);
        await pausing.Started.WaitAsync(TimeSpan.FromSeconds(10));
        await WithdrawConsentAsync(fixture);
        pausing.Release();
        var result = await completion;

        Assert.Equal(
            RawExportSourceClaimComparisonOutcome.SourceRetentionNotAuthorized,
            result.Outcome);
        Assert.NotEqual(
            RawExportSourceClaimComparisonOutcome.HistoricCommitmentKeyUnavailable,
            result.Outcome);
        Assert.Null(result.SourceArtifactId);
        Assert.Equal(("Evaluating", "Active"), await ReadAliasStateAsync(existing.IngressIdempotencyKey));
        await AssertOneCanonicalSourceAsync(sourceId);
    }

    [Fact]
    public async Task BETA_alternate_alias_requires_all_immutable_identity_fields_to_match()
    {
        var fixture = await SeedCandidateAsync();
        await using var provider = CreateProvider();
        await CompleteNewAsync(provider, fixture.Command);

        var exact = await BeginExistingAsync(fixture, Guid.NewGuid());
        Assert.Equal(
            "ExistingClaimComparisonToken",
            exact.Token.Variant);

        var fields = new[]
        {
            "ClientApplicationId",
            "AuthenticatedPrincipalId",
            "ProducerId",
            "CaptureAgentInstanceId",
            "VerificationSessionId",
            "CaptureAcceptanceId",
            "CaptureArtifactId",
            "CaptureRevision",
            "RawClass",
            "SessionChallengeHash",
            "AuthoritySnapshotId",
        };

        foreach (var field in fields)
        {
            var variant = await ProbeImmutableMismatchAsync(fixture, field);
            Assert.NotEqual("ExistingClaimComparisonToken", variant);
        }
    }

    private async Task<Guid> CompleteNewAsync(
        ServiceProvider provider,
        RawExportSourceClaimComparisonCommand command)
    {
        await using var scope = provider.CreateAsyncScope();
        var result = await scope.ServiceProvider
            .GetRequiredService<IRawExportSourceClaimComparisonBroker>()
            .CompleteNewCandidateAsync(command, CancellationToken.None);
        Assert.Equal(RawExportSourceClaimComparisonOutcome.NewReservation, result.Outcome);
        return Assert.IsType<Guid>(result.SourceArtifactId);
    }

    private async Task<RawExportSourceClaimComparisonCommand> BeginExistingAsync(
        CandidateFixture fixture,
        Guid ingressKey)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, fixture.Command.ActorPrincipalId);
        await using var begin = new NpgsqlCommand(
            """
            SELECT *
            FROM tagekyc.begin_raw_export_source_ingress_claim(
                @actor,@client,@producer,@agent,@ingress,@session,@acceptance,
                @artifact,1,'LiveSelfieImage',@challenge,@authority,
                12345,'image/jpeg',@captured,@retentionStart,@retentionExpires,
                1800,'fixture-content-commitment',1,@owner,300,100);
            """,
            connection,
            transaction);
        begin.Parameters.AddWithValue("actor", fixture.Command.ActorPrincipalId);
        begin.Parameters.AddWithValue("client", fixture.Command.ClientApplicationId);
        begin.Parameters.AddWithValue("producer", fixture.Command.ProducerId);
        begin.Parameters.AddWithValue("agent", fixture.Command.CaptureAgentInstanceId);
        begin.Parameters.AddWithValue("ingress", ingressKey.ToString("N"));
        begin.Parameters.AddWithValue("session", fixture.SessionId);
        begin.Parameters.AddWithValue("acceptance", fixture.AcceptanceId);
        begin.Parameters.AddWithValue("artifact", fixture.ArtifactId);
        begin.Parameters.AddWithValue("challenge", ChallengeHash);
        begin.Parameters.AddWithValue("authority", fixture.AuthoritySnapshotId.ToString("D"));
        begin.Parameters.AddWithValue("captured", fixture.Command.CapturedAtUtc);
        begin.Parameters.AddWithValue(
            "retentionStart",
            fixture.Command.PlaintextRetentionStartedAtUtc);
        begin.Parameters.AddWithValue(
            "retentionExpires",
            fixture.Command.PlaintextRetentionExpiresAtUtc);
        begin.Parameters.AddWithValue("owner", Guid.NewGuid());
        await using var reader = await begin.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.True(reader.IsDBNull(0));
        Assert.Equal("ExistingClaimComparisonToken", reader.GetString(2));
        var token = new RawExportClaimEvaluationToken(
            reader.GetGuid(4),
            reader.GetInt64(5),
            reader.GetInt64(6),
            reader.GetString(2),
            reader.GetFieldValue<DateTimeOffset>(3),
            reader.GetString(1));
        await reader.CloseAsync();
        await transaction.CommitAsync();
        return fixture.Command with
        {
            IngressIdempotencyKey = ingressKey,
            Token = token,
        };
    }

    private async Task<string?> ProbeImmutableMismatchAsync(
        CandidateFixture fixture,
        string field)
    {
        var actor = field == "AuthenticatedPrincipalId"
            ? Guid.NewGuid()
            : fixture.Command.ActorPrincipalId;
        var client = field == "ClientApplicationId"
            ? Guid.NewGuid()
            : fixture.Command.ClientApplicationId;
        var producer = field == "ProducerId"
            ? Guid.NewGuid().ToString("N")
            : fixture.Command.ProducerId;
        var agent = field == "CaptureAgentInstanceId"
            ? "capture-agent-beta-other"
            : fixture.Command.CaptureAgentInstanceId;
        var session = field == "VerificationSessionId"
            ? Guid.NewGuid()
            : fixture.SessionId;
        var acceptance = field == "CaptureAcceptanceId"
            ? Guid.NewGuid()
            : fixture.AcceptanceId;
        var artifact = field == "CaptureArtifactId"
            ? Guid.NewGuid()
            : fixture.ArtifactId;
        var revision = field == "CaptureRevision" ? 2 : 1;
        var rawClass = field == "RawClass"
            ? "ChipDg2Portrait"
            : "LiveSelfieImage";
        var challenge = field == "SessionChallengeHash"
            ? ChallengeHash + "-other"
            : ChallengeHash;
        var authority = field == "AuthoritySnapshotId"
            ? Guid.NewGuid().ToString("D")
            : fixture.AuthoritySnapshotId.ToString("D");

        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(
            connection,
            transaction,
            fixture.Command.ActorPrincipalId);
        await using var begin = new NpgsqlCommand(
            """
            SELECT *
            FROM tagekyc.begin_raw_export_source_ingress_claim(
                @actor,@client,@producer,@agent,@ingress,@session,@acceptance,
                @artifact,@revision,@rawClass,@challenge,@authority,
                12345,'image/jpeg',@captured,@retentionStart,@retentionExpires,
                1800,'fixture-content-commitment',1,@owner,300,100);
            """,
            connection,
            transaction);
        begin.Parameters.AddWithValue("actor", actor);
        begin.Parameters.AddWithValue("client", client);
        begin.Parameters.AddWithValue("producer", producer);
        begin.Parameters.AddWithValue("agent", agent);
        begin.Parameters.AddWithValue("ingress", Guid.NewGuid().ToString("N"));
        begin.Parameters.AddWithValue("session", session);
        begin.Parameters.AddWithValue("acceptance", acceptance);
        begin.Parameters.AddWithValue("artifact", artifact);
        begin.Parameters.AddWithValue("revision", revision);
        begin.Parameters.AddWithValue("rawClass", rawClass);
        begin.Parameters.AddWithValue("challenge", challenge);
        begin.Parameters.AddWithValue("authority", authority);
        begin.Parameters.AddWithValue("captured", fixture.Command.CapturedAtUtc);
        begin.Parameters.AddWithValue(
            "retentionStart",
            fixture.Command.PlaintextRetentionStartedAtUtc);
        begin.Parameters.AddWithValue(
            "retentionExpires",
            fixture.Command.PlaintextRetentionExpiresAtUtc);
        begin.Parameters.AddWithValue("owner", Guid.NewGuid());

        try
        {
            await using var reader = await begin.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            var variant = reader.IsDBNull(2) ? null : reader.GetString(2);
            await reader.CloseAsync();
            await transaction.RollbackAsync();
            return variant;
        }
        catch (PostgresException)
        {
            await transaction.RollbackAsync();
            return null;
        }
    }

    private async Task<CandidateFixture> SeedCandidateAsync()
    {
        var actor = Guid.NewGuid();
        var client = Guid.NewGuid();
        var session = Guid.NewGuid();
        var artifact = Guid.NewGuid();
        var policy = await SeedConsentPolicyAsync();
        var now = DateTimeOffset.UtcNow;
        await using (var db = postgres.CreateDbContext())
        {
            db.Sessions.Add(new VerificationSessionRow
            {
                Id = session,
                ClientApplicationId = client,
                SubjectRef = $"subject:{Guid.NewGuid():N}",
                Profile = "ChallengeBoundEkycProfile",
                Purpose = "raw-export",
                RequiredChecksJson = "[\"LiveSelfie\"]",
                BindingNonceHash = ChallengeHash,
                RequestId = Guid.NewGuid().ToString("N"),
                CorrelationId = Guid.NewGuid().ToString("N"),
                State = "Completed",
                Result = "Passed",
                AssuranceLevel = "Substantial",
                PolicySnapshotId = "c1b2-beta-policy",
                RetentionClass = "Standard",
                DeletionEligibility = "Pending",
                LegalHoldStatus = "None",
                PurgeBlockReason = "None",
                ExpiresAt = now.AddHours(2),
                CreatedAt = now,
                CompletedAt = now,
            });
            db.CaptureArtifacts.Add(new CaptureArtifactRow
            {
                Id = artifact,
                VerificationSessionId = session,
                ArtifactType = "SelfieImage",
                CaptureSource = "MobileSdk",
                ArtifactHash = $"sha256:{new string('a', 64)}",
                MetadataHash = $"sha256:{new string('b', 64)}",
                QualityState = "Accepted",
                RequestId = Guid.NewGuid().ToString("N"),
                CorrelationId = Guid.NewGuid().ToString("N"),
                CreatedAt = now,
                ExpiresAt = now.AddHours(2),
            });
            await db.SaveChangesAsync();
        }

        var acceptance = await AppendAcceptanceAsync(actor, session, client, artifact);
        await GrantConsentAsync(actor, client, session, policy);
        var authority = await AppendAuthorityAsync(actor, client, session, acceptance, policy);
        var command = await BeginNewAsync(
            actor,
            client,
            session,
            acceptance,
            artifact,
            authority,
            now);
        return new CandidateFixture(policy, session, acceptance, artifact, authority, command);
    }

    private async Task<RawExportSourceClaimComparisonCommand> BeginNewAsync(
        Guid actor,
        Guid client,
        Guid session,
        Guid acceptance,
        Guid artifact,
        Guid authority,
        DateTimeOffset now)
    {
        var captured = now.AddSeconds(-5);
        var retentionStart = now.AddSeconds(-4);
        var retentionExpires = now.AddMinutes(30);
        var ingressKey = Guid.NewGuid();
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor);
        await using var begin = new NpgsqlCommand(
            """
            SELECT * FROM tagekyc.begin_raw_export_source_ingress_claim(
                @actor,@client,@producer,@agent,@ingress,@session,@acceptance,
                @artifact,1,'LiveSelfieImage',@challenge,@authority,
                12345,'image/jpeg',@captured,@retentionStart,@retentionExpires,
                1800,'fixture-content-commitment',1,@owner,300,100);
            """,
            connection,
            transaction);
        begin.Parameters.AddWithValue("actor", actor);
        begin.Parameters.AddWithValue("client", client);
        begin.Parameters.AddWithValue("producer", actor.ToString("N"));
        begin.Parameters.AddWithValue("agent", "capture-agent-beta");
        begin.Parameters.AddWithValue("ingress", ingressKey.ToString("N"));
        begin.Parameters.AddWithValue("session", session);
        begin.Parameters.AddWithValue("acceptance", acceptance);
        begin.Parameters.AddWithValue("artifact", artifact);
        begin.Parameters.AddWithValue("challenge", ChallengeHash);
        begin.Parameters.AddWithValue("authority", authority.ToString("D"));
        begin.Parameters.AddWithValue("captured", captured);
        begin.Parameters.AddWithValue("retentionStart", retentionStart);
        begin.Parameters.AddWithValue("retentionExpires", retentionExpires);
        begin.Parameters.AddWithValue("owner", Guid.NewGuid());
        await using var reader = await begin.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var token = new RawExportClaimEvaluationToken(
            reader.GetGuid(4),reader.GetInt64(5),reader.GetInt64(6),
            reader.GetString(2),reader.GetFieldValue<DateTimeOffset>(3),reader.GetString(1));
        await reader.CloseAsync();
        await transaction.CommitAsync();
        return new RawExportSourceClaimComparisonCommand(
            actor,client,actor.ToString("N"),"capture-agent-beta",ingressKey,token,
            12345,"image/jpeg",captured,retentionStart,retentionExpires,1800,
            Convert.FromHexString(
                "E0F1E2F3E4F5E6F7E8F9EAEBECEDEEEFF0F1F2F3F4F5F6F7F8F9FAFBFCFDFEFF"));
    }

    private async Task<Guid> SeedConsentPolicyAsync()
    {
        var policyId = Guid.NewGuid();
        await using var db = postgres.CreateDbContext();
        var policy = await new EfRawExportPolicyRepository(db).AddVersionAsync(
            new AddRawExportPolicyVersionCommand(
                policyId,0,RawExportMode.EncryptedRawVaultRetained,
                "SubjectRawBiometricExport","fixture-c1-retained-v1",
                "SubjectRawBiometricExport",RawExportConsentRequirement.Required,
                null,null,"Controller","controller:fixture","VN","VN","VN",
                null,null,
                new HashSet<RawExportRawClass>
                {
                    RawExportRawClass.ChipDg2Portrait,
                    RawExportRawClass.LiveSelfieImage,
                },
                300));
        Assert.Equal(1, policy.RequirementRuleSetVersion);
        return policyId;
    }

    private async Task<Guid> AppendAcceptanceAsync(
        Guid actor,
        Guid session,
        Guid client,
        Guid artifact)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor);
        await using var command = new NpgsqlCommand(
            """
            SELECT tagekyc.raw_export_append_capture_acceptance(
                @session,@client,'LiveSelfieImage',@artifact,1,@challenge,
                'evidence:c1b2-beta','acceptance-policy:c1b2-beta',1);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("session", session);
        command.Parameters.AddWithValue("client", client);
        command.Parameters.AddWithValue("artifact", artifact);
        command.Parameters.AddWithValue("challenge", ChallengeHash);
        var id = (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);
        await transaction.CommitAsync();
        return id;
    }

    private async Task GrantConsentAsync(
        Guid actor,
        Guid client,
        Guid session,
        Guid policy)
    {
        var admin = Guid.NewGuid();
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteAsync(
            connection,transaction,
            "SELECT tagekyc.raw_export_bootstrap_global_authority(@admin,'RecorderAuthorityAdmin','decision:c1b2-beta-bootstrap');",
            new NpgsqlParameter("admin", admin));
        await SetActorAsync(connection, transaction, admin);
        await ExecuteAsync(
            connection,transaction,
            "SELECT tagekyc.raw_export_append_subject_consent_authority(@actor,@client,'SubjectConsentRecorder',0,'Granted',NULL,'decision:c1b2-beta-recorder',@until);",
            new NpgsqlParameter("actor", actor),new NpgsqlParameter("client", client),
            new NpgsqlParameter("until", DateTimeOffset.UtcNow.AddHours(1)));
        await ExecuteAsync(
            connection,transaction,
            "SELECT tagekyc.raw_export_append_subject_consent_authority(@actor,@client,'SubjectConsentWithdrawer',0,'Granted',NULL,'decision:c1b2-beta-withdrawer',@until);",
            new NpgsqlParameter("actor", actor),new NpgsqlParameter("client", client),
            new NpgsqlParameter("until", DateTimeOffset.UtcNow.AddHours(1)));
        await SetActorAsync(connection, transaction, actor);
        await ExecuteAsync(
            connection,transaction,
            "SELECT tagekyc.raw_export_append_subject_consent_granted(@session,@policy,1,ARRAY['LiveSelfieImage']::text[],'consent-text-v1','sha256:consent-text','artifact:consent-c1b2-beta','decision:c1b2-beta',@until);",
            new NpgsqlParameter("session", session),new NpgsqlParameter("policy", policy),
            new NpgsqlParameter("until", DateTimeOffset.UtcNow.AddHours(1)));
        await transaction.CommitAsync();
    }

    private async Task WithdrawConsentAsync(CandidateFixture fixture)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, fixture.Command.ActorPrincipalId);
        await ExecuteAsync(
            connection,transaction,
            "SELECT tagekyc.raw_export_append_subject_consent_withdrawn(@session,@policy,1,1,1,'decision:c1b2-beta-withdraw',NULL);",
            new NpgsqlParameter("session", fixture.SessionId),
            new NpgsqlParameter("policy", fixture.PolicyId));
        await transaction.CommitAsync();
    }

    private async Task<Guid> AppendAuthorityAsync(
        Guid actor,
        Guid client,
        Guid session,
        Guid acceptance,
        Guid policy)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor);
        await using var command = new NpgsqlCommand(
            """
            SELECT "AuthoritySnapshotId"
            FROM tagekyc.raw_export_append_authority_snapshot(
                @client,@session,@acceptance,'LiveSelfieImage',@artifact,1,
                'controller:fixture','scope:fixture','retention-policy:fixture',1,
                @policy,1,'RawBiometric','CaptureAccepted',@sourceExpires,
                'revocation-policy:fixture','purge-policy:fixture',
                'legal-hold-policy:fixture',@evaluated,@validUntil);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("client", client);
        command.Parameters.AddWithValue("session", session);
        command.Parameters.AddWithValue("acceptance", acceptance);
        command.Parameters.AddWithValue("artifact", Guid.NewGuid());
        command.Parameters.AddWithValue("policy", policy);
        command.Parameters.AddWithValue("sourceExpires", DateTimeOffset.UtcNow.AddHours(1));
        command.Parameters.AddWithValue("evaluated", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("validUntil", DateTimeOffset.UtcNow.AddMinutes(45));
        var id = (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);
        await transaction.CommitAsync();
        return id;
    }

    private async Task<(string State, string Disposition)> ReadAliasStateAsync(Guid ingressKey)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT \"AliasState\",\"CurrentClaimEvaluationDisposition\" FROM tagekyc.raw_export_source_ingress_claim_aliases WHERE \"IngressIdempotencyKey\"=@key;",
            connection);
        command.Parameters.AddWithValue("key", ingressKey);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetString(0), reader.GetString(1));
    }

    private async Task AssertOneCanonicalSourceAsync(Guid sourceId)
    {
        await using var db = postgres.CreateDbContext();
        Assert.Equal(1, db.RawExportSourceReservations.Count());
        Assert.Equal(1, db.RawExportSourceEncryptionAttempts.Count());
        Assert.Equal(1, db.RawExportSourceHeads.Count());
        Assert.NotNull(await db.RawExportSourceReservations.FindAsync(sourceId));
    }

    private ServiceProvider CreateProvider(IContentCommitmentService? commitment = null)
    {
        var configuration = new ConfigurationManager
        {
            ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] =
                "0123456789abcdef0123456789abcdef",
            ["TagEkyc:RawExport:SubjectRefToken:FixtureKeys:fixture-subject-ref-token:1"] =
                "abcdef0123456789abcdef0123456789",
            ["TagEkyc:RawExport:CustodyProfile:Profile"] = "Fixture",
            ["RawExportSourceClaimSafetyMarginMilliseconds"] = "1000",
            ["RawExportSourceMaximumRemainingContinuationWindowSeconds"] = "1800",
            ["RawExportSourceEncryptionAttemptDeadlineSeconds"] = "30",
            ["RawExportSourceOwnershipLeaseDurationSeconds"] = "300",
        };
        var services = new ServiceCollection();
        services.AddScoped(_ => postgres.CreateDbContext());
        if (commitment is not null)
        {
            services.AddSingleton(commitment);
        }
        services.AddTagEkycRawExportSourceClaimComparison(configuration);
        return services.BuildServiceProvider();
    }

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task SetActorAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid actor) =>
        await ExecuteAsync(
            connection,transaction,
            "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true);",
            new NpgsqlParameter("actor", actor.ToString("D")));

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private sealed record CandidateFixture(
        Guid PolicyId,
        Guid SessionId,
        Guid AcceptanceId,
        Guid ArtifactId,
        Guid AuthoritySnapshotId,
        RawExportSourceClaimComparisonCommand Command);

    private sealed class AlwaysUnavailableContentCommitmentService :
        IContentCommitmentService
    {
        public ValueTask<ContentCommitmentResult> ComputeAsync(
            CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(
                ContentCommitmentResult.Failed(
                    ContentCommitmentFailure.ProviderFailure));
    }

    private sealed class PausingUnavailableContentCommitmentService :
        IContentCommitmentService
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public void Release() => _release.TrySetResult();

        public async ValueTask<ContentCommitmentResult> ComputeAsync(
            CommitmentKeySelector selector,
            ReadOnlyMemory<byte> lpPayload,
            CancellationToken cancellationToken)
        {
            _started.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return ContentCommitmentResult.Failed(
                ContentCommitmentFailure.ProviderFailure);
        }
    }
}
