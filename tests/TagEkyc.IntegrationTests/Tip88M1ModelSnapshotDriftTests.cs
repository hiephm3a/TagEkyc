using Npgsql;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88M1ModelSnapshotDriftTests(PostgresPersistenceFixture postgres)
{
    [Fact]
    public async Task Landed_withdrawn_event_shape_rejects_whitespace_decision_ref_and_accepts_null()
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();

        await using (var rejectedTransaction = await connection.BeginTransactionAsync())
        {
            await ExecuteAsync(connection, rejectedTransaction, "ALTER TABLE tagekyc.raw_export_subject_consent_events DISABLE TRIGGER tr_raw_export_subject_consent_events_insert_guard;");
            var exception = await Assert.ThrowsAsync<PostgresException>(() =>
                InsertWithdrawnAsync(connection, rejectedTransaction, Guid.NewGuid(), "   "));
            Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
            Assert.Equal("CK_raw_export_subject_consent_events_EventShape", exception.ConstraintName);
            await rejectedTransaction.RollbackAsync();
        }

        await using (var acceptedTransaction = await connection.BeginTransactionAsync())
        {
            await ExecuteAsync(connection, acceptedTransaction, "ALTER TABLE tagekyc.raw_export_subject_consent_events DISABLE TRIGGER tr_raw_export_subject_consent_events_insert_guard;");
            var recordId = Guid.NewGuid();
            await InsertWithdrawnAsync(connection, acceptedTransaction, recordId, null);

            await using var command = new NpgsqlCommand("SELECT \"DecisionRef\" FROM tagekyc.raw_export_subject_consent_events WHERE \"SubjectConsentRecordId\" = @recordId;", connection, acceptedTransaction);
            command.Parameters.AddWithValue("recordId", recordId);
            Assert.Equal(DBNull.Value, await command.ExecuteScalarAsync());
            await acceptedTransaction.RollbackAsync();
        }
    }

    private static async Task InsertWithdrawnAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid recordId,
        string? decisionRef)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO tagekyc.raw_export_subject_consent_events
                ("SubjectConsentRecordId", "ConsentScopeHash", "VerificationSessionId", "SubjectRef",
                 "PolicyId", "PolicyVersion", "PurposeCode", "RecipientClientApplicationId",
                 "Revision", "EventType", "TargetRevision", "DecisionRef",
                 "WithdrawnByPrincipalId", "RecordedAtUtc")
            VALUES
                (@recordId, decode(repeat('00', 32), 'hex'), '88b2f100-0000-4000-8000-000000000001', 'm1-drift-proof',
                 '88b2f100-0000-4000-8000-000000000002', 1, 'SubjectRawBiometricExport', '88b2f100-0000-4000-8000-000000000003',
                 2, 'Withdrawn', 1, @decisionRef,
                 '88b2f100-0000-4000-8000-000000000004', transaction_timestamp());
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("recordId", recordId);
        command.Parameters.AddWithValue("decisionRef", (object?)decisionRef ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync();
    }
}
