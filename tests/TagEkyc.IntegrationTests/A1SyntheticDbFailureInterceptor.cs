using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace TagEkyc.IntegrationTests;

// Synthetic integration fixture only. Records failures for test assertions;
// does not alter production exception mapping or any returned HTTP payload.
internal sealed class A1SyntheticDbFailureInterceptor : DbCommandInterceptor
{
    private static readonly ConcurrentQueue<string> Failures = new();
    internal static string Recorded => string.Join(Environment.NewLine, Failures);
    internal static void Clear() => Failures.Clear();

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Record(eventData.Exception);
        return Task.CompletedTask;
    }

    internal static void Record(Exception error)
    {
        Failures.Enqueue(error is PostgresException pg
            ? $"SQLSTATE={pg.SqlState}; Message={pg.MessageText}; Detail={pg.Detail}; Where={pg.Where}; Constraint={pg.ConstraintName}"
            : error.ToString());
    }
}

internal sealed class A1SyntheticTransactionFailureInterceptor : DbTransactionInterceptor
{
    public override Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        A1SyntheticDbFailureInterceptor.Record(eventData.Exception);
        return Task.CompletedTask;
    }
}
