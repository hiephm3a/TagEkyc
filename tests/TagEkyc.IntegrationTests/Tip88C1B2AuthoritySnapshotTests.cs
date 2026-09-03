using System.Text;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1B2AuthoritySnapshotTests(
    PostgresPersistenceFixture postgres)
{
    private const string Table = "raw_export_authority_snapshots";

    private static readonly string[] Constraints =
    [
        "pk_raw_export_authority_snapshots",
        "uq_raw_export_authority_snapshot_scope_revision",
        "fk_raw_export_authority_snapshot_session",
        "fk_raw_export_authority_snapshot_acceptance",
        "ck_raw_export_authority_snapshot_event_type",
        "ck_raw_export_authority_snapshot_values",
        "ck_raw_export_authority_snapshot_event_shape",
    ];

    private static readonly string[] Functions =
    [
        "enforce_raw_export_authority_snapshot_insert",
        "raw_export_append_authority_snapshot",
        "raw_export_withdraw_authority_snapshot",
        "raw_export_revoke_authority_snapshot",
        "raw_export_resolve_current_authority_for_source",
    ];

    private static readonly string[] Triggers =
    [
        "tr_raw_export_authority_snapshots_insert_guard",
        "tr_raw_export_authority_snapshots_append_only",
    ];

    private static readonly string[] Indexes =
    [
        "IX_raw_export_authority_snapshots_CaptureAcceptanceId",
        "IX_raw_export_authority_snapshots_VerificationSessionId",
    ];

    private static readonly string[] Columns =
    [
        "AbsoluteSourceExpiresAtUtc",
        "ApprovedPurpose",
        "AuthorityArtifactId",
        "AuthorityArtifactVersion",
        "AuthoritySnapshotEventId",
        "AuthoritySnapshotId",
        "AuthoritySnapshotSchemaVersion",
        "CapturedByPrincipalId",
        "CaptureAcceptanceId",
        "ClientApplicationId",
        "ControllerIdentity",
        "ConsentPolicyId",
        "ConsentPolicyVersion",
        "EvaluatedAtUtc",
        "EventType",
        "ExtensionDisposition",
        "LegalHoldPolicyId",
        "PurgePolicyId",
        "RawClass",
        "RecordedAtUtc",
        "RetentionClass",
        "RetentionPolicyId",
        "RetentionPolicyVersion",
        "RetentionStartEvent",
        "ReuseDisposition",
        "Revision",
        "RevocationPolicyId",
        "RevokedByPrincipalId",
        "StableDataScopeId",
        "TargetRevision",
        "ValidFromUtc",
        "ValidUntilUtc",
        "VerificationSessionId",
        "WithdrawnByPrincipalId",
    ];

    [Fact]
    public async Task C1B2_authority_identifiers_round_trip_and_acl_is_exact()
    {
        var intended = Constraints
            .Concat(Functions)
            .Concat(Triggers)
            .Concat(Indexes)
            .Concat(Columns)
            .Append(Table)
            .ToArray();
        Assert.All(
            intended,
            name => Assert.InRange(Encoding.UTF8.GetByteCount(name), 1, 63));

        await using var connection = await OpenAsync();
        Assert.Equal(
            Table,
            await ScalarStringAsync(
                connection,
                """
                SELECT relation.relname
                FROM pg_catalog.pg_class AS relation
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = @table
                  AND relation.relkind = 'r';
                """,
                new NpgsqlParameter("table", Table)));
        Assert.Equal(
            Constraints.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(
                connection,
                """
                SELECT constraint_row.conname
                FROM pg_catalog.pg_constraint AS constraint_row
                JOIN pg_catalog.pg_class AS relation
                  ON relation.oid = constraint_row.conrelid
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = @table;
                """,
                new NpgsqlParameter("table", Table)))
            .Where(Constraints.Contains)
            .Order(StringComparer.Ordinal));
        Assert.Equal(
            Indexes.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(
                connection,
                """
                SELECT index_relation.relname
                FROM pg_catalog.pg_index AS index_row
                JOIN pg_catalog.pg_class AS table_relation
                  ON table_relation.oid = index_row.indrelid
                JOIN pg_catalog.pg_class AS index_relation
                  ON index_relation.oid = index_row.indexrelid
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = table_relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND table_relation.relname = @table
                  AND index_relation.relname = ANY(@names);
                """,
                new NpgsqlParameter("table", Table),
                new NpgsqlParameter("names", Indexes)))
            .Order(StringComparer.Ordinal));
        Assert.Equal(
            Columns.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(
                connection,
                """
                SELECT attribute.attname
                FROM pg_catalog.pg_attribute AS attribute
                JOIN pg_catalog.pg_class AS relation
                  ON relation.oid = attribute.attrelid
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = @table
                  AND attribute.attnum > 0
                  AND NOT attribute.attisdropped;
                """,
                new NpgsqlParameter("table", Table)))
            .Order(StringComparer.Ordinal));
        Assert.Equal(
            Functions.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(
                connection,
                """
                SELECT procedure.proname
                FROM pg_catalog.pg_proc AS procedure
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = procedure.pronamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND procedure.proname = ANY(@names);
                """,
                new NpgsqlParameter("names", Functions)))
            .Order(StringComparer.Ordinal));
        Assert.Equal(
            Triggers.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(
                connection,
                """
                SELECT trigger_row.tgname
                FROM pg_catalog.pg_trigger AS trigger_row
                JOIN pg_catalog.pg_class AS relation
                  ON relation.oid = trigger_row.tgrelid
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = @table
                  AND NOT trigger_row.tgisinternal;
                """,
                new NpgsqlParameter("table", Table)))
            .Order(StringComparer.Ordinal));

        Assert.False(await HasFunctionPrivilegeAsync(
            connection,
            "tagekyc_runtime",
            "tagekyc.raw_export_append_authority_snapshot(uuid,uuid,uuid,text,uuid,integer,text,text,text,integer,uuid,integer,text,text,timestamptz,text,text,text,timestamptz,timestamptz)",
            "EXECUTE"));
        Assert.False(await HasFunctionPrivilegeAsync(
            connection,
            "tagekyc_runtime",
            "tagekyc.raw_export_withdraw_authority_snapshot(uuid,uuid,uuid,text,bigint,uuid)",
            "EXECUTE"));
        Assert.False(await HasFunctionPrivilegeAsync(
            connection,
            "tagekyc_runtime",
            "tagekyc.raw_export_revoke_authority_snapshot(uuid,uuid,uuid,text,bigint,uuid)",
            "EXECUTE"));
        Assert.True(await HasFunctionPrivilegeAsync(
            connection,
            "tagekyc_runtime",
            "tagekyc.raw_export_resolve_current_authority_for_source(uuid,uuid,uuid,text,timestamptz)",
            "EXECUTE"));
        Assert.False(await HasTablePrivilegeAsync(
            connection,
            "tagekyc_runtime",
            "tagekyc.raw_export_authority_snapshots",
            "SELECT"));
        Assert.False(await HasTablePrivilegeAsync(
            connection,
            "tagekyc_runtime",
            "tagekyc.raw_export_authority_snapshots",
            "INSERT"));
    }

    [Fact]
    public async Task C1B2_grant_resolves_exact_frozen_authority_and_foreign_bindings_do_not()
    {
        var fixture = await SeedScopeAsync();
        var evaluatedAt = DateTimeOffset.UtcNow;
        var expiresAt = evaluatedAt.AddHours(2);
        var validUntil = evaluatedAt.AddHours(1);
        var granted = await AppendGrantAsync(
            fixture,
            evaluatedAt,
            expiresAt,
            validUntil);

        var resolved = await ResolveAsync(fixture, evaluatedAt.AddMinutes(1));
        Assert.NotNull(resolved);
        Assert.Equal(1, resolved.Revision);
        Assert.Equal(granted.AuthoritySnapshotId, resolved.AuthoritySnapshotId);
        Assert.Equal("controller:fixture", resolved.ControllerIdentity);
        Assert.Equal("scope:fixture", resolved.StableDataScopeId);
        Assert.Equal("SubjectRawBiometricExport", resolved.ApprovedPurpose);
        Assert.Equal(
            expiresAt.ToUnixTimeMilliseconds(),
            resolved.AbsoluteSourceExpiresAtUtc.ToUnixTimeMilliseconds());
        Assert.Equal("FreshAuthorityRequired", resolved.ReuseDisposition);
        Assert.Equal("Forbidden", resolved.ExtensionDisposition);

        Assert.Null(await ResolveAsync(
            fixture with { ClientApplicationId = Guid.NewGuid() },
            evaluatedAt.AddMinutes(1)));
        Assert.Null(await ResolveAsync(
            fixture with { SessionId = Guid.NewGuid() },
            evaluatedAt.AddMinutes(1)));
        Assert.Null(await ResolveAsync(
            fixture with { AcceptanceId = Guid.NewGuid() },
            evaluatedAt.AddMinutes(1)));
        Assert.Null(await ResolveAsync(
            fixture with { RawClass = "ChipDg2Portrait" },
            evaluatedAt.AddMinutes(1)));
        Assert.Null(await ResolveAsync(fixture, evaluatedAt.AddHours(-1)));
        Assert.Null(await ResolveAsync(fixture, validUntil));
    }

    [Fact]
    public async Task C1B2_grant_rejects_unknown_consent_policy_version_binding()
    {
        var fixture = await SeedScopeAsync();
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => AppendGrantAsync(
                fixture,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(2),
                DateTimeOffset.UtcNow.AddHours(1),
                consentPolicyIdOverride: Guid.NewGuid()));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.Equal(
            "fk_raw_export_authority_snapshot_consent_policy",
            exception.ConstraintName);
    }

    [Fact]
    public async Task C1B2_withdraw_and_revoke_make_latest_authority_resolve_none()
    {
        var withdrawnFixture = await SeedScopeAsync();
        var evaluatedAt = DateTimeOffset.UtcNow;
        await AppendGrantAsync(
            withdrawnFixture,
            evaluatedAt,
            evaluatedAt.AddHours(2),
            evaluatedAt.AddHours(1));
        var staleWithdraw = await Assert.ThrowsAsync<PostgresException>(
            () => EndGrantAsync(
                withdrawnFixture,
                999,
                "withdraw"));
        Assert.Equal(PostgresErrorCodes.RaiseException, staleWithdraw.SqlState);
        Assert.Equal(
            "RAW_EXPORT_AUTHORITY_SNAPSHOT_STALE_TARGET_REVISION",
            staleWithdraw.MessageText);
        Assert.Equal(2, await EndGrantAsync(
            withdrawnFixture,
            1,
            "withdraw"));
        Assert.Null(await ResolveAsync(
            withdrawnFixture,
            evaluatedAt.AddMinutes(1)));

        var revokedFixture = await SeedScopeAsync();
        await AppendGrantAsync(
            revokedFixture,
            evaluatedAt,
            evaluatedAt.AddHours(2),
            evaluatedAt.AddHours(1));
        var staleRevoke = await Assert.ThrowsAsync<PostgresException>(
            () => EndGrantAsync(
                revokedFixture,
                999,
                "revoke"));
        Assert.Equal(PostgresErrorCodes.RaiseException, staleRevoke.SqlState);
        Assert.Equal(
            "RAW_EXPORT_AUTHORITY_SNAPSHOT_STALE_TARGET_REVISION",
            staleRevoke.MessageText);
        Assert.Equal(2, await EndGrantAsync(
            revokedFixture,
            1,
            "revoke"));
        Assert.Null(await ResolveAsync(
            revokedFixture,
            evaluatedAt.AddMinutes(1)));
    }

    [Fact]
    public async Task C1B2_latest_grant_after_withdraw_wins_without_reusing_snapshot_identity()
    {
        var fixture = await SeedScopeAsync();
        var evaluatedAt = DateTimeOffset.UtcNow;
        var first = await AppendGrantAsync(
            fixture,
            evaluatedAt,
            evaluatedAt.AddHours(3),
            evaluatedAt.AddHours(1));
        await EndGrantAsync(fixture, first.Revision, "withdraw");
        var third = await AppendGrantAsync(
            fixture,
            evaluatedAt.AddMinutes(1),
            evaluatedAt.AddHours(4),
            evaluatedAt.AddHours(2));

        var resolved = await ResolveAsync(fixture, evaluatedAt.AddMinutes(2));
        Assert.NotNull(resolved);
        Assert.Equal(3, third.Revision);
        Assert.Equal(3, resolved.Revision);
        Assert.Equal(third.AuthoritySnapshotId, resolved.AuthoritySnapshotId);
        Assert.NotEqual(first.AuthoritySnapshotId, resolved.AuthoritySnapshotId);
    }

    [Fact]
    public async Task C1B2_shape_and_closed_fixture_values_are_database_enforced()
    {
        var fixture = await SeedScopeAsync();
        foreach (var mutation in new[]
                 {
                     "granted-null-controller",
                     "granted-target-revision",
                     "invalid-purpose",
                     "invalid-reuse",
                     "invalid-extension",
                 })
        {
            var exception = await AssertInvalidDirectInsertAsync(
                fixture,
                "Granted",
                mutation);
            Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
            Assert.Contains(
                exception.ConstraintName,
                new[]
                {
                    "ck_raw_export_authority_snapshot_event_shape",
                    "ck_raw_export_authority_snapshot_values",
                });
        }

        foreach (var eventType in new[] { "Withdrawn", "Revoked" })
        {
            var missingTarget = await AssertInvalidDirectInsertAsync(
                fixture,
                eventType,
                "sparse-null-target");
            Assert.Equal(PostgresErrorCodes.CheckViolation, missingTarget.SqlState);
            Assert.Equal(
                "ck_raw_export_authority_snapshot_event_shape",
                missingTarget.ConstraintName);

            var frozenField = await AssertInvalidDirectInsertAsync(
                fixture,
                eventType,
                "sparse-frozen-controller");
            Assert.Equal(PostgresErrorCodes.CheckViolation, frozenField.SqlState);
            Assert.Equal(
                "ck_raw_export_authority_snapshot_event_shape",
                frozenField.ConstraintName);
        }
    }

    [Fact]
    public async Task C1B2_append_only_direct_insert_and_actor_guards_fail_closed()
    {
        var fixture = await SeedScopeAsync();
        var grant = await AppendGrantAsync(
            fixture,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(2),
            DateTimeOffset.UtcNow.AddHours(1));

        await using var connection = await OpenAsync();
        foreach (var statement in new[]
                 {
                     $"""
                     UPDATE tagekyc.raw_export_authority_snapshots
                     SET "ControllerIdentity" = 'changed'
                     WHERE "AuthoritySnapshotId" = '{grant.AuthoritySnapshotId}';
                     """,
                     $"""
                     DELETE FROM tagekyc.raw_export_authority_snapshots
                     WHERE "AuthoritySnapshotId" = '{grant.AuthoritySnapshotId}';
                     """,
                 })
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => ExecuteAsync(connection, statement));
            Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
            Assert.Contains("append-only table", exception.MessageText);
        }

        var direct = await Assert.ThrowsAsync<PostgresException>(
            () => ExecuteAsync(
                connection,
                ValidGrantedInsertSql(
                    fixture,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    2)));
        Assert.Equal(PostgresErrorCodes.RaiseException, direct.SqlState);
        Assert.Equal(
            "RAW_EXPORT_AUTHORITY_SNAPSHOT_DIRECT_INSERT_UNSUPPORTED",
            direct.MessageText);

        var actorMissing = await Assert.ThrowsAsync<PostgresException>(
            () => AppendGrantAsync(
                fixture,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(2),
                DateTimeOffset.UtcNow.AddHours(1),
                setActor: false));
        Assert.Equal(PostgresErrorCodes.RaiseException, actorMissing.SqlState);
        Assert.Equal("RAW_EXPORT_ACTOR_CONTEXT_MISSING", actorMissing.MessageText);

        var bindingMismatch = await Assert.ThrowsAsync<PostgresException>(
            () => AppendGrantAsync(
                fixture with { ClientApplicationId = Guid.NewGuid() },
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(2),
                DateTimeOffset.UtcNow.AddHours(1)));
        Assert.Equal(
            PostgresErrorCodes.RaiseException,
            bindingMismatch.SqlState);
        Assert.Equal(
            "RAW_EXPORT_AUTHORITY_SNAPSHOT_BINDING_INVALID",
            bindingMismatch.MessageText);

        var actorMismatch = await Assert.ThrowsAsync<PostgresException>(
            () => EndGrantAsync(
                fixture,
                grant.Revision,
                "withdraw",
                declaredActor: Guid.NewGuid()));
        Assert.Equal(PostgresErrorCodes.RaiseException, actorMismatch.SqlState);
        Assert.Equal(
            "RAW_EXPORT_AUTHORITY_SNAPSHOT_ACTOR_MISMATCH",
            actorMismatch.MessageText);
    }

    [Fact]
    public async Task C1B2_readiness_profile_is_exact_and_fail_closed()
    {
        await AssertReadinessFailureAsync(
            null,
            isProduction: false,
            RawExportAuthoritySnapshotReadinessValidator.ProfileMissing);
        await AssertReadinessFailureAsync(
            " ",
            isProduction: false,
            RawExportAuthoritySnapshotReadinessValidator.ProfileMissing);
        await AssertReadinessFailureAsync(
            "Unknown",
            isProduction: false,
            RawExportAuthoritySnapshotReadinessValidator.ProfileInvalid);
        await AssertReadinessFailureAsync(
            "Fixture",
            isProduction: true,
            RawExportAuthoritySnapshotReadinessValidator.FixtureActive);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [RawExportAuthoritySnapshotProfileState.ConfigurationPath] =
                    "Fixture",
            })
            .Build();
        var state = RawExportAuthoritySnapshotProfileState.Resolve(
            configuration,
            isProduction: false);
        await new RawExportAuthoritySnapshotReadinessValidator(state)
            .ValidateAsync(CancellationToken.None);
    }

    private async Task<AuthorityScope> SeedScopeAsync()
    {
        var sessionId = Guid.NewGuid();
        var clientApplicationId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var db = postgres.CreateDbContext();
        db.Sessions.Add(new VerificationSessionRow
        {
            Id = sessionId,
            ClientApplicationId = clientApplicationId,
            SubjectRef = $"c1b2-subject-{Guid.NewGuid():N}",
            Profile = "ChallengeBoundEkycProfile",
            Purpose = "raw-export",
            RequiredChecksJson = "[\"DocumentNfc\"]",
            BindingNonceHash = "caller-owned-challenge",
            RequestId = Guid.NewGuid().ToString("N"),
            CorrelationId = Guid.NewGuid().ToString("N"),
            State = "Completed",
            Result = "Passed",
            AssuranceLevel = "Substantial",
            PolicySnapshotId = "c1b2-policy",
            RetentionClass = "Standard",
            DeletionEligibility = "Pending",
            LegalHoldStatus = "None",
            PurgeBlockReason = "None",
            ExpiresAt = now.AddHours(4),
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
            ExpiresAt = now.AddHours(4),
        });
        await db.SaveChangesAsync();

        var acceptanceId = await AppendAcceptanceAsync(
            sessionId,
            clientApplicationId,
            artifactId);
        return new(
            clientApplicationId,
            sessionId,
            acceptanceId,
            "LiveSelfieImage");
    }

    private async Task<Guid> AppendAcceptanceAsync(
        Guid sessionId,
        Guid clientApplicationId,
        Guid artifactId)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, Guid.NewGuid());
        await using var command = new NpgsqlCommand(
            """
            SELECT tagekyc.raw_export_append_capture_acceptance(
                @sessionId,
                @clientApplicationId,
                'LiveSelfieImage',
                @artifactId,
                1,
                'sha256:caller-computed-session-challenge',
                'evidence:c1b2',
                'acceptance-policy:c1b2',
                1);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue(
            "clientApplicationId",
            clientApplicationId);
        command.Parameters.AddWithValue("artifactId", artifactId);
        var result = (Guid)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException(
                "Acceptance function returned null."));
        await transaction.CommitAsync();
        return result;
    }

    private async Task<GrantedAuthority> AppendGrantAsync(
        AuthorityScope scope,
        DateTimeOffset evaluatedAt,
        DateTimeOffset absoluteSourceExpiresAt,
        DateTimeOffset? validUntil,
        bool setActor = true,
        Guid? consentPolicyIdOverride = null)
    {
        var consentPolicyId =
            consentPolicyIdOverride ?? await SeedCoreConsentPolicyAsync();
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        if (setActor)
        {
            await SetActorAsync(connection, transaction, Guid.NewGuid());
        }

        await using var command = new NpgsqlCommand(
            """
            SELECT "Revision", "AuthoritySnapshotId"
            FROM tagekyc.raw_export_append_authority_snapshot(
                @clientApplicationId,
                @sessionId,
                @acceptanceId,
                @rawClass,
                @authorityArtifactId,
                1,
                'controller:fixture',
                'scope:fixture',
                'retention-policy:fixture',
                1,
                @consentPolicyId,
                1,
                'RawBiometric',
                'CaptureAccepted',
                @absoluteSourceExpiresAt,
                'revocation-policy:fixture',
                'purge-policy:fixture',
                'legal-hold-policy:fixture',
                @evaluatedAt,
                @validUntil);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue(
            "clientApplicationId",
            scope.ClientApplicationId);
        command.Parameters.AddWithValue("sessionId", scope.SessionId);
        command.Parameters.AddWithValue("acceptanceId", scope.AcceptanceId);
        command.Parameters.AddWithValue("rawClass", scope.RawClass);
        command.Parameters.AddWithValue("authorityArtifactId", Guid.NewGuid());
        command.Parameters.AddWithValue("consentPolicyId", consentPolicyId);
        command.Parameters.AddWithValue(
            "absoluteSourceExpiresAt",
            absoluteSourceExpiresAt);
        command.Parameters.AddWithValue("evaluatedAt", evaluatedAt);
        command.Parameters.AddWithValue(
            "validUntil",
            validUntil.HasValue
                ? validUntil.Value
                : DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var result = new GrantedAuthority(
            reader.GetInt64(0),
            reader.GetGuid(1));
        await reader.CloseAsync();
        await transaction.CommitAsync();
        return result;
    }

    private async Task<long> EndGrantAsync(
        AuthorityScope scope,
        long targetRevision,
        string eventType,
        Guid? declaredActor = null)
    {
        var actor = Guid.NewGuid();
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetActorAsync(connection, transaction, actor);
        var function = eventType == "withdraw"
            ? "raw_export_withdraw_authority_snapshot"
            : "raw_export_revoke_authority_snapshot";
        await using var command = new NpgsqlCommand(
            $"""
            SELECT tagekyc.{function}(
                @clientApplicationId,
                @sessionId,
                @acceptanceId,
                @rawClass,
                @targetRevision,
                @actor);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue(
            "clientApplicationId",
            scope.ClientApplicationId);
        command.Parameters.AddWithValue("sessionId", scope.SessionId);
        command.Parameters.AddWithValue("acceptanceId", scope.AcceptanceId);
        command.Parameters.AddWithValue("rawClass", scope.RawClass);
        command.Parameters.AddWithValue("targetRevision", targetRevision);
        command.Parameters.AddWithValue("actor", declaredActor ?? actor);
        var result = (long)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException(
                "Authority terminal event function returned null."));
        await transaction.CommitAsync();
        return result;
    }

    private async Task<ResolvedAuthority?> ResolveAsync(
        AuthorityScope scope,
        DateTimeOffset evaluatedAt)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteAsync(
            connection,
            transaction,
            "SET LOCAL ROLE tagekyc_runtime;");
        await using var command = new NpgsqlCommand(
            """
            SELECT
                "Revision",
                "AuthoritySnapshotId",
                "ControllerIdentity",
                "ApprovedPurpose",
                "StableDataScopeId",
                "ConsentPolicyId",
                "ConsentPolicyVersion",
                "AbsoluteSourceExpiresAtUtc",
                "ReuseDisposition",
                "ExtensionDisposition"
            FROM tagekyc.raw_export_resolve_current_authority_for_source(
                @clientApplicationId,
                @sessionId,
                @acceptanceId,
                @rawClass,
                @evaluatedAt);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue(
            "clientApplicationId",
            scope.ClientApplicationId);
        command.Parameters.AddWithValue("sessionId", scope.SessionId);
        command.Parameters.AddWithValue("acceptanceId", scope.AcceptanceId);
        command.Parameters.AddWithValue("rawClass", scope.RawClass);
        command.Parameters.AddWithValue("evaluatedAt", evaluatedAt);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            await reader.CloseAsync();
            await transaction.RollbackAsync();
            return null;
        }

        var result = new ResolvedAuthority(
            reader.GetInt64(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetGuid(5),
            reader.GetInt32(6),
            reader.GetFieldValue<DateTimeOffset>(7),
            reader.GetString(8),
            reader.GetString(9));
        await reader.CloseAsync();
        await transaction.RollbackAsync();
        return result;
    }

    private async Task<PostgresException> AssertInvalidDirectInsertAsync(
        AuthorityScope scope,
        string eventType,
        string mutation)
    {
        var consentPolicyId = await SeedCoreConsentPolicyAsync();
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteAsync(
            connection,
            transaction,
            """
            ALTER TABLE tagekyc.raw_export_authority_snapshots
            DISABLE TRIGGER
                tr_raw_export_authority_snapshots_insert_guard;
            """);
        var insert = eventType == "Granted"
            ? ValidGrantedInsertSql(
                scope,
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                consentPolicyId)
            : SparseEventInsertSql(
                scope,
                eventType,
                targetRevision: mutation == "sparse-null-target"
                    ? null
                    : 1,
                controllerIdentity:
                    mutation == "sparse-frozen-controller"
                        ? "must-be-null"
                        : null);
        insert = mutation switch
        {
            "granted-null-controller" => insert.Replace(
                "'controller:fixture','SubjectRawBiometricExport'",
                "NULL,'SubjectRawBiometricExport'",
                StringComparison.Ordinal),
            "granted-target-revision" => insert
                .Replace(
                    "\"AuthoritySnapshotEventId\",\"EventType\",\"Revision\",\n",
                    "\"AuthoritySnapshotEventId\",\"EventType\",\"Revision\",\"TargetRevision\",\n",
                    StringComparison.Ordinal)
                .Replace(
                    $"'Granted',1,\n",
                    "'Granted',1,1,\n",
                    StringComparison.Ordinal),
            "invalid-purpose" => insert.Replace(
                "'SubjectRawBiometricExport'",
                "'OtherPurpose'",
                StringComparison.Ordinal),
            "invalid-reuse" => insert.Replace(
                "'FreshAuthorityRequired'",
                "'Reusable'",
                StringComparison.Ordinal),
            "invalid-extension" => insert.Replace(
                "'Forbidden'",
                "'Allowed'",
                StringComparison.Ordinal),
            "sparse-null-target" => insert,
            "sparse-frozen-controller" => insert,
            _ => throw new ArgumentOutOfRangeException(
                nameof(mutation),
                mutation,
                "Unknown direct-insert mutation."),
        };
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => ExecuteAsync(
                connection,
                transaction,
                insert));
        await transaction.RollbackAsync();
        return exception;
    }

    private static string ValidGrantedInsertSql(
        AuthorityScope scope,
        Guid eventId,
        Guid snapshotId,
        long revision,
        Guid? consentPolicyId = null) =>
        $"""
        INSERT INTO tagekyc.raw_export_authority_snapshots
            ("AuthoritySnapshotEventId","EventType","Revision",
             "ValidFromUtc","RecordedAtUtc","CapturedByPrincipalId",
             "ClientApplicationId","VerificationSessionId",
             "CaptureAcceptanceId","RawClass",
             "AuthoritySnapshotSchemaVersion","AuthoritySnapshotId",
             "AuthorityArtifactId","AuthorityArtifactVersion",
             "ControllerIdentity","ApprovedPurpose","StableDataScopeId",
             "RetentionPolicyId","RetentionPolicyVersion",
             "ConsentPolicyId","ConsentPolicyVersion","RetentionClass",
             "RetentionStartEvent","AbsoluteSourceExpiresAtUtc",
             "ReuseDisposition","ExtensionDisposition",
             "RevocationPolicyId","PurgePolicyId","LegalHoldPolicyId",
             "EvaluatedAtUtc")
        VALUES
            ('{eventId}','Granted',{revision},
             pg_catalog.transaction_timestamp(),
             pg_catalog.transaction_timestamp(),'{Guid.NewGuid()}',
             '{scope.ClientApplicationId}','{scope.SessionId}',
             '{scope.AcceptanceId}','{scope.RawClass}',
             1,'{snapshotId}',
             '{Guid.NewGuid()}',1,
             'controller:fixture','SubjectRawBiometricExport','scope:fixture',
             'retention-policy:fixture',1,
             '{consentPolicyId ?? Guid.NewGuid()}',1,'RawBiometric',
             'CaptureAccepted',pg_catalog.transaction_timestamp() + interval '2 hours',
             'FreshAuthorityRequired','Forbidden',
             'revocation-policy:fixture','purge-policy:fixture',
             'legal-hold-policy:fixture',pg_catalog.transaction_timestamp());
        """;

    private static string SparseEventInsertSql(
        AuthorityScope scope,
        string eventType,
        long? targetRevision,
        string? controllerIdentity)
    {
        var actorColumn = eventType == "Withdrawn"
            ? "\"WithdrawnByPrincipalId\""
            : "\"RevokedByPrincipalId\"";
        var frozenColumn = controllerIdentity is null
            ? string.Empty
            : ",\"ControllerIdentity\"";
        var frozenValue = controllerIdentity is null
            ? string.Empty
            : $",'{controllerIdentity}'";
        var targetValue = targetRevision?.ToString(
            System.Globalization.CultureInfo.InvariantCulture) ?? "NULL";
        return
            $"""
            INSERT INTO tagekyc.raw_export_authority_snapshots
                ("AuthoritySnapshotEventId","EventType","Revision",
                 "TargetRevision","RecordedAtUtc"{frozenColumn},{actorColumn},
                 "ClientApplicationId","VerificationSessionId",
                 "CaptureAcceptanceId","RawClass")
            VALUES
                ('{Guid.NewGuid()}','{eventType}',2,
                 {targetValue},pg_catalog.transaction_timestamp(){frozenValue},'{Guid.NewGuid()}',
                 '{scope.ClientApplicationId}','{scope.SessionId}',
                 '{scope.AcceptanceId}','{scope.RawClass}');
            """;
    }

    private static async Task AssertReadinessFailureAsync(
        string? profile,
        bool isProduction,
        string expectedCode)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [RawExportAuthoritySnapshotProfileState.ConfigurationPath] =
                    profile,
            })
            .Build();
        var state = RawExportAuthoritySnapshotProfileState.Resolve(
            configuration,
            isProduction);
        var exception =
            await Assert.ThrowsAsync<RawExportAuthoritySnapshotReadinessException>(
                () => new RawExportAuthoritySnapshotReadinessValidator(state)
                    .ValidateAsync(CancellationToken.None));
        Assert.Equal(expectedCode, exception.Code);
        Assert.Equal(expectedCode, exception.Message);
    }

    private static async Task SetActorAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid actor) =>
        await ExecuteAsync(
            connection,
            transaction,
            """
            SELECT pg_catalog.set_config(
                'tagekyc.actor_principal_id',
                @actor,
                true);
            """,
            new NpgsqlParameter("actor", actor.ToString("D")));

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private async Task<Guid> SeedCoreConsentPolicyAsync()
    {
        var policyId = Guid.NewGuid();
        await using var db = postgres.CreateDbContext();
        var repository = new EfRawExportPolicyRepository(db);
        var policy = await repository.AddVersionAsync(
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
        Assert.Equal(1, policy.PolicyVersion);
        Assert.Equal(
            RawExportPolicyConstants.RequirementRuleSetId,
            policy.RequirementRuleSetId);
        Assert.Equal(1, policy.RequirementRuleSetVersion);
        return policyId;
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
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

    private static async Task<string> ScalarStringAsync(
        NpgsqlConnection connection,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return (string)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException(
                "Catalog scalar query returned null."));
    }

    private static async Task<bool> HasFunctionPrivilegeAsync(
        NpgsqlConnection connection,
        string role,
        string function,
        string privilege)
    {
        await using var command = new NpgsqlCommand(
            "SELECT pg_catalog.has_function_privilege(@role, @function, @privilege);",
            connection);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("function", function);
        command.Parameters.AddWithValue("privilege", privilege);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException(
                "Function privilege query returned null."));
    }

    private static async Task<bool> HasTablePrivilegeAsync(
        NpgsqlConnection connection,
        string role,
        string table,
        string privilege)
    {
        await using var command = new NpgsqlCommand(
            "SELECT pg_catalog.has_table_privilege(@role, @table, @privilege);",
            connection);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("privilege", privilege);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException(
                "Table privilege query returned null."));
    }

    private sealed record AuthorityScope(
        Guid ClientApplicationId,
        Guid SessionId,
        Guid AcceptanceId,
        string RawClass);

    private sealed record GrantedAuthority(
        long Revision,
        Guid AuthoritySnapshotId);

    private sealed record ResolvedAuthority(
        long Revision,
        Guid AuthoritySnapshotId,
        string ControllerIdentity,
        string ApprovedPurpose,
        string StableDataScopeId,
        Guid ConsentPolicyId,
        int ConsentPolicyVersion,
        DateTimeOffset AbsoluteSourceExpiresAtUtc,
        string ReuseDisposition,
        string ExtensionDisposition);
}
