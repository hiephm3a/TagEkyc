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
public sealed class Tip88C1B2CoreTests(PostgresPersistenceFixture postgres)
    : IAsyncLifetime
{
    private const string ChallengeHash =
        "sha256:c1b2-core-session-challenge";

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task C1B2CORE_new_candidate_commits_full_recovery_context_atomically()
    {
        var fixture = await SeedCandidateAsync();
        var command = fixture.Command;
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var broker =
            scope.ServiceProvider
                .GetRequiredService<IRawExportSourceClaimComparisonBroker>();

        var result = await broker.CompleteNewCandidateAsync(
            command,
            CancellationToken.None);

        Assert.Equal(
            RawExportSourceClaimComparisonOutcome.NewReservation,
            result.Outcome);
        Assert.NotNull(result.SourceArtifactId);

        await using var db = postgres.CreateDbContext();
        var reservation = await db.RawExportSourceReservations
            .FindAsync(result.SourceArtifactId!.Value);
        Assert.NotNull(reservation);
        Assert.Equal(fixture.PolicyId, reservation.ConsentPolicyId);
        Assert.Equal(1, reservation.ConsentPolicyVersion);
        Assert.Equal("controller:fixture", reservation.ControllerIdentity);
        Assert.Equal("scope:fixture", reservation.StableDataScopeId);
        Assert.Equal(command.ClaimedPlaintextLength, reservation.ClaimedPlaintextLength);
        Assert.Equal(command.MediaType, reservation.MediaType);
        Assert.Equal(
            C1HashCanonical.CanonicalTimestamp(command.CapturedAtUtc),
            C1HashCanonical.CanonicalTimestamp(reservation.CapturedAtUtc));
        Assert.Equal(
            C1HashCanonical.CanonicalTimestamp(
                command.PlaintextRetentionStartedAtUtc),
            C1HashCanonical.CanonicalTimestamp(
                reservation.PlaintextRetentionStartedAtUtc));
        Assert.Equal(
            C1HashCanonical.CanonicalTimestamp(
                command.PlaintextRetentionExpiresAtUtc),
            C1HashCanonical.CanonicalTimestamp(
                reservation.PlaintextRetentionExpiresAtUtc));
        Assert.Equal(
            command.PlaintextRetentionBudgetSeconds,
            reservation.PlaintextRetentionBudgetSeconds);
        Assert.Equal(32, reservation.ContentCommitment.Length);
        Assert.Equal(32, reservation.SubjectRefToken.Length);
        Assert.Equal(32, reservation.AdmissionFingerprint.Length);
        Assert.Equal(32, reservation.SourceReservationFingerprint.Length);

        var claim = (await db.RawExportSourceIngressClaims.FindAsync(
            fixture.IngressClaimId))!;
        var admission = C1HashCanonical.Compute(
            "tip-88c1-ingress-admission-v1",
            new C1HashCanonical.Scalar(
                Convert.ToHexString(claim.IngressIdentityFingerprint)
                    .ToLowerInvariant()),
            new C1HashCanonical.Scalar("1"),
            new C1HashCanonical.Scalar(reservation.ContentCommitmentKeyId),
            new C1HashCanonical.Scalar(
                reservation.ContentCommitmentKeyVersion.ToString()),
            new C1HashCanonical.Scalar(
                Convert.ToHexString(reservation.ContentCommitment)
                    .ToLowerInvariant()),
            new C1HashCanonical.Scalar(reservation.MediaType),
            new C1HashCanonical.Scalar(
                C1HashCanonical.CanonicalTimestamp(reservation.CapturedAtUtc)),
            new C1HashCanonical.Scalar(
                C1HashCanonical.CanonicalTimestamp(
                    reservation.PlaintextRetentionStartedAtUtc)),
            new C1HashCanonical.Scalar(
                C1HashCanonical.CanonicalTimestamp(
                    reservation.PlaintextRetentionExpiresAtUtc)),
            new C1HashCanonical.Scalar(
                reservation.PlaintextRetentionBudgetSeconds.ToString()));
        Assert.Equal(admission, reservation.AdmissionFingerprint);

        var attempt = Assert.Single(
            db.RawExportSourceEncryptionAttempts.Where(
                row => row.SourceArtifactId == result.SourceArtifactId));
        Assert.Equal("none", attempt.NonceDerivationSeedReferenceOrWrappedSeed);
        Assert.Equal(32, attempt.NonceDerivationSeedCommitment.Length);
        Assert.Equal(32, attempt.FramingParametersDigest.Length);
        Assert.Equal(32, attempt.EncryptionAttemptFingerprint.Length);
        Assert.Null(attempt.R2TerminationDisposition);

        var sourceFingerprint = C1HashCanonical.Compute(
            "tip-88c1-source-reservation-v2",
            new C1HashCanonical.Scalar(reservation.SourceArtifactId.ToString("N")),
            new C1HashCanonical.Scalar(
                Convert.ToHexString(reservation.AdmissionFingerprint)
                    .ToLowerInvariant()),
            new C1HashCanonical.Scalar(
                reservation.SubjectRefTokenSchemaVersion.ToString()),
            new C1HashCanonical.Scalar(reservation.SubjectRefTokenKeyId),
            new C1HashCanonical.Scalar(
                reservation.SubjectRefTokenKeyVersion.ToString()),
            new C1HashCanonical.Scalar(
                Convert.ToHexString(reservation.SubjectRefToken)
                    .ToLowerInvariant()),
            new C1HashCanonical.Scalar(
                reservation.AuthoritySnapshotSchemaVersion.ToString()),
            new C1HashCanonical.Scalar(
                reservation.AuthoritySnapshotId.ToString("N")),
            new C1HashCanonical.Scalar(
                C1HashCanonical.CanonicalTimestamp(
                    reservation.AbsoluteSourceExpiresAtUtc)),
            new C1HashCanonical.Scalar(reservation.StorageProfileId),
            new C1HashCanonical.Scalar(reservation.SourceEncryptionProfileId),
            new C1HashCanonical.Scalar(
                reservation.SourceEncryptionProfileVersion.ToString()));
        Assert.Equal(
            sourceFingerprint,
            reservation.SourceReservationFingerprint);

        var attemptFingerprint = C1HashCanonical.Compute(
            "tip-88c1-encryption-attempt-v1",
            new C1HashCanonical.Scalar(
                Convert.ToHexString(sourceFingerprint).ToLowerInvariant()),
            new C1HashCanonical.Scalar(
                attempt.EncryptionAttemptRevision.ToString()),
            new C1HashCanonical.Scalar(attempt.Fence.ToString()),
            new C1HashCanonical.Scalar(
                attempt.ProvisionalObjectIdentity.ToString("N")),
            new C1HashCanonical.Scalar(
                attempt.AttemptKeyReservationId.ToString("N")),
            new C1HashCanonical.Scalar(attempt.EncryptionSuiteId),
            new C1HashCanonical.Scalar(
                attempt.EncryptionFramingVersion.ToString()),
            new C1HashCanonical.Scalar(attempt.KeyProviderId),
            new C1HashCanonical.Scalar(attempt.KekId),
            new C1HashCanonical.Scalar(attempt.KekVersion.ToString()),
            new C1HashCanonical.Scalar(attempt.KekFingerprint),
            new C1HashCanonical.Scalar(attempt.NonceStrategyId),
            new C1HashCanonical.Scalar(
                Convert.ToHexString(attempt.NonceDerivationSeedCommitment)
                    .ToLowerInvariant()),
            new C1HashCanonical.Scalar(attempt.ChunkSize.ToString()),
            new C1HashCanonical.Scalar(
                Convert.ToHexString(attempt.FramingParametersDigest)
                    .ToLowerInvariant()));
        Assert.Equal(
            attemptFingerprint,
            attempt.EncryptionAttemptFingerprint);

        var head = await db.RawExportSourceHeads.FindAsync(result.SourceArtifactId);
        Assert.NotNull(head);
        Assert.Equal("Reserved", head.CustodyState);
        Assert.Equal(attempt.AttemptId, head.CurrentEncryptionAttemptId);
        Assert.Equal("Reserved", claim.ClaimState);
    }

    [Fact]
    public async Task C1B2CORE_complete_is_broker_only_and_tables_reject_direct_dml()
    {
        await using var connection = await OpenAsync();
        var signature =
            "tagekyc.complete_raw_export_source_ingress_claim(" +
            "uuid,text,text,text,uuid,bigint,bigint,text,timestamp with time zone," +
            "text,bytea,integer,text,integer,bytea,integer,text,integer,bytea," +
            "bigint,text,timestamp with time zone,timestamp with time zone," +
            "timestamp with time zone,integer,text,text,integer,text,integer,text," +
            "bytea,integer,bytea,text,text,integer,text,integer,integer,integer,integer)";
        Assert.False(await HasFunctionPrivilegeAsync(
            connection,
            "tagekyc_runtime",
            signature));
        Assert.True(await HasFunctionPrivilegeAsync(
            connection,
            "tagekyc_raw_export_claim_broker",
            signature));

        foreach (var table in new[]
                 {
                     "raw_export_source_reservations",
                     "raw_export_source_encryption_attempts",
                     "raw_export_source_head",
                 })
        {
            Assert.False(await HasTablePrivilegeAsync(
                connection,
                "tagekyc_runtime",
                $"tagekyc.{table}",
                "SELECT"));
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => ExecuteAsync(
                    connection,
                    $"INSERT INTO tagekyc.{table} DEFAULT VALUES;"));
            Assert.True(
                exception.SqlState is PostgresErrorCodes.RaiseException
                    or PostgresErrorCodes.NotNullViolation
                    or PostgresErrorCodes.InsufficientPrivilege,
                $"{table}: {exception.SqlState} {exception.MessageText}");
        }
    }

    [Fact]
    public async Task C1B2CORE_identifiers_round_trip_exactly_from_catalog()
    {
        string[] tables =
        [
            "raw_export_source_reservations",
            "raw_export_source_encryption_attempts",
            "raw_export_source_head",
        ];
        string[] constraints =
        [
            "pk_raw_export_source_reservations",
            "uq_raw_export_source_ingress_source",
            "ck_raw_export_source_reservation_values",
            "fk_raw_export_source_reservation_claim",
            "fk_raw_export_source_reservation_consent_policy",
            "pk_raw_export_source_encryption_attempts",
            "uq_raw_export_source_attempt_fence",
            "uq_raw_export_source_attempt_revision",
            "ck_raw_export_source_attempt_values",
            "fk_raw_export_source_attempt_reservation",
            "pk_raw_export_source_head",
            "ck_raw_export_source_head_values",
            "fk_raw_export_source_head_attempt",
            "fk_raw_export_source_head_reservation",
            "fk_raw_export_authority_snapshot_consent_policy",
        ];
        string[] functions =
        [
            "enforce_raw_export_source_core_write",
            "enforce_raw_export_source_head_write",
            "raw_export_c1_hash_canonical",
            "complete_raw_export_source_ingress_claim",
        ];
        string[] triggers =
        [
            "tr_raw_export_source_reservations_guard",
            "tr_raw_export_source_attempts_guard",
            "tr_raw_export_source_head_guard",
        ];

        await using var connection = await OpenAsync();
        Assert.Equal(
            tables.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(
                connection,
                """
                SELECT relation.relname
                FROM pg_catalog.pg_class relation
                JOIN pg_catalog.pg_namespace namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = ANY(@names)
                  AND relation.relkind = 'r';
                """,
                new NpgsqlParameter("names", tables)))
            .Order(StringComparer.Ordinal));
        Assert.Equal(
            constraints.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(
                connection,
                """
                SELECT constraint_row.conname
                FROM pg_catalog.pg_constraint constraint_row
                JOIN pg_catalog.pg_namespace namespace
                  ON namespace.oid = constraint_row.connamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND constraint_row.conname = ANY(@names);
                """,
                new NpgsqlParameter("names", constraints)))
            .Order(StringComparer.Ordinal));
        Assert.Equal(
            functions.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(
                connection,
                """
                SELECT procedure.proname
                FROM pg_catalog.pg_proc procedure
                JOIN pg_catalog.pg_namespace namespace
                  ON namespace.oid = procedure.pronamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND procedure.proname = ANY(@names);
                """,
                new NpgsqlParameter("names", functions)))
            .Order(StringComparer.Ordinal));
        Assert.Equal(
            triggers.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(
                connection,
                """
                SELECT trigger_row.tgname
                FROM pg_catalog.pg_trigger trigger_row
                JOIN pg_catalog.pg_class relation
                  ON relation.oid = trigger_row.tgrelid
                JOIN pg_catalog.pg_namespace namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND NOT trigger_row.tgisinternal
                  AND trigger_row.tgname = ANY(@names);
                """,
                new NpgsqlParameter("names", triggers)))
            .Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task C1B2CORE_envelope_mismatch_is_token_invalid_with_zero_residue()
    {
        var fixture = await SeedCandidateAsync();
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var broker =
            scope.ServiceProvider
                .GetRequiredService<IRawExportSourceClaimComparisonBroker>();

        var result = await broker.CompleteNewCandidateAsync(
            fixture.Command with { MediaType = "image/png" },
            CancellationToken.None);

        Assert.Equal(
            RawExportSourceClaimComparisonOutcome.ClaimTokenInvalid,
            result.Outcome);
        await using var db = postgres.CreateDbContext();
        Assert.DoesNotContain(
            db.RawExportSourceReservations,
            row => row.IngressClaimId == fixture.IngressClaimId);
    }

    [Fact]
    public async Task C1B2CORE_effective_cap_completes_evaluation_without_r1_rows()
    {
        var fixture = await SeedCandidateAsync(TimeSpan.FromSeconds(10));
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var broker =
            scope.ServiceProvider
                .GetRequiredService<IRawExportSourceClaimComparisonBroker>();

        var result = await broker.CompleteNewCandidateAsync(
            fixture.Command,
            CancellationToken.None);

        Assert.Equal(
            RawExportSourceClaimComparisonOutcome.PlaintextRetentionInvalid,
            result.Outcome);
        await using var db = postgres.CreateDbContext();
        Assert.DoesNotContain(
            db.RawExportSourceReservations,
            row => row.IngressClaimId == fixture.IngressClaimId);
        var alias = Assert.Single(
            db.RawExportSourceIngressClaimAliases.Where(
                row => row.IngressClaimId == fixture.IngressClaimId));
        Assert.Equal("Evaluating", alias.AliasState);
        Assert.Equal("Completed", alias.CurrentClaimEvaluationDisposition);
        Assert.Equal(
            "ClaimEvaluating",
            (await db.RawExportSourceIngressClaims.FindAsync(
                fixture.IngressClaimId))!.ClaimState);
    }

    [Fact]
    public async Task C1B2CORE_reserved_claim_requires_a_source_reservation()
    {
        var fixture = await SeedCandidateAsync();
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var exception = await Assert.ThrowsAsync<PostgresException>(
            async () =>
            {
                await ExecuteAsync(
                    connection,
                    transaction,
                    """
                    SET LOCAL ROLE tagekyc_raw_export_deployer;
                    SELECT pg_catalog.set_config(
                        'tagekyc.raw_export_source_ingress_write_context',
                        'claim:UPDATE',
                        true);
                    UPDATE tagekyc.raw_export_source_ingress_claims
                    SET "ClaimState" = 'Reserved'
                    WHERE "IngressClaimId" = @claim;
                    """,
                    new NpgsqlParameter("claim", fixture.IngressClaimId));
                await transaction.CommitAsync();
            });
        Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
        Assert.Equal(
            "RAW_EXPORT_SOURCE_RESERVATION_REQUIRED",
            exception.MessageText);
    }

    [Fact]
    public async Task C1B2CORE_source_reservation_is_append_only_even_as_owner()
    {
        var fixture = await SeedCandidateAsync();
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var result = await scope.ServiceProvider
            .GetRequiredService<IRawExportSourceClaimComparisonBroker>()
            .CompleteNewCandidateAsync(fixture.Command, CancellationToken.None);
        Assert.Equal(
            RawExportSourceClaimComparisonOutcome.NewReservation,
            result.Outcome);

        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteAsync(
            connection,
            transaction,
            """
            SET LOCAL ROLE tagekyc_raw_export_deployer;
            SELECT pg_catalog.set_config(
                'tagekyc.raw_export_source_core_write_context',
                'complete-r1',
                true);
            """);
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => ExecuteAsync(
                connection,
                transaction,
                """
                UPDATE tagekyc.raw_export_source_reservations
                SET "MediaType" = 'image/png'
                WHERE "SourceArtifactId" = @source;
                """,
                new NpgsqlParameter("source", result.SourceArtifactId!.Value)));
        Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
        Assert.Equal(
            "RAW_EXPORT_SOURCE_CORE_APPEND_ONLY",
            exception.MessageText);
    }

    [Fact]
    public void C1B2CORE_three_fingerprint_golden_vectors_are_pinned()
    {
        const string ingressFingerprint =
            "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f";
        const string contentCommitment =
            "202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f";
        const string subjectRefToken =
            "404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f";

        var admission = C1HashCanonical.Compute(
            "tip-88c1-ingress-admission-v1",
            new C1HashCanonical.Scalar(ingressFingerprint),
            new C1HashCanonical.Scalar("1"),
            new C1HashCanonical.Scalar("fixture-content-key"),
            new C1HashCanonical.Scalar("7"),
            new C1HashCanonical.Scalar(contentCommitment),
            new C1HashCanonical.Scalar("image/jpeg"),
            new C1HashCanonical.Scalar("2026-07-31T01:02:03.123456Z"),
            new C1HashCanonical.Scalar("2026-07-31T01:02:04.234567Z"),
            new C1HashCanonical.Scalar("2026-07-31T01:32:04.234567Z"),
            new C1HashCanonical.Scalar("1800"));
        Assert.Equal(
            "54BFF92D062CCA6F9D6702D09075B473C9747A6669772D54F55345B78D6BEA62",
            Convert.ToHexString(admission));

        var source = C1HashCanonical.Compute(
            "tip-88c1-source-reservation-v2",
            new C1HashCanonical.Scalar("11111111222233334444555555555555"),
            new C1HashCanonical.Scalar(
                Convert.ToHexString(admission).ToLowerInvariant()),
            new C1HashCanonical.Scalar("1"),
            new C1HashCanonical.Scalar("fixture-subject-key"),
            new C1HashCanonical.Scalar("3"),
            new C1HashCanonical.Scalar(subjectRefToken),
            new C1HashCanonical.Scalar("1"),
            new C1HashCanonical.Scalar("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            new C1HashCanonical.Scalar("2026-07-31T02:02:03.123456Z"),
            new C1HashCanonical.Scalar("fixture-filesystem-v1"),
            new C1HashCanonical.Scalar("fixture-aes256-gcm-v1"),
            new C1HashCanonical.Scalar("1"));
        Assert.Equal(
            "1D740F22E174288D24FC17C884F750F1975D88997529717AD9D8BCDEF56CF500",
            Convert.ToHexString(source));

        var attempt = C1HashCanonical.Compute(
            "tip-88c1-encryption-attempt-v1",
            new C1HashCanonical.Scalar(
                Convert.ToHexString(source).ToLowerInvariant()),
            new C1HashCanonical.Scalar("1"),
            new C1HashCanonical.Scalar("1"),
            new C1HashCanonical.Scalar("66666666666666666666666666666666"),
            new C1HashCanonical.Scalar("77777777777777777777777777777777"),
            new C1HashCanonical.Scalar("fixture-aead-aes256gcm-v1"),
            new C1HashCanonical.Scalar("1"),
            new C1HashCanonical.Scalar("fixture-key-provider"),
            new C1HashCanonical.Scalar("fixture-kek"),
            new C1HashCanonical.Scalar("1"),
            new C1HashCanonical.Scalar("fixture-kek-fingerprint-v1"),
            new C1HashCanonical.Scalar("fixture-nonce-random96-v1"),
            new C1HashCanonical.Scalar(
                "dfa71064ba60f7e073a333f71031969ff2daa847043976d7ff8f58c40f6005ea"),
            new C1HashCanonical.Scalar("1048576"),
            new C1HashCanonical.Scalar(
                "87ab6a5c24a28c357f097a1623b787e3f34b4ee283b11de47fce53f19a4258d8"));
        Assert.Equal(
            "C53985AA6523F79BD92FD2B10EC67D13DF5BA5402E35D19EB8A7050C9EC1A431",
            Convert.ToHexString(attempt));
    }

    [Fact]
    public async Task C1B2CORE_nonce_and_framing_codecs_are_pinned_and_plaintext_digest_is_absent()
    {
        Assert.Equal(
            "DFA71064BA60F7E073A333F71031969FF2DAA847043976D7FF8F58C40F6005EA",
            Convert.ToHexString(
                C1HashCanonical.ComputeNonceSeedCommitment(
                    "fixture-nonce-random96-v1")));
        Assert.Equal(
            "87AB6A5C24A28C357F097A1623B787E3F34B4EE283B11DE47FCE53F19A4258D8",
            Convert.ToHexString(
                C1HashCanonical.ComputeFramingParametersDigest(
                    "fixture-aead-aes256gcm-v1",
                    1,
                    1_048_576,
                    "fixture-nonce-random96-v1")));

        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = 'tagekyc'
              AND table_name IN (
                    'raw_export_source_reservations',
                    'raw_export_source_encryption_attempts',
                    'raw_export_source_head')
              AND lower(column_name) LIKE '%plaintextdigest%';
            """,
            connection);
        Assert.Equal(0L, (long)(await command.ExecuteScalarAsync() ?? -1L));
    }

    private async Task<CandidateFixture> SeedCandidateAsync(
        TimeSpan? remainingRetention = null)
    {
        var actor = Guid.NewGuid();
        var client = Guid.NewGuid();
        var session = Guid.NewGuid();
        var artifact = Guid.NewGuid();
        var policy = await SeedCoreConsentPolicyAsync();
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
                PolicySnapshotId = "c1b2-core-policy",
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
        var authoritySnapshot = await AppendAuthorityAsync(
            actor,
            client,
            session,
            acceptance,
            policy);
        var captured = now.AddSeconds(-5);
        var retentionStart = now.AddSeconds(-4);
        var retentionExpires =
            now.Add(remainingRetention ?? TimeSpan.FromMinutes(30));
        var ingressKey = Guid.NewGuid();
        var owner = Guid.NewGuid();

        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor);
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
        begin.Parameters.AddWithValue("actor", actor);
        begin.Parameters.AddWithValue("client", client);
        begin.Parameters.AddWithValue("producer", actor.ToString("N"));
        begin.Parameters.AddWithValue("agent", "capture-agent-core");
        begin.Parameters.AddWithValue("ingress", ingressKey.ToString("N"));
        begin.Parameters.AddWithValue("session", session);
        begin.Parameters.AddWithValue("acceptance", acceptance);
        begin.Parameters.AddWithValue("artifact", artifact);
        begin.Parameters.AddWithValue("challenge", ChallengeHash);
        begin.Parameters.AddWithValue("authority", authoritySnapshot.ToString("D"));
        begin.Parameters.AddWithValue("captured", captured);
        begin.Parameters.AddWithValue("retentionStart", retentionStart);
        begin.Parameters.AddWithValue("retentionExpires", retentionExpires);
        begin.Parameters.AddWithValue("owner", owner);
        await using var reader = await begin.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.True(reader.IsDBNull(0));
        var token = new RawExportClaimEvaluationToken(
            reader.GetGuid(4),
            reader.GetInt64(5),
            reader.GetInt64(6),
            reader.GetString(2),
            reader.GetFieldValue<DateTimeOffset>(3),
            reader.GetString(1));
        await reader.CloseAsync();
        await transaction.CommitAsync();

        await using var dbRead = postgres.CreateDbContext();
        var claim = Assert.Single(
            dbRead.RawExportSourceIngressClaims.Where(
                row => row.VerificationSessionId == session));
        return new CandidateFixture(
            policy,
            claim.IngressClaimId,
            new RawExportSourceClaimComparisonCommand(
                actor,
                client,
                actor.ToString("N"),
                "capture-agent-core",
                ingressKey,
                token,
                12345,
                "image/jpeg",
                captured,
                retentionStart,
                retentionExpires,
                1800,
                Convert.FromHexString(
                    "E0F1E2F3E4F5E6F7E8F9EAEBECEDEEEFF0F1F2F3F4F5F6F7F8F9FAFBFCFDFEFF")));
    }

    private async Task<Guid> SeedCoreConsentPolicyAsync()
    {
        var policyId = Guid.NewGuid();
        await using var db = postgres.CreateDbContext();
        var policy = await new EfRawExportPolicyRepository(db).AddVersionAsync(
            new AddRawExportPolicyVersionCommand(
                policyId,
                0,
                RawExportMode.EncryptedRawVaultRetained,
                "SubjectRawBiometricExport",
                "fixture-c1-retained-v1",
                "SubjectRawBiometricExport",
                RawExportConsentRequirement.Required,
                null,
                null,
                "Controller",
                "controller:fixture",
                "VN",
                "VN",
                "VN",
                null,
                null,
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
                'evidence:c1b2-core','acceptance-policy:c1b2-core',1);
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
            connection,
            transaction,
            """
            SELECT tagekyc.raw_export_bootstrap_global_authority(
                @admin,'RecorderAuthorityAdmin',
                'decision:c1b2-core-bootstrap');
            """,
            new NpgsqlParameter("admin", admin));
        await SetActorAsync(connection, transaction, admin);
        await ExecuteAsync(
            connection,
            transaction,
            """
            SELECT tagekyc.raw_export_append_subject_consent_authority(
                @actor,@client,'SubjectConsentRecorder',0,'Granted',NULL,
                'decision:c1b2-core',@validUntil);
            """,
            new NpgsqlParameter("actor", actor),
            new NpgsqlParameter("client", client),
            new NpgsqlParameter("validUntil", DateTimeOffset.UtcNow.AddHours(1)));
        await SetActorAsync(connection, transaction, actor);
        await ExecuteAsync(
            connection,
            transaction,
            """
            SELECT tagekyc.raw_export_append_subject_consent_granted(
                @session,@policy,1,ARRAY['LiveSelfieImage']::text[],
                'consent-text-v1','sha256:consent-text',
                'artifact:consent-c1b2-core','decision:c1b2-core',@validUntil);
            """,
            new NpgsqlParameter("session", session),
            new NpgsqlParameter("policy", policy),
            new NpgsqlParameter("validUntil", DateTimeOffset.UtcNow.AddHours(1)));
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

    private ServiceProvider CreateProvider()
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
            connection,
            transaction,
            "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true);",
            new NpgsqlParameter("actor", actor.ToString("D")));

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

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

    private static async Task<bool> HasFunctionPrivilegeAsync(
        NpgsqlConnection connection,
        string role,
        string function)
    {
        await using var command = new NpgsqlCommand(
            "SELECT pg_catalog.has_function_privilege(@role,@function,'EXECUTE');",
            connection);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("function", function);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    private static async Task<string[]> QueryStringsAsync(
        NpgsqlConnection connection,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values.ToArray();
    }

    private static async Task<bool> HasTablePrivilegeAsync(
        NpgsqlConnection connection,
        string role,
        string table,
        string privilege)
    {
        await using var command = new NpgsqlCommand(
            "SELECT pg_catalog.has_table_privilege(@role,@table,@privilege);",
            connection);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("privilege", privilege);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    private sealed record CandidateFixture(
        Guid PolicyId,
        Guid IngressClaimId,
        RawExportSourceClaimComparisonCommand Command);
}
