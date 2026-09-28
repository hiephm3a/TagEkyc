using Npgsql;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class PostgresSiteRawIngressQualificationBrokerObserver(NpgsqlDataSource source)
    : ISiteRawIngressQualificationBrokerObserver
{
    public async Task HoldBeforeCommitAsync(Guid qualificationRunId,
        CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var mark = new NpgsqlCommand(
            "SELECT tagekyc.site_qualification_broker_mark_held($1)",
            connection);
        Add(mark, qualificationRunId);
        var value = await mark.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (value is not Guid runId || runId == Guid.Empty) return;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var released = new NpgsqlCommand(
                "SELECT tagekyc.site_qualification_broker_is_released($1)",
                connection);
            Add(released, runId);
            if (await released.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is true)
                return;
            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RecordCommittedAsync(Guid qualificationRunId,
        CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(
            "SELECT tagekyc.site_qualification_broker_mark_committed($1)",
            connection);
        Add(command, qualificationRunId);
        if (await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not true)
            return;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var acknowledged = new NpgsqlCommand(
                "SELECT tagekyc.site_qualification_broker_is_commit_acknowledged($1)",
                connection);
            Add(acknowledged, qualificationRunId);
            if (await acknowledged.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is true)
                return;
            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken).ConfigureAwait(false);
        }
    }

    private static void Add(NpgsqlCommand command, params object[] values)
    {
        foreach (var value in values) command.Parameters.Add(new NpgsqlParameter { Value = value });
    }
}
