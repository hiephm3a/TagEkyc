using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Application.VerificationSessions;
namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfAppendBusinessTransaction(TagEkycDbContext db) : IAppendBusinessTransaction
{
    public async Task<SessionOperationResult<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<SessionOperationResult<T>>> operation, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("The append business transaction has one owner.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            if (result.IsSuccess)
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                db.ChangeTracker.Clear();
            }
            return result;
        }
        catch (Exception exception) when (exception is NpgsqlException or DbUpdateException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            return SessionOperationResult<T>.Failure("NOT_READY", "Capture Runtime is not ready.", 503);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
    }
}
