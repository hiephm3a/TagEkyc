using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1AAcceptanceSurfaceTests(PostgresPersistenceFixture postgres)
{
    private static readonly string[] Tables =
    [
        "raw_export_capture_acceptance_events",
        "raw_export_session_capture_selections",
    ];

    private static readonly string[] Constraints =
    [
        "pk_raw_export_capture_acceptance_events",
        "uq_raw_export_capture_acceptance_revision",
        "uq_raw_export_capture_acceptance_artifact",
        "pk_raw_export_session_capture_selections",
        "uq_raw_export_session_capture_selection_class",
        "fk_raw_export_capture_acceptance_session",
        "fk_raw_export_capture_acceptance_artifact",
        "fk_raw_export_session_selection_acceptance",
    ];

    private static readonly string[] Indexes =
    [
        "IX_raw_export_capture_acceptance_events_CaptureArtifactId",
        "IX_raw_export_session_capture_selections_CaptureAcceptanceId",
    ];

    private static readonly string[] Triggers =
    [
        "tr_raw_export_capture_acceptance_events_insert_guard",
        "tr_raw_export_session_capture_selections_insert_guard",
        "tr_raw_export_capture_acceptance_events_append_only",
        "tr_raw_export_session_capture_selections_append_only",
    ];

    private static readonly string[] Functions =
    [
        "enforce_raw_export_capture_acceptance_insert",
        "raw_export_append_capture_acceptance",
        "raw_export_select_session_capture_acceptance",
    ];

    private static readonly string[] Columns =
    [
        "raw_export_capture_acceptance_events.AcceptancePolicyId",
        "raw_export_capture_acceptance_events.AcceptancePolicyVersion",
        "raw_export_capture_acceptance_events.AcceptedAtUtc",
        "raw_export_capture_acceptance_events.AcceptedEvidenceRef",
        "raw_export_capture_acceptance_events.CaptureAcceptanceId",
        "raw_export_capture_acceptance_events.CaptureArtifactId",
        "raw_export_capture_acceptance_events.CaptureRevision",
        "raw_export_capture_acceptance_events.ClientApplicationId",
        "raw_export_capture_acceptance_events.RawClass",
        "raw_export_capture_acceptance_events.SessionChallengeHash",
        "raw_export_capture_acceptance_events.VerificationSessionId",
        "raw_export_session_capture_selections.CaptureAcceptanceId",
        "raw_export_session_capture_selections.RawClass",
        "raw_export_session_capture_selections.SessionCaptureSelectionId",
        "raw_export_session_capture_selections.VerificationSessionId",
    ];

    [Fact]
    public async Task C1A_identifiers_round_trip_exactly_and_acl_is_minimal()
    {
        await using var connection = await OpenAsync();

        Assert.Equal(
            Tables.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(connection, """
                SELECT relation.relname
                FROM pg_catalog.pg_class AS relation
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = ANY(@names);
                """, new NpgsqlParameter("names", Tables))).Order(StringComparer.Ordinal));

        Assert.Equal(
            Constraints.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(connection, """
                SELECT constraint_row.conname
                FROM pg_catalog.pg_constraint AS constraint_row
                JOIN pg_catalog.pg_class AS relation
                  ON relation.oid = constraint_row.conrelid
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = ANY(@tables)
                  AND constraint_row.contype <> 't';
                """, new NpgsqlParameter("tables", Tables))).Order(StringComparer.Ordinal));

        Assert.Equal(
            Indexes.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(connection, """
                SELECT relation.relname
                FROM pg_catalog.pg_class AS relation
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relkind = 'i'
                  AND relation.relname = ANY(@names);
                """, new NpgsqlParameter("names", Indexes))).Order(StringComparer.Ordinal));

        Assert.Equal(
            Triggers.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(connection, """
                SELECT trigger_row.tgname
                FROM pg_catalog.pg_trigger AS trigger_row
                JOIN pg_catalog.pg_class AS relation
                  ON relation.oid = trigger_row.tgrelid
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = ANY(@tables)
                  AND NOT trigger_row.tgisinternal;
                """, new NpgsqlParameter("tables", Tables))).Order(StringComparer.Ordinal));

        Assert.Equal(
            Functions.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(connection, """
                SELECT function_row.proname
                FROM pg_catalog.pg_proc AS function_row
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = function_row.pronamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND function_row.proname = ANY(@names);
                """, new NpgsqlParameter("names", Functions))).Order(StringComparer.Ordinal));

        Assert.Equal(
            Columns.Order(StringComparer.Ordinal),
            (await QueryStringsAsync(connection, """
                SELECT relation.relname || '.' || attribute.attname
                FROM pg_catalog.pg_attribute AS attribute
                JOIN pg_catalog.pg_class AS relation
                  ON relation.oid = attribute.attrelid
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = ANY(@tables)
                  AND attribute.attnum > 0
                  AND NOT attribute.attisdropped;
                """, new NpgsqlParameter("tables", Tables))).Order(StringComparer.Ordinal));

        var identifiers = Tables.Concat(Constraints).Concat(Indexes).Concat(Triggers)
            .Concat(Functions)
            .Concat(Columns.Select(column => column[(column.IndexOf('.') + 1)..]))
            .ToArray();
        Assert.All(
            identifiers,
            identifier => Assert.InRange(Encoding.UTF8.GetByteCount(identifier), 1, 63));

        await using (var command = new NpgsqlCommand(
            """
            SELECT function_row.proname,
                   owner.rolname,
                   function_row.prosecdef,
                   COALESCE(array_to_string(function_row.proconfig, ','), ''),
                   has_function_privilege('public', function_row.oid, 'EXECUTE'),
                   has_function_privilege('tagekyc_runtime', function_row.oid, 'EXECUTE')
            FROM pg_catalog.pg_proc AS function_row
            JOIN pg_catalog.pg_namespace AS namespace
              ON namespace.oid = function_row.pronamespace
            JOIN pg_catalog.pg_roles AS owner
              ON owner.oid = function_row.proowner
            WHERE namespace.nspname = 'tagekyc'
              AND function_row.proname = ANY(@names)
            ORDER BY function_row.proname;
            """,
            connection))
        {
            command.Parameters.AddWithValue("names", Functions);
            await using var reader = await command.ExecuteReaderAsync();
            var seen = new List<string>();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                seen.Add(name);
                Assert.Equal("tagekyc_raw_export_deployer", reader.GetString(1));
                Assert.Equal(
                    name != "enforce_raw_export_capture_acceptance_insert",
                    reader.GetBoolean(2));
                Assert.Equal("search_path=pg_catalog", reader.GetString(3));
                Assert.False(reader.GetBoolean(4));
                Assert.Equal(
                    name != "enforce_raw_export_capture_acceptance_insert",
                    reader.GetBoolean(5));
            }

            Assert.Equal(Functions.Order(StringComparer.Ordinal), seen);
        }

        foreach (var table in Tables)
        {
            foreach (var privilege in new[]
                     {
                         "SELECT", "INSERT", "UPDATE", "DELETE", "TRUNCATE",
                         "REFERENCES", "TRIGGER",
                     })
            {
                Assert.False(Convert.ToBoolean(await ScalarAsync(
                    connection,
                    "SELECT has_table_privilege('tagekyc_runtime', @table, @privilege);",
                    new NpgsqlParameter("table", $"tagekyc.{table}"),
                    new NpgsqlParameter("privilege", privilege))));
            }
        }

        Assert.Equal(
            0L,
            Convert.ToInt64(await ScalarAsync(
                connection,
                """
                SELECT count(*)
                FROM pg_catalog.pg_attribute AS attribute
                JOIN pg_catalog.pg_class AS relation
                  ON relation.oid = attribute.attrelid
                JOIN pg_catalog.pg_namespace AS namespace
                  ON namespace.oid = relation.relnamespace
                CROSS JOIN LATERAL pg_catalog.aclexplode(attribute.attacl) AS acl
                WHERE namespace.nspname = 'tagekyc'
                  AND relation.relname = ANY(@tables)
                  AND acl.grantee = 'tagekyc_runtime'::regrole::oid;
                """,
                new NpgsqlParameter("tables", Tables))));
    }

    [Fact]
    public async Task C1A_both_tables_reject_update_and_delete_even_as_owner()
    {
        var fixture = await SeedSessionAndArtifactAsync();
        var acceptanceId = await AppendAcceptanceAsync(
            fixture.SessionId,
            fixture.ClientApplicationId,
            fixture.ArtifactId,
            1);
        await SelectAcceptanceAsync(fixture.SessionId, "LiveSelfieImage", acceptanceId);

        await using var connection = await OpenAsync();
        foreach (var statement in new[]
                 {
                     $"""UPDATE tagekyc.raw_export_capture_acceptance_events SET "AcceptedEvidenceRef"='changed' WHERE "CaptureAcceptanceId"='{acceptanceId}';""",
                     $"""DELETE FROM tagekyc.raw_export_capture_acceptance_events WHERE "CaptureAcceptanceId"='{acceptanceId}';""",
                     $"""UPDATE tagekyc.raw_export_session_capture_selections SET "RawClass"='LivenessMedia' WHERE "CaptureAcceptanceId"='{acceptanceId}';""",
                     $"""DELETE FROM tagekyc.raw_export_session_capture_selections WHERE "CaptureAcceptanceId"='{acceptanceId}';""",
                 })
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(
                () => ExecuteAsync(connection, statement));
            Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
            Assert.Contains("append-only table", exception.MessageText, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task C1A_selection_is_first_only_and_requires_existing_acceptance()
    {
        var fixture = await SeedSessionAndArtifactAsync();
        var acceptanceId = await AppendAcceptanceAsync(
            fixture.SessionId,
            fixture.ClientApplicationId,
            fixture.ArtifactId,
            1);
        await SelectAcceptanceAsync(fixture.SessionId, "LiveSelfieImage", acceptanceId);

        var duplicate = await Assert.ThrowsAsync<PostgresException>(
            () => SelectAcceptanceAsync(
                fixture.SessionId,
                "LiveSelfieImage",
                acceptanceId));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
        Assert.Equal(
            "uq_raw_export_session_capture_selection_class",
            duplicate.ConstraintName);

        var mismatch = await Assert.ThrowsAsync<PostgresException>(
            () => SelectAcceptanceAsync(
                fixture.SessionId,
                "ChipDg2Portrait",
                acceptanceId));
        Assert.Equal(PostgresErrorCodes.RaiseException, mismatch.SqlState);
        Assert.Equal(
            "RAW_EXPORT_SESSION_CAPTURE_SELECTION_MISMATCH",
            mismatch.MessageText);

        var missing = await Assert.ThrowsAsync<PostgresException>(
            () => SelectAcceptanceAsync(
                Guid.NewGuid(),
                "ChipDg2Portrait",
                Guid.NewGuid()));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, missing.SqlState);
        Assert.Equal(
            "fk_raw_export_session_selection_acceptance",
            missing.ConstraintName);
    }

    [Fact]
    public async Task C1A_fk_actor_revision_and_direct_insert_guards_fail_closed()
    {
        var fixture = await SeedSessionAndArtifactAsync();

        var artifactSessionMismatch = await Assert.ThrowsAsync<PostgresException>(
            () => AppendAcceptanceAsync(
                Guid.NewGuid(),
                fixture.ClientApplicationId,
                fixture.ArtifactId,
                1));
        Assert.Equal(PostgresErrorCodes.RaiseException, artifactSessionMismatch.SqlState);
        Assert.Equal(
            "RAW_EXPORT_CAPTURE_ARTIFACT_SESSION_MISMATCH",
            artifactSessionMismatch.MessageText);

        var missingSession = await AssertDirectEventInsertFailureAsync(
            Guid.NewGuid(),
            fixture.ClientApplicationId,
            fixture.ArtifactId,
            1);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, missingSession.SqlState);
        Assert.Equal("fk_raw_export_capture_acceptance_session", missingSession.ConstraintName);

        var missingArtifact = await AssertDirectEventInsertFailureAsync(
            fixture.SessionId,
            fixture.ClientApplicationId,
            Guid.NewGuid(),
            1);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, missingArtifact.SqlState);
        Assert.Equal("fk_raw_export_capture_acceptance_artifact", missingArtifact.ConstraintName);

        await using (var connection = await OpenAsync())
        {
            var direct = await Assert.ThrowsAsync<PostgresException>(
                () => ExecuteAsync(
                    connection,
                    DirectEventInsertSql(
                        Guid.NewGuid(),
                        fixture.SessionId,
                        fixture.ClientApplicationId,
                        fixture.ArtifactId,
                        1)));
            Assert.Equal(PostgresErrorCodes.RaiseException, direct.SqlState);
            Assert.Equal(
                "RAW_EXPORT_CAPTURE_ACCEPTANCE_DIRECT_INSERT_UNSUPPORTED",
                direct.MessageText);

            var directSelection = await Assert.ThrowsAsync<PostgresException>(
                () => ExecuteAsync(
                    connection,
                    $"""
                    INSERT INTO tagekyc.raw_export_session_capture_selections
                        ("SessionCaptureSelectionId",
                         "VerificationSessionId",
                         "RawClass",
                         "CaptureAcceptanceId")
                    VALUES
                        ('{Guid.NewGuid()}',
                         '{fixture.SessionId}',
                         'LiveSelfieImage',
                         '{Guid.NewGuid()}');
                    """));
            Assert.Equal(PostgresErrorCodes.RaiseException, directSelection.SqlState);
            Assert.Equal(
                "RAW_EXPORT_CAPTURE_ACCEPTANCE_DIRECT_INSERT_UNSUPPORTED",
                directSelection.MessageText);
        }

        var actorMissing = await Assert.ThrowsAsync<PostgresException>(
            () => AppendAcceptanceAsync(
                fixture.SessionId,
                fixture.ClientApplicationId,
                fixture.ArtifactId,
                1,
                setActor: false));
        Assert.Equal(PostgresErrorCodes.RaiseException, actorMissing.SqlState);
        Assert.Equal("RAW_EXPORT_ACTOR_CONTEXT_MISSING", actorMissing.MessageText);

        var nonPositive = await Assert.ThrowsAsync<PostgresException>(
            () => AppendAcceptanceAsync(
                fixture.SessionId,
                fixture.ClientApplicationId,
                fixture.ArtifactId,
                0));
        Assert.Equal(PostgresErrorCodes.RaiseException, nonPositive.SqlState);
        Assert.Equal(
            "RAW_EXPORT_CAPTURE_ACCEPTANCE_REVISION_CONFLICT",
            nonPositive.MessageText);

        var skipped = await Assert.ThrowsAsync<PostgresException>(
            () => AppendAcceptanceAsync(
                fixture.SessionId,
                fixture.ClientApplicationId,
                fixture.ArtifactId,
                2));
        Assert.Equal(PostgresErrorCodes.RaiseException, skipped.SqlState);
        Assert.Equal(
            "RAW_EXPORT_CAPTURE_ACCEPTANCE_REVISION_CONFLICT",
            skipped.MessageText);

        var accepted = await AppendAcceptanceAsync(
            fixture.SessionId,
            fixture.ClientApplicationId,
            fixture.ArtifactId,
            1);
        Assert.NotEqual(Guid.Empty, accepted);

        var selectionActorMissing = await Assert.ThrowsAsync<PostgresException>(
            () => SelectAcceptanceAsync(
                fixture.SessionId,
                "LiveSelfieImage",
                accepted,
                setActor: false));
        Assert.Equal(PostgresErrorCodes.RaiseException, selectionActorMissing.SqlState);
        Assert.Equal("RAW_EXPORT_ACTOR_CONTEXT_MISSING", selectionActorMissing.MessageText);
    }

    private async Task<SeededCapture> SeedSessionAndArtifactAsync()
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
            SubjectRef = $"c1a-subject-{Guid.NewGuid():N}",
            Profile = "ChallengeBoundEkycProfile",
            Purpose = "raw-export",
            RequiredChecksJson = "[\"DocumentNfc\"]",
            BindingNonceHash = "caller-owned-challenge",
            RequestId = Guid.NewGuid().ToString("N"),
            CorrelationId = Guid.NewGuid().ToString("N"),
            State = "Completed",
            Result = "Passed",
            AssuranceLevel = "Substantial",
            PolicySnapshotId = "c1a-policy",
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

        return new SeededCapture(sessionId, clientApplicationId, artifactId);
    }

    private async Task<Guid> AppendAcceptanceAsync(
        Guid sessionId,
        Guid clientApplicationId,
        Guid artifactId,
        int revision,
        bool setActor = true)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        if (setActor)
        {
            await ExecuteAsync(
                connection,
                transaction,
                "SELECT pg_catalog.set_config('tagekyc.actor_principal_id', @actor, true);",
                new NpgsqlParameter("actor", Guid.NewGuid().ToString("D")));
        }

        await using var command = new NpgsqlCommand(
            """
            SELECT tagekyc.raw_export_append_capture_acceptance(
                @sessionId,
                @clientApplicationId,
                'LiveSelfieImage',
                @artifactId,
                @revision,
                'sha256:caller-computed-session-challenge',
                'evidence:c1a',
                'acceptance-policy:c1a',
                1);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue("clientApplicationId", clientApplicationId);
        command.Parameters.AddWithValue("artifactId", artifactId);
        command.Parameters.AddWithValue("revision", revision);
        var result = (Guid)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Acceptance function returned null."));
        await transaction.CommitAsync();
        return result;
    }

    private async Task<Guid> SelectAcceptanceAsync(
        Guid sessionId,
        string rawClass,
        Guid acceptanceId,
        bool setActor = true)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        if (setActor)
        {
            await ExecuteAsync(
                connection,
                transaction,
                "SELECT pg_catalog.set_config('tagekyc.actor_principal_id', @actor, true);",
                new NpgsqlParameter("actor", Guid.NewGuid().ToString("D")));
        }

        await using var command = new NpgsqlCommand(
            """
            SELECT tagekyc.raw_export_select_session_capture_acceptance(
                @sessionId,
                @rawClass,
                @acceptanceId);
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("sessionId", sessionId);
        command.Parameters.AddWithValue("rawClass", rawClass);
        command.Parameters.AddWithValue("acceptanceId", acceptanceId);
        var result = (Guid)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Selection function returned null."));
        await transaction.CommitAsync();
        return result;
    }

    private async Task<PostgresException> AssertDirectEventInsertFailureAsync(
        Guid sessionId,
        Guid clientApplicationId,
        Guid artifactId,
        int revision)
    {
        await using var connection = await OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteAsync(connection, transaction, "SET LOCAL ROLE tagekyc_raw_export_deployer;");
        await ExecuteAsync(
            connection,
            transaction,
            """
            SELECT pg_catalog.set_config(
                'tagekyc.raw_export_capture_acceptance_append_context',
                'event',
                true);
            """);
        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => ExecuteAsync(
                connection,
                transaction,
                DirectEventInsertSql(
                    Guid.NewGuid(),
                    sessionId,
                    clientApplicationId,
                    artifactId,
                    revision)));
        await transaction.RollbackAsync();
        return exception;
    }

    private static string DirectEventInsertSql(
        Guid acceptanceId,
        Guid sessionId,
        Guid clientApplicationId,
        Guid artifactId,
        int revision) =>
        $"""
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
            ('{acceptanceId}',
             '{sessionId}',
             '{clientApplicationId}',
             'LiveSelfieImage',
             '{artifactId}',
             {revision},
             'sha256:caller-computed-session-challenge',
             'evidence:c1a',
             pg_catalog.transaction_timestamp(),
             'acceptance-policy:c1a',
             1);
        """;

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        return connection;
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

    private static async Task<object?> ScalarAsync(
        NpgsqlConnection connection,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return await command.ExecuteScalarAsync();
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

    private sealed record SeededCapture(
        Guid SessionId,
        Guid ClientApplicationId,
        Guid ArtifactId);
}
