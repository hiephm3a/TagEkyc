using System.Transactions;
using Npgsql;

namespace TagEkyc.Infrastructure.RawExport;

internal enum RetainedNoStartOutcome { TerminatedBeforeStart, ExistingMatch, LeaseLive, ProviderEvidencePresent, NotFound, StateConflict }
internal sealed record RetainedNoStartResult(RetainedNoStartOutcome Outcome, DateTimeOffset? TerminatedAtUtc);
internal enum RetainedTerminalFinalizeOutcome { Finalized, ExistingMatch, CleanupPending, NotFound, StateConflict }
internal sealed record RetainedTerminalFinalizeResult(RetainedTerminalFinalizeOutcome Outcome, string? TerminalOutcomeCode);

// The role-specific composition owns this data source and the frozen custody
// actor. It must not supply the provider's active connection or a Client actor.
internal sealed class RawExportR2TerminalIntentRecorder : IRawExportR2TerminalIntentRecorder
{
    private readonly NpgsqlDataSource source;
    private readonly Guid actorPrincipalId;
    private readonly int requestTimeoutMilliseconds;
    private readonly CancellationToken hostStopping;

    internal RawExportR2TerminalIntentRecorder(NpgsqlDataSource source, Guid actorPrincipalId,
        int requestTimeoutMilliseconds, CancellationToken hostStopping)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (actorPrincipalId == Guid.Empty)
            throw new ArgumentException("A3_R2_TERMINAL_ACTOR_INVALID", nameof(actorPrincipalId));
        if (requestTimeoutMilliseconds is < 1 or > 30000)
            throw new ArgumentOutOfRangeException(nameof(requestTimeoutMilliseconds));
        this.source = source;
        this.actorPrincipalId = actorPrincipalId;
        this.requestTimeoutMilliseconds = requestTimeoutMilliseconds;
        this.hostStopping = hostStopping;
    }

    public async Task<RawExportR2TerminalIntentOutcome> RecordAsync(
        Guid sourceArtifactId, Guid attemptId, long expectedRevision, long expectedFence,
        string operationalDisposition, string terminalOutcomeCode, CancellationToken cancellationToken)
    {
        // Input failure can be observed after the HTTP token has died. Only
        // host shutdown and the configured independent recording deadline own
        // this small transaction; the request token deliberately does not.
        _ = cancellationToken;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(hostStopping);
        deadline.CancelAfter(requestTimeoutMilliseconds);
        var token = deadline.Token;
        token.ThrowIfCancellationRequested();
        // A fresh connection must not auto-enlist in a caller/provider ambient
        // transaction. Suppression is async-flow local, not a global setting.
        using var independent = new TransactionScope(TransactionScopeOption.Suppress,
            TransactionScopeAsyncFlowOption.Enabled);
        await using var connection = await source.OpenConnectionAsync(token).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(token).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = checked((requestTimeoutMilliseconds + 999) / 1000);
        command.CommandText = "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true)";
        command.Parameters.AddWithValue("actor", actorPrincipalId.ToString("D"));
        await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);

        command.Parameters.Clear();
        command.CommandText = """
            SELECT * FROM tagekyc.raw_export_record_retained_r2_terminal_intent(
              @source,@attempt,@revision,@fence,@disposition,@code)
            """;
        command.Parameters.AddWithValue("source", sourceArtifactId);
        command.Parameters.AddWithValue("attempt", attemptId);
        command.Parameters.AddWithValue("revision", expectedRevision);
        command.Parameters.AddWithValue("fence", expectedFence);
        command.Parameters.AddWithValue("disposition", operationalDisposition);
        command.Parameters.AddWithValue("code", terminalOutcomeCode);
        RawExportR2TerminalIntentOutcome outcome;
        await using (var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false))
        {
            if (reader.FieldCount != 4 || !await reader.ReadAsync(token).ConfigureAwait(false))
                throw InvalidResult();
            outcome = reader.GetString(reader.GetOrdinal("Outcome")) switch
            {
                "Recorded" => RawExportR2TerminalIntentOutcome.Recorded,
                "ExistingMatch" => RawExportR2TerminalIntentOutcome.ExistingMatch,
                "StateConflict" => RawExportR2TerminalIntentOutcome.StateConflict,
                "NotFound" => RawExportR2TerminalIntentOutcome.NotFound,
                _ => throw InvalidResult(),
            };
            var code = reader.GetOrdinal("TerminalIntentCode");
            var disposition = reader.GetOrdinal("TerminalIntentDisposition");
            var at = reader.GetOrdinal("TerminalIntentAtUtc");
            if (outcome is RawExportR2TerminalIntentOutcome.Recorded or RawExportR2TerminalIntentOutcome.ExistingMatch)
            {
                if (reader.IsDBNull(code) || reader.IsDBNull(disposition) || reader.IsDBNull(at)
                    || reader.GetString(code) != terminalOutcomeCode
                    || reader.GetString(disposition) != operationalDisposition)
                    throw InvalidResult();
                _ = reader.GetFieldValue<DateTimeOffset>(at);
            }
            else if (!reader.IsDBNull(code) || !reader.IsDBNull(disposition) || !reader.IsDBNull(at))
                throw InvalidResult();
            if (await reader.ReadAsync(token).ConfigureAwait(false)
                || await reader.NextResultAsync(token).ConfigureAwait(false))
                throw InvalidResult();
        }
        // An uncertain/failed commit throws; it must never produce a persisted
        // observation merely because the SQL reader returned Recorded.
        await transaction.CommitAsync(token).ConfigureAwait(false);
        independent.Complete();
        return outcome;
    }

    internal Task<RetainedNoStartResult> TerminateBeforeProviderStartAsync(
        Guid sourceArtifactId, Guid attemptId, long expectedRevision, long expectedFence, CancellationToken stoppingToken) =>
        ExecuteRecoveryAsync(sourceArtifactId, attemptId, expectedRevision, expectedFence,
            "SELECT * FROM tagekyc.raw_export_terminate_retained_r2_before_provider_start(@source,@attempt,@revision,@fence)",
            ["Outcome", "R2TerminationDisposition", "R2TerminatedAtUtc"], reader =>
            {
                var outcome = reader.GetString(0) switch
                {
                    "TerminatedBeforeStart" => RetainedNoStartOutcome.TerminatedBeforeStart,
                    "ExistingMatch" => RetainedNoStartOutcome.ExistingMatch,
                    "LeaseLive" => RetainedNoStartOutcome.LeaseLive,
                    "ProviderEvidencePresent" => RetainedNoStartOutcome.ProviderEvidencePresent,
                    "NotFound" => RetainedNoStartOutcome.NotFound,
                    "StateConflict" => RetainedNoStartOutcome.StateConflict,
                    _ => throw InvalidResult(),
                };
                DateTimeOffset? at = null;
                if (outcome is RetainedNoStartOutcome.TerminatedBeforeStart or RetainedNoStartOutcome.ExistingMatch)
                {
                    if (reader.IsDBNull(1) || reader.GetString(1) != "TerminatedBeforeStart" || reader.IsDBNull(2))
                        throw InvalidResult();
                    at = reader.GetFieldValue<DateTimeOffset>(2);
                }
                else if (!reader.IsDBNull(1) || !reader.IsDBNull(2)) throw InvalidResult();
                return new RetainedNoStartResult(outcome, at);
            }, stoppingToken);

    internal Task<RetainedTerminalFinalizeResult> FinalizeAsync(Guid sourceArtifactId, Guid attemptId,
        long expectedRevision, long expectedFence, string expectedIntentCode, CancellationToken stoppingToken)
    {
        if (expectedIntentCode is not ("RECAPTURE_REQUIRED" or "CONTENT_COMMITMENT_MISMATCH"
            or "RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED")) throw InvalidResult();
        return ExecuteRecoveryAsync(sourceArtifactId, attemptId, expectedRevision, expectedFence,
            "SELECT * FROM tagekyc.raw_export_finalize_retained_r2_terminal(@source,@attempt,@revision,@fence)",
            ["Outcome", "TerminalOutcomeCode"], reader =>
            {
                var outcome = reader.GetString(0) switch
                {
                    "Finalized" => RetainedTerminalFinalizeOutcome.Finalized,
                    "ExistingMatch" => RetainedTerminalFinalizeOutcome.ExistingMatch,
                    "CleanupPending" => RetainedTerminalFinalizeOutcome.CleanupPending,
                    "NotFound" => RetainedTerminalFinalizeOutcome.NotFound,
                    "StateConflict" => RetainedTerminalFinalizeOutcome.StateConflict,
                    _ => throw InvalidResult(),
                };
                var code = reader.IsDBNull(1) ? null : reader.GetString(1);
                if (outcome is RetainedTerminalFinalizeOutcome.Finalized or RetainedTerminalFinalizeOutcome.ExistingMatch)
                {
                    if (code != expectedIntentCode) throw InvalidResult();
                }
                else if (code is not null) throw InvalidResult();
                return new RetainedTerminalFinalizeResult(outcome, code);
            }, stoppingToken);
    }

    // Only recovery's host-stop token is supplied here, never an HTTP token.
    // SQL owns absence/settlement, locked time and all durable state changes.
    private async Task<T> ExecuteRecoveryAsync<T>(Guid sourceArtifactId, Guid attemptId, long revision, long fence,
        string sql, string[] columns, Func<NpgsqlDataReader, T> project, CancellationToken stoppingToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(hostStopping, stoppingToken);
        deadline.CancelAfter(requestTimeoutMilliseconds);
        var token = deadline.Token;
        using var independent = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
        await using var connection = await source.OpenConnectionAsync(token).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(token).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = checked((requestTimeoutMilliseconds + 999) / 1000);
        command.CommandText = "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true)";
        command.Parameters.AddWithValue("actor", actorPrincipalId.ToString("D"));
        await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        command.Parameters.Clear();
        command.CommandText = sql;
        command.Parameters.AddWithValue("source", sourceArtifactId);
        command.Parameters.AddWithValue("attempt", attemptId);
        command.Parameters.AddWithValue("revision", revision);
        command.Parameters.AddWithValue("fence", fence);
        T result;
        await using (var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false))
        {
            if (reader.FieldCount != columns.Length
                || !columns.SequenceEqual(Enumerable.Range(0, reader.FieldCount).Select(reader.GetName), StringComparer.Ordinal)
                || !await reader.ReadAsync(token).ConfigureAwait(false)) throw InvalidResult();
            result = project(reader);
            if (await reader.ReadAsync(token).ConfigureAwait(false) || await reader.NextResultAsync(token).ConfigureAwait(false))
                throw InvalidResult();
        }
        await transaction.CommitAsync(token).ConfigureAwait(false);
        independent.Complete();
        return result;
    }

    private static InvalidOperationException InvalidResult() => new("A3_R2_TERMINAL_INTENT_RESULT_INVALID");
}
