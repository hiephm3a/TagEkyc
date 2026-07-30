using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1B1IngressClaimTests(PostgresPersistenceFixture postgres)
{
    private const string ChallengeHash =
        "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    private const string TokenAudience =
        "tagekyc.raw-export-source-ingress-claim-comparison";

    private static readonly string[] Tables =
    [
        "raw_export_source_ingress_claim_aliases",
        "raw_export_source_ingress_claims",
    ];

    private static readonly string[] Constraints =
    [
        "ck_raw_export_source_ingress_alias_state",
        "ck_raw_export_source_ingress_state",
        "fk_raw_export_source_ingress_alias_claim",
        "fk_raw_export_source_ingress_claims_acceptance",
        "fk_raw_export_source_ingress_claims_capture_artifact",
        "fk_raw_export_source_ingress_claims_session",
        "pk_raw_export_source_ingress_claim_aliases",
        "pk_raw_export_source_ingress_claims",
        "uq_raw_export_source_ingress_alias_key",
        "uq_raw_export_source_ingress_exact_artifact",
    ];

    private static readonly string[] Indexes =
    [
        "IX_raw_export_source_ingress_aliases_claim",
        "IX_raw_export_source_ingress_claims_acceptance",
        "IX_raw_export_source_ingress_claims_artifact",
        "IX_raw_export_source_ingress_claims_session",
    ];

    private static readonly string[] Triggers =
    [
        "tr_raw_export_source_ingress_aliases_write_guard",
        "tr_raw_export_source_ingress_claims_write_guard",
    ];

    private static readonly string[] Functions =
    [
        "begin_raw_export_source_ingress_claim",
        "enforce_raw_export_source_ingress_write",
        "validate_raw_export_claim_evaluation_token",
    ];

    [Fact]
    public async Task C1B1_identifiers_round_trip_and_acl_manifest_is_exact()
    {
        await using var connection = await OpenAsync();

        Assert.Equal(
            Tables,
            await QueryStringsAsync(
                connection,
                """
                SELECT relation.relname
                FROM pg_catalog.pg_class AS relation
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = ANY(@names)
                ORDER BY relation.relname;
                """,
                new NpgsqlParameter("names", Tables)));

        Assert.Equal(
            Constraints,
            await QueryStringsAsync(
                connection,
                """
                SELECT constraint_row.conname
                FROM pg_catalog.pg_constraint AS constraint_row
                JOIN pg_catalog.pg_class AS relation
                  ON relation.oid = constraint_row.conrelid
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = ANY(@tables)
                  AND constraint_row.contype <> 't'
                ORDER BY constraint_row.conname;
                """,
                new NpgsqlParameter("tables", Tables)));

        Assert.Equal(
            Indexes,
            await QueryStringsAsync(
                connection,
                """
                SELECT relation.relname
                FROM pg_catalog.pg_class AS relation
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relkind = 'i'
                  AND relation.relname = ANY(@names)
                ORDER BY relation.relname;
                """,
                new NpgsqlParameter("names", Indexes)));

        Assert.Equal(
            Triggers,
            await QueryStringsAsync(
                connection,
                """
                SELECT trigger_row.tgname
                FROM pg_catalog.pg_trigger AS trigger_row
                JOIN pg_catalog.pg_class AS relation
                  ON relation.oid = trigger_row.tgrelid
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = ANY(@tables)
                  AND NOT trigger_row.tgisinternal
                ORDER BY trigger_row.tgname;
                """,
                new NpgsqlParameter("tables", Tables)));

        Assert.Equal(
            Functions,
            await QueryStringsAsync(
                connection,
                """
                SELECT function_row.proname
                FROM pg_catalog.pg_proc AS function_row
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = function_row.pronamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND function_row.proname = ANY(@names)
                ORDER BY function_row.proname;
                """,
                new NpgsqlParameter("names", Functions)));

        var runtimeExecutables = await QueryStringsAsync(
            connection,
            """
            SELECT function_row.proname
            FROM pg_catalog.pg_proc AS function_row
            JOIN pg_catalog.pg_namespace AS namespace
              ON namespace.oid = function_row.pronamespace
            WHERE namespace.nspname = 'tagekyc'
              AND function_row.proname = ANY(@names)
              AND pg_catalog.has_function_privilege(
                    'tagekyc_runtime',
                    function_row.oid,
                    'EXECUTE')
            ORDER BY function_row.proname;
            """,
            new NpgsqlParameter("names", Functions));
        Assert.Equal(
            [
                "begin_raw_export_source_ingress_claim",
                "validate_raw_export_claim_evaluation_token",
            ],
            runtimeExecutables);

        Assert.False(await ScalarAsync<bool>(
            connection,
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_catalog.pg_proc AS function_row
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = function_row.pronamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND function_row.proname =
                        'complete_raw_export_source_ingress_claim');
            """));

        foreach (var table in Tables)
        {
            foreach (var privilege in new[]
                     {
                         "SELECT",
                         "INSERT",
                         "UPDATE",
                         "DELETE",
                         "TRUNCATE",
                     })
            {
                Assert.False(await ScalarAsync<bool>(
                    connection,
                    "SELECT pg_catalog.has_table_privilege('tagekyc_runtime', @table, @privilege);",
                    new NpgsqlParameter("table", $"tagekyc.{table}"),
                    new NpgsqlParameter("privilege", privilege)));
            }
        }
    }

    [Fact]
    public async Task C1B1_same_key_is_idempotent_live_slot_is_not_replaced_and_codec_round_trips()
    {
        var request = await CreateRequestAsync(
            tokenTtlSeconds: 30,
            deterministic: true);
        var first = await BeginAsync(request);

        Assert.Null(first.OutcomeCode);
        Assert.Equal("NewClaimEvaluationToken", first.TokenVariant);
        Assert.NotNull(first.Token);
        Assert.Equal(43, first.Token!.Length);
        Assert.Equal(1, first.Revision);
        Assert.Equal(1, first.Fence);
        Assert.Null(first.RetryNotBeforeUtc);

        var repeated = await BeginAsync(request);
        Assert.Equal(
            "RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS",
            repeated.OutcomeCode);
        Assert.Null(repeated.Token);
        Assert.Equal(first.ExpiresAtUtc, repeated.RetryNotBeforeUtc);

        await using var connection = await OpenAsync();
        Assert.Equal(1L, await ScalarAsync<long>(
            connection,
            """
            SELECT count(*)
            FROM tagekyc.raw_export_source_ingress_claim_aliases
            WHERE "IngressIdempotencyKey" = @key;
            """,
            new NpgsqlParameter("key", request.IngressIdempotencyKey)));
        Assert.Equal(1L, await ScalarAsync<long>(
            connection,
            """
            SELECT count(*)
            FROM tagekyc.raw_export_source_ingress_claims
            WHERE "VerificationSessionId" = @sessionId
              AND "CaptureArtifactId" = @artifactId
              AND "CaptureRevision" = @revision
              AND "RawClass" = @rawClass;
            """,
            new NpgsqlParameter("sessionId", request.SessionId),
            new NpgsqlParameter("artifactId", request.ArtifactId),
            new NpgsqlParameter("revision", request.CaptureRevision),
            new NpgsqlParameter("rawClass", request.RawClass)));

        await using var fingerprintCommand = new NpgsqlCommand(
            """
            SELECT claim."IngressIdentityFingerprint",
                   alias."ProducerClaimEnvelopeFingerprint",
                   alias."CurrentClaimEvaluationRevision",
                   alias."CurrentClaimEvaluationFence"
            FROM tagekyc.raw_export_source_ingress_claim_aliases AS alias
            JOIN tagekyc.raw_export_source_ingress_claims AS claim
              ON claim."IngressClaimId" = alias."IngressClaimId"
            WHERE alias."IngressIdempotencyKey" = @key;
            """,
            connection);
        fingerprintCommand.Parameters.AddWithValue(
            "key",
            request.IngressIdempotencyKey);
        await using var reader = await fingerprintCommand.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var ingressFingerprint = reader.GetFieldValue<byte[]>(0);
        var envelopeFingerprint = reader.GetFieldValue<byte[]>(1);
        Assert.Equal(1, reader.GetInt64(2));
        Assert.Equal(1, reader.GetInt64(3));
        Assert.False(await reader.ReadAsync());

        var expectedIngress = C1HashCanonical.ComputeIngressIdentityFingerprint(
            request.ClientApplicationId,
            request.ProducerId,
            request.CaptureAgentInstanceId,
            request.IngressIdempotencyKey,
            request.ActorId,
            request.SessionId,
            request.CaptureAcceptanceId,
            request.ArtifactId,
            request.CaptureRevision,
            request.RawClass,
            request.SessionChallengeHash,
            request.AuthoritySnapshotId);
        var expectedEnvelope =
            C1HashCanonical.ComputeProducerClaimEnvelopeFingerprint(
                expectedIngress,
                request.ClaimedPlaintextLength,
                request.MediaType,
                request.CapturedAtUtc,
                request.RetentionStartedAtUtc,
                request.RetentionExpiresAtUtc,
                request.RetentionBudgetSeconds);

        Assert.Equal(expectedIngress, ingressFingerprint);
        Assert.Equal(expectedEnvelope, envelopeFingerprint);
        Assert.Equal(
            "3D8D47378C0ACA5E402E7B0562F22FE6F7EEA6F53CB4CA60CDEC2E721DA27E74",
            Convert.ToHexString(expectedIngress));
        Assert.Equal(
            "2C9DE1F0E8DFE6728F741A03A9ACF70D91896737AB468D66DAEAD515EA4260D1",
            Convert.ToHexString(expectedEnvelope));

        var jcs = EvidenceCanonicalization.HashCanonical(
            "tip-88c1-producer-claim-envelope-v2",
            new
            {
                IngressIdentityFingerprint =
                    Convert.ToHexString(expectedIngress).ToLowerInvariant(),
                request.ClaimedPlaintextLength,
                request.MediaType,
                CapturedAtUtc =
                    C1HashCanonical.CanonicalTimestamp(request.CapturedAtUtc),
                PlaintextRetentionStartedAtUtc =
                    C1HashCanonical.CanonicalTimestamp(request.RetentionStartedAtUtc),
                PlaintextRetentionExpiresAtUtc =
                    C1HashCanonical.CanonicalTimestamp(request.RetentionExpiresAtUtc),
                request.RetentionBudgetSeconds,
            });
        Assert.NotEqual(
            jcs["sha256:".Length..],
            Convert.ToHexString(expectedEnvelope).ToLowerInvariant());

        var orderedArrayA = C1HashCanonical.Compute(
            "tip-88c1-ordered-array-proof-v1",
            new C1HashCanonical.OrderedArray(
                new IReadOnlyList<string>[]
                {
                    new[] { "ab", "c" },
                    new[] { "d" },
                }));
        var orderedArrayB = C1HashCanonical.Compute(
            "tip-88c1-ordered-array-proof-v1",
            new C1HashCanonical.OrderedArray(
                new IReadOnlyList<string>[]
                {
                    new[] { "a", "bc" },
                    new[] { "d" },
                }));
        var reorderedArray = C1HashCanonical.Compute(
            "tip-88c1-ordered-array-proof-v1",
            new C1HashCanonical.OrderedArray(
                new IReadOnlyList<string>[]
                {
                    new[] { "d" },
                    new[] { "ab", "c" },
                }));
        Assert.NotEqual(orderedArrayA, orderedArrayB);
        Assert.NotEqual(orderedArrayA, reorderedArray);

        var changedEnvelopes = new[]
        {
            request with
            {
                ClaimedPlaintextLength = request.ClaimedPlaintextLength + 1,
            },
            request with
            {
                MediaType = "image/png",
            },
            request with
            {
                CapturedAtUtc =
                    request.CapturedAtUtc.AddTicks(TimeSpan.TicksPerMicrosecond),
            },
            request with
            {
                RetentionStartedAtUtc =
                    request.RetentionStartedAtUtc.AddTicks(
                        TimeSpan.TicksPerMicrosecond),
            },
            request with
            {
                RetentionExpiresAtUtc =
                    request.RetentionExpiresAtUtc.AddTicks(
                        -TimeSpan.TicksPerMicrosecond),
            },
            request with
            {
                RetentionBudgetSeconds = request.RetentionBudgetSeconds + 1,
            },
        };
        foreach (var changedEnvelope in changedEnvelopes)
        {
            var invalid = await Assert.ThrowsAsync<PostgresException>(
                () => BeginAsync(changedEnvelope));
            Assert.Equal(PostgresErrorCodes.RaiseException, invalid.SqlState);
            Assert.Equal(
                "RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID",
                invalid.MessageText);
        }
    }

    [Fact]
    public async Task C1B1_expired_slot_reissues_with_fresh_identity_token_revision_and_fence()
    {
        var request = await CreateRequestAsync(tokenTtlSeconds: 1);
        var first = await BeginAsync(request);
        var delay = first.ExpiresAtUtc - DateTimeOffset.UtcNow
            + TimeSpan.FromMilliseconds(75);
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay);
        }

        var second = await BeginAsync(request);
        Assert.Null(second.OutcomeCode);
        Assert.Equal(first.TokenVariant, second.TokenVariant);
        Assert.NotEqual(first.Token, second.Token);
        Assert.NotEqual(first.EvaluationId, second.EvaluationId);
        Assert.Equal(first.Revision + 1, second.Revision);
        Assert.Equal(first.Fence + 1, second.Fence);
        Assert.True(second.ExpiresAtUtc > first.ExpiresAtUtc);

        await using var connection = await OpenAsync();
        var latestIssued = await ScalarAsync<DateTime>(
            connection,
            """
            SELECT "LatestIssuedTokenExpiresAtUtc"
            FROM tagekyc.raw_export_source_ingress_claim_aliases
            WHERE "IngressIdempotencyKey" = @key;
            """,
            new NpgsqlParameter("key", request.IngressIdempotencyKey));
        Assert.Equal(
            second.ExpiresAtUtc,
            new DateTimeOffset(
                DateTime.SpecifyKind(latestIssued, DateTimeKind.Utc)));

        Assert.False(await ValidateAsync(request, first));
        Assert.True(await ValidateAsync(request, second));
    }

    [Fact]
    public async Task C1B1_token_validator_rejects_tamper_wrong_manifest_expiry_and_stale_cas()
    {
        var request = await CreateRequestAsync(tokenTtlSeconds: 30);
        var issued = await BeginAsync(request);
        Assert.True(await ValidateAsync(request, issued));

        var tamperedToken = issued.Token![..^1]
            + (issued.Token[^1] == 'A' ? "B" : "A");
        Assert.False(await ValidateAsync(
            request,
            issued with { Token = tamperedToken }));
        Assert.False(await ValidateAsync(
            request,
            issued with { TokenVariant = "ExistingClaimComparisonToken" }));
        Assert.False(await ValidateAsync(
            request,
            issued with { EvaluationId = Guid.NewGuid() }));
        Assert.False(await ValidateAsync(
            request,
            issued with
            {
                ExpiresAtUtc =
                    issued.ExpiresAtUtc.AddTicks(TimeSpan.TicksPerMicrosecond),
            }));
        Assert.False(await ValidateAsync(
            request,
            issued with { Revision = issued.Revision + 1 }));
        Assert.False(await ValidateAsync(
            request,
            issued with { Fence = issued.Fence + 1 }));
        Assert.False(await ValidateAsync(
            request,
            issued,
            actorOverride: Guid.NewGuid()));

        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteAsync(
            connection,
            transaction,
            """
            ALTER TABLE tagekyc.raw_export_source_ingress_claim_aliases
            DROP CONSTRAINT ck_raw_export_source_ingress_alias_state;
            """);
        await SetActorAsync(connection, transaction, request.ActorId);
        await ExecuteAsync(
            connection,
            transaction,
            "SET LOCAL ROLE tagekyc_raw_export_deployer;");
        await ExecuteAsync(
            connection,
            transaction,
            """
            SELECT pg_catalog.set_config(
                'tagekyc.raw_export_source_ingress_write_context',
                'alias:UPDATE',
                true);
            UPDATE tagekyc.raw_export_source_ingress_claim_aliases
            SET "CurrentTokenAudience" = 'wrong-audience'
            WHERE "IngressIdempotencyKey" = @key;
            """,
            new NpgsqlParameter("key", request.IngressIdempotencyKey));
        await ExecuteAsync(connection, transaction, "RESET ROLE;");

        Assert.False(await ValidateAsync(
            connection,
            transaction,
            request,
            issued));
        await transaction.RollbackAsync();

        await using var schemaTransaction =
            await connection.BeginTransactionAsync();
        await ExecuteAsync(
            connection,
            schemaTransaction,
            """
            ALTER TABLE tagekyc.raw_export_source_ingress_claim_aliases
            DROP CONSTRAINT ck_raw_export_source_ingress_alias_state;
            """);
        await SetActorAsync(connection, schemaTransaction, request.ActorId);
        await ExecuteAsync(
            connection,
            schemaTransaction,
            "SET LOCAL ROLE tagekyc_raw_export_deployer;");
        await ExecuteAsync(
            connection,
            schemaTransaction,
            """
            SELECT pg_catalog.set_config(
                'tagekyc.raw_export_source_ingress_write_context',
                'alias:UPDATE',
                true);
            UPDATE tagekyc.raw_export_source_ingress_claim_aliases
            SET "CurrentTokenSchemaVersion" = 2
            WHERE "IngressIdempotencyKey" = @key;
            """,
            new NpgsqlParameter("key", request.IngressIdempotencyKey));
        await ExecuteAsync(connection, schemaTransaction, "RESET ROLE;");

        Assert.False(await ValidateAsync(
            connection,
            schemaTransaction,
            request,
            issued));
        await schemaTransaction.RollbackAsync();

        var expiringRequest = await CreateRequestAsync(tokenTtlSeconds: 1);
        var expiringToken = await BeginAsync(expiringRequest);
        var delay = expiringToken.ExpiresAtUtc - DateTimeOffset.UtcNow
            + TimeSpan.FromMilliseconds(75);
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay);
        }

        Assert.False(await ValidateAsync(expiringRequest, expiringToken));
    }

    [Fact]
    public async Task C1B1_bounded_lookup_lock_contention_is_busy_not_live_evaluation()
    {
        var request = await CreateRequestAsync(tokenTtlSeconds: 30);
        await using var blocker = await OpenAsync();
        await using var blockerTransaction = await blocker.BeginTransactionAsync();
        await using (var lockCommand = new NpgsqlCommand(
            """
            SELECT pg_catalog.pg_advisory_xact_lock(
                pg_catalog.hashtextextended(
                    'tip88c1b1:exact:' ||
                    @clientApplicationId::text || ':' ||
                    @producerId || ':' ||
                    @sessionId::text || ':' ||
                    @artifactId::text || ':' ||
                    @captureRevision::text || ':' ||
                    @rawClass,
                    0));
            """,
            blocker,
            blockerTransaction))
        {
            lockCommand.Parameters.AddWithValue(
                "clientApplicationId",
                request.ClientApplicationId);
            lockCommand.Parameters.AddWithValue("producerId", request.ProducerId);
            lockCommand.Parameters.AddWithValue("sessionId", request.SessionId);
            lockCommand.Parameters.AddWithValue("artifactId", request.ArtifactId);
            lockCommand.Parameters.AddWithValue(
                "captureRevision",
                request.CaptureRevision);
            lockCommand.Parameters.AddWithValue("rawClass", request.RawClass);
            await lockCommand.ExecuteNonQueryAsync();
        }

        var busy = await Assert.ThrowsAsync<PostgresException>(
            () => BeginAsync(request with { LockTimeoutMilliseconds = 10 }));
        Assert.Equal(PostgresErrorCodes.LockNotAvailable, busy.SqlState);
        Assert.Equal("RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY", busy.MessageText);

        await using (var observer = await OpenAsync())
        {
            Assert.Equal(0L, await ScalarAsync<long>(
                observer,
                """
                SELECT count(*)
                FROM tagekyc.raw_export_source_ingress_claim_aliases
                WHERE "IngressIdempotencyKey" = @key;
                """,
                new NpgsqlParameter("key", request.IngressIdempotencyKey)));
            Assert.Equal(0L, await ScalarAsync<long>(
                observer,
                """
                SELECT count(*)
                FROM tagekyc.raw_export_source_ingress_claims
                WHERE "VerificationSessionId" = @sessionId;
                """,
                new NpgsqlParameter("sessionId", request.SessionId)));
        }

        await blockerTransaction.RollbackAsync();
        var issued = await BeginAsync(request);
        Assert.NotNull(issued.Token);
    }

    [Fact]
    public async Task C1B1_binding_actor_and_direct_dml_fail_closed()
    {
        var request = await CreateRequestAsync(tokenTtlSeconds: 30);

        var missingActor = await Assert.ThrowsAsync<PostgresException>(
            () => BeginAsync(request, setActor: false));
        Assert.Equal(PostgresErrorCodes.RaiseException, missingActor.SqlState);
        Assert.Equal("RAW_EXPORT_ACTOR_CONTEXT_MISSING", missingActor.MessageText);

        var actorMismatch = await Assert.ThrowsAsync<PostgresException>(
            () => BeginAsync(
                request,
                actorOverride: Guid.NewGuid()));
        Assert.Equal(PostgresErrorCodes.RaiseException, actorMismatch.SqlState);
        Assert.Equal("RAW_EXPORT_SOURCE_BINDING_INVALID", actorMismatch.MessageText);

        var producerMismatch = await Assert.ThrowsAsync<PostgresException>(
            () => BeginAsync(request with { ProducerId = Guid.NewGuid().ToString("N") }));
        Assert.Equal("RAW_EXPORT_SOURCE_BINDING_INVALID", producerMismatch.MessageText);

        foreach (var mutation in new[]
                 {
                     request with { ClientApplicationId = Guid.NewGuid() },
                     request with { SessionChallengeHash = $"sha256:{new string('d', 64)}" },
                     request with { CaptureRevision = 2 },
                     request with { RawClass = "LivenessMedia" },
                     request with { ArtifactId = Guid.NewGuid() },
                 })
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => BeginAsync(mutation));
            Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
            Assert.Equal("RAW_EXPORT_SOURCE_BINDING_INVALID", exception.MessageText);
        }

        var issued = await BeginAsync(request);
        Assert.NotNull(issued.Token);

        await using (var connection = await OpenAsync())
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await ExecuteAsync(
                connection,
                transaction,
                "SET LOCAL ROLE tagekyc_raw_export_deployer;");
            var direct = await Assert.ThrowsAsync<PostgresException>(
                () => ExecuteAsync(
                    connection,
                    transaction,
                    """
                    UPDATE tagekyc.raw_export_source_ingress_claim_aliases
                    SET "CurrentClaimEvaluationFence" =
                        "CurrentClaimEvaluationFence" + 1
                    WHERE "IngressIdempotencyKey" = @key;
                    """,
                    new NpgsqlParameter("key", request.IngressIdempotencyKey)));
            Assert.Equal(PostgresErrorCodes.RaiseException, direct.SqlState);
            Assert.Equal(
                "RAW_EXPORT_SOURCE_INGRESS_DIRECT_DML_UNSUPPORTED",
                direct.MessageText);
            await transaction.RollbackAsync();
        }

        await using (var connection = await OpenAsync())
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await ExecuteAsync(
                connection,
                transaction,
                "SET LOCAL ROLE tagekyc_runtime;");
            var runtimeInsert = await Assert.ThrowsAsync<PostgresException>(
                () => ExecuteAsync(
                    connection,
                    transaction,
                    """
                    INSERT INTO tagekyc.raw_export_source_ingress_claim_aliases
                        ("IngressClaimAliasId")
                    VALUES (pg_catalog.gen_random_uuid());
                    """));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, runtimeInsert.SqlState);
            await transaction.RollbackAsync();
        }
    }

    [Fact]
    public async Task C1B1_unique_alias_and_exact_artifact_edges_reject_duplicates()
    {
        var request = await CreateRequestAsync(tokenTtlSeconds: 30);
        await BeginAsync(request);

        var aliasViolation = await AssertDuplicateCopyFailsAsync(
            """
            INSERT INTO tagekyc.raw_export_source_ingress_claim_aliases
            SELECT
                pg_catalog.gen_random_uuid(),
                alias."ClientApplicationId",
                alias."ProducerId",
                alias."CaptureAgentInstanceId",
                alias."IngressIdempotencyKey",
                alias."AttemptedIngressIdentityFingerprint",
                alias."ProducerClaimEnvelopeFingerprint",
                alias."AliasState",
                alias."IngressClaimId",
                pg_catalog.gen_random_uuid(),
                alias."CurrentClaimEvaluationOwnerId",
                alias."CurrentClaimEvaluationDisposition",
                alias."CurrentTokenIssuedAtUtc",
                alias."CurrentTokenExpiresAtUtc",
                alias."CurrentTokenSchemaVersion",
                alias."CurrentTokenVariant",
                alias."CurrentTokenAudience",
                alias."CurrentTokenDigest",
                alias."CurrentClaimEvaluationRevision",
                alias."CurrentClaimEvaluationFence",
                alias."LatestIssuedTokenExpiresAtUtc",
                alias."CreatedAtUtc"
            FROM tagekyc.raw_export_source_ingress_claim_aliases AS alias
            WHERE alias."IngressIdempotencyKey" = @key;
            """,
            "alias:INSERT",
            request.IngressIdempotencyKey);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, aliasViolation.SqlState);
        Assert.Equal(
            "uq_raw_export_source_ingress_alias_key",
            aliasViolation.ConstraintName);

        var exactViolation = await AssertDuplicateCopyFailsAsync(
            """
            INSERT INTO tagekyc.raw_export_source_ingress_claims
            SELECT
                pg_catalog.gen_random_uuid(),
                claim."VerificationSessionId",
                claim."CaptureAcceptanceId",
                claim."CaptureArtifactId",
                claim."ClientApplicationId",
                claim."AuthenticatedPrincipalId",
                claim."ProducerId",
                claim."CaptureAgentInstanceId",
                claim."CaptureRevision",
                claim."RawClass",
                claim."SessionChallengeHash",
                claim."AuthoritySnapshotId",
                claim."IngressIdentityFingerprint",
                claim."ClaimState",
                claim."CommitmentKeySelectorId",
                claim."CommitmentKeySelectorVersion",
                claim."CreatedAtUtc"
            FROM tagekyc.raw_export_source_ingress_claims AS claim
            WHERE claim."VerificationSessionId" = @sessionId;
            """,
            "claim:INSERT",
            request.IngressIdempotencyKey,
            request.SessionId);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, exactViolation.SqlState);
        Assert.Equal(
            "uq_raw_export_source_ingress_exact_artifact",
            exactViolation.ConstraintName);
    }

    private async Task<BeginRequest> CreateRequestAsync(
        int tokenTtlSeconds,
        bool deterministic = false)
    {
        var actorId = deterministic
            ? Guid.Parse("11111111-2222-4333-8444-555555555555")
            : Guid.NewGuid();
        var sessionId = deterministic
            ? Guid.Parse("01234567-89ab-4def-8123-456789abcdef")
            : Guid.NewGuid();
        var clientApplicationId = deterministic
            ? Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee")
            : Guid.NewGuid();
        var artifactId = deterministic
            ? Guid.Parse("fedcba98-7654-4321-8fed-cba987654321")
            : Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var db = postgres.CreateDbContext())
        {
            db.Sessions.Add(new VerificationSessionRow
            {
                Id = sessionId,
                ClientApplicationId = clientApplicationId,
                SubjectRef = $"c1b1-subject-{Guid.NewGuid():N}",
                Profile = "ChallengeBoundEkycProfile",
                Purpose = "raw-export",
                RequiredChecksJson = "[\"LiveSelfie\"]",
                BindingNonceHash = ChallengeHash,
                RequestId = Guid.NewGuid().ToString("N"),
                CorrelationId = Guid.NewGuid().ToString("N"),
                State = "Completed",
                Result = "Passed",
                AssuranceLevel = "Substantial",
                PolicySnapshotId = "c1b1-policy",
                RetentionClass = "Standard",
                DeletionEligibility = "Pending",
                LegalHoldStatus = "None",
                PurgeBlockReason = "None",
                ExpiresAt = now.AddHours(1),
                CreatedAt = now,
                CompletedAt = now,
            });
            db.CaptureArtifacts.Add(new CaptureArtifactRow
            {
                Id = artifactId,
                VerificationSessionId = sessionId,
                ArtifactType = "SelfieImage",
                CaptureSource = "MobileSdk",
                ArtifactHash = $"sha256:{new string('a', 64)}",
                MetadataHash = $"sha256:{new string('b', 64)}",
                QualityState = "Accepted",
                RequestId = Guid.NewGuid().ToString("N"),
                CorrelationId = Guid.NewGuid().ToString("N"),
                CreatedAt = now,
                ExpiresAt = now.AddHours(1),
            });
            await db.SaveChangesAsync();
        }

        var acceptanceId = deterministic
            ? await InsertAcceptanceFixtureAsync(
                Guid.Parse("22222222-3333-4444-8555-666666666666"),
                sessionId,
                clientApplicationId,
                artifactId)
            : await AppendAcceptanceAsync(
                actorId,
                sessionId,
                clientApplicationId,
                artifactId);
        var capturedAt = new DateTimeOffset(
            2026,
            7,
            30,
            1,
            2,
            3,
            TimeSpan.Zero).AddTicks(1_234_567);
        var retentionStarted = capturedAt.AddSeconds(1);
        var retentionExpires = retentionStarted.AddSeconds(60);
        return new BeginRequest(
            actorId,
            clientApplicationId,
            actorId.ToString("N"),
            "capture-agent-e\u0301",
            deterministic
                ? Guid.Parse("33333333-4444-4555-8666-777777777777")
                : Guid.NewGuid(),
            sessionId,
            acceptanceId,
            artifactId,
            1,
            "LiveSelfieImage",
            ChallengeHash,
            "authority-snapshot-c1b1",
            12_345,
            "image/jpeg",
            capturedAt,
            retentionStarted,
            retentionExpires,
            60,
            "commitment-selector-c1b1",
            1,
            deterministic
                ? Guid.Parse("44444444-5555-4666-8777-888888888888")
                : Guid.NewGuid(),
            tokenTtlSeconds,
            100);
    }

    private async Task<Guid> InsertAcceptanceFixtureAsync(
        Guid acceptanceId,
        Guid sessionId,
        Guid clientApplicationId,
        Guid artifactId)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteAsync(
            connection,
            transaction,
            "SET LOCAL ROLE tagekyc_raw_export_deployer;");
        await ExecuteAsync(
            connection,
            transaction,
            """
            SELECT pg_catalog.set_config(
                'tagekyc.raw_export_capture_acceptance_append_context',
                'event',
                true);
            INSERT INTO tagekyc.raw_export_capture_acceptance_events
                ("CaptureAcceptanceId",
                 "VerificationSessionId",
                 "ClientApplicationId",
                 "RawClass",
                 "CaptureArtifactId",
                 "CaptureRevision",
                 "SessionChallengeHash",
                 "AcceptedEvidenceRef",
                 "AcceptedAtUtc",
                 "AcceptancePolicyId",
                 "AcceptancePolicyVersion")
            VALUES
                (@acceptanceId,
                 @sessionId,
                 @clientApplicationId,
                 'LiveSelfieImage',
                 @artifactId,
                 1,
                 @challengeHash,
                 'evidence:c1b1-golden',
                 pg_catalog.transaction_timestamp(),
                 'acceptance-policy:c1b1',
                 1);
            """,
            new NpgsqlParameter("acceptanceId", acceptanceId),
            new NpgsqlParameter("sessionId", sessionId),
            new NpgsqlParameter("clientApplicationId", clientApplicationId),
            new NpgsqlParameter("artifactId", artifactId),
            new NpgsqlParameter("challengeHash", ChallengeHash));
        await transaction.CommitAsync();
        return acceptanceId;
    }

    private async Task<Guid> AppendAcceptanceAsync(
        Guid actorId,
        Guid sessionId,
        Guid clientApplicationId,
        Guid artifactId)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actorId);
        await using var command = new NpgsqlCommand(
            """
            SELECT tagekyc.raw_export_append_capture_acceptance(
                @sessionId,
                @clientApplicationId,
                'LiveSelfieImage',
                @artifactId,
                1,
                @challengeHash,
                'evidence:c1b1',
                'acceptance-policy:c1b1',
                1);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue("clientApplicationId", clientApplicationId);
        command.Parameters.AddWithValue("artifactId", artifactId);
        command.Parameters.AddWithValue("challengeHash", ChallengeHash);
        var result = (Guid)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Acceptance function returned null."));
        await transaction.CommitAsync();
        return result;
    }

    private async Task<BeginResult> BeginAsync(
        BeginRequest request,
        bool setActor = true,
        Guid? actorOverride = null)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        if (setActor)
        {
            await SetActorAsync(
                connection,
                transaction,
                actorOverride ?? request.ActorId);
        }

        await using var command = CreateBeginCommand(connection, transaction, request);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var result = new BeginResult(
            reader.IsDBNull(0) ? null : reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? default : reader.GetFieldValue<DateTimeOffset>(3),
            reader.IsDBNull(4) ? default : reader.GetGuid(4),
            reader.IsDBNull(5) ? 0 : reader.GetInt64(5),
            reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
            reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7));
        Assert.False(await reader.ReadAsync());
        await reader.CloseAsync();
        await transaction.CommitAsync();
        return result;
    }

    private static NpgsqlCommand CreateBeginCommand(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        BeginRequest request)
    {
        var command = new NpgsqlCommand(
            """
            SELECT *
            FROM tagekyc.begin_raw_export_source_ingress_claim(
                @actorId,
                @clientApplicationId,
                @producerId,
                @captureAgentInstanceId,
                @ingressIdempotencyKey,
                @sessionId,
                @captureAcceptanceId,
                @artifactId,
                @captureRevision,
                @rawClass,
                @sessionChallengeHash,
                @authoritySnapshotId,
                @claimedPlaintextLength,
                @mediaType,
                @capturedAtUtc,
                @retentionStartedAtUtc,
                @retentionExpiresAtUtc,
                @retentionBudgetSeconds,
                @commitmentKeySelectorId,
                @commitmentKeySelectorVersion,
                @claimEvaluationOwnerId,
                @tokenTtlSeconds,
                @lockTimeoutMilliseconds);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("actorId", request.ActorId);
        command.Parameters.AddWithValue(
            "clientApplicationId",
            request.ClientApplicationId);
        command.Parameters.AddWithValue("producerId", request.ProducerId);
        command.Parameters.AddWithValue(
            "captureAgentInstanceId",
            request.CaptureAgentInstanceId);
        command.Parameters.AddWithValue(
            "ingressIdempotencyKey",
            request.IngressIdempotencyKey.ToString("N"));
        command.Parameters.AddWithValue("sessionId", request.SessionId);
        command.Parameters.AddWithValue(
            "captureAcceptanceId",
            request.CaptureAcceptanceId);
        command.Parameters.AddWithValue("artifactId", request.ArtifactId);
        command.Parameters.AddWithValue("captureRevision", request.CaptureRevision);
        command.Parameters.AddWithValue("rawClass", request.RawClass);
        command.Parameters.AddWithValue(
            "sessionChallengeHash",
            request.SessionChallengeHash);
        command.Parameters.AddWithValue(
            "authoritySnapshotId",
            request.AuthoritySnapshotId);
        command.Parameters.AddWithValue(
            "claimedPlaintextLength",
            request.ClaimedPlaintextLength);
        command.Parameters.AddWithValue("mediaType", request.MediaType);
        command.Parameters.AddWithValue("capturedAtUtc", request.CapturedAtUtc);
        command.Parameters.AddWithValue(
            "retentionStartedAtUtc",
            request.RetentionStartedAtUtc);
        command.Parameters.AddWithValue(
            "retentionExpiresAtUtc",
            request.RetentionExpiresAtUtc);
        command.Parameters.AddWithValue(
            "retentionBudgetSeconds",
            request.RetentionBudgetSeconds);
        command.Parameters.AddWithValue(
            "commitmentKeySelectorId",
            request.CommitmentKeySelectorId);
        command.Parameters.AddWithValue(
            "commitmentKeySelectorVersion",
            request.CommitmentKeySelectorVersion);
        command.Parameters.AddWithValue(
            "claimEvaluationOwnerId",
            request.ClaimEvaluationOwnerId);
        command.Parameters.AddWithValue(
            "tokenTtlSeconds",
            request.TokenTtlSeconds);
        command.Parameters.AddWithValue(
            "lockTimeoutMilliseconds",
            request.LockTimeoutMilliseconds);
        return command;
    }

    private async Task<bool> ValidateAsync(
        BeginRequest request,
        BeginResult result,
        Guid? actorOverride = null)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(
            connection,
            transaction,
            actorOverride ?? request.ActorId);
        var valid = await ValidateAsync(connection, transaction, request, result);
        await transaction.RollbackAsync();
        return valid;
    }

    private static async Task<bool> ValidateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        BeginRequest request,
        BeginResult result)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT tagekyc.validate_raw_export_claim_evaluation_token(
                @clientApplicationId,
                @producerId,
                @captureAgentInstanceId,
                @ingressIdempotencyKey,
                @evaluationId,
                @revision,
                @fence,
                @tokenVariant,
                @expiresAtUtc,
                @token);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue(
            "clientApplicationId",
            request.ClientApplicationId);
        command.Parameters.AddWithValue("producerId", request.ProducerId);
        command.Parameters.AddWithValue(
            "captureAgentInstanceId",
            request.CaptureAgentInstanceId);
        command.Parameters.AddWithValue(
            "ingressIdempotencyKey",
            request.IngressIdempotencyKey.ToString("N"));
        command.Parameters.AddWithValue("evaluationId", result.EvaluationId);
        command.Parameters.AddWithValue("revision", result.Revision);
        command.Parameters.AddWithValue("fence", result.Fence);
        command.Parameters.AddWithValue("tokenVariant", result.TokenVariant!);
        command.Parameters.AddWithValue("expiresAtUtc", result.ExpiresAtUtc);
        command.Parameters.AddWithValue("token", result.Token!);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Token validator returned null."));
    }

    private async Task<PostgresException> AssertDuplicateCopyFailsAsync(
        string sql,
        string context,
        Guid ingressKey,
        Guid? sessionId = null)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteAsync(
            connection,
            transaction,
            "SET LOCAL ROLE tagekyc_raw_export_deployer;");
        await ExecuteAsync(
            connection,
            transaction,
            """
            SELECT pg_catalog.set_config(
                'tagekyc.raw_export_source_ingress_write_context',
                @context,
                true);
            """,
            new NpgsqlParameter("context", context));
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => ExecuteAsync(
                connection,
                transaction,
                sql,
                new NpgsqlParameter("key", ingressKey),
                new NpgsqlParameter(
                    "sessionId",
                    sessionId ?? Guid.Empty)));
        await transaction.RollbackAsync();
        return exception;
    }

    private static Task SetActorAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid actorId) =>
        ExecuteAsync(
            connection,
            transaction,
            """
            SELECT pg_catalog.set_config(
                'tagekyc.actor_principal_id',
                @actor,
                true);
            """,
            new NpgsqlParameter("actor", actorId.ToString("D")));

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        return connection;
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

    private static async Task<T> ScalarAsync<T>(
        NpgsqlConnection connection,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return (T)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Scalar query returned null."));
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

    private sealed record BeginRequest(
        Guid ActorId,
        Guid ClientApplicationId,
        string ProducerId,
        string CaptureAgentInstanceId,
        Guid IngressIdempotencyKey,
        Guid SessionId,
        Guid CaptureAcceptanceId,
        Guid ArtifactId,
        int CaptureRevision,
        string RawClass,
        string SessionChallengeHash,
        string AuthoritySnapshotId,
        long ClaimedPlaintextLength,
        string MediaType,
        DateTimeOffset CapturedAtUtc,
        DateTimeOffset RetentionStartedAtUtc,
        DateTimeOffset RetentionExpiresAtUtc,
        int RetentionBudgetSeconds,
        string CommitmentKeySelectorId,
        int CommitmentKeySelectorVersion,
        Guid ClaimEvaluationOwnerId,
        int TokenTtlSeconds,
        int LockTimeoutMilliseconds);

    private sealed record BeginResult(
        string? OutcomeCode,
        string? Token,
        string? TokenVariant,
        DateTimeOffset ExpiresAtUtc,
        Guid EvaluationId,
        long Revision,
        long Fence,
        DateTimeOffset? RetryNotBeforeUtc);
}
