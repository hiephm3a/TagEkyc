using Microsoft.EntityFrameworkCore;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RawExportR3StagingService(TagEkycDbContext db)
{
    internal async Task<RawExportR3StageResult> StageAsync(
        RawExportR3StageCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var repository = new RawExportR3Repository(db);
        await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await repository.SetActorLocalAsync(command.ActorPrincipalId, cancellationToken).ConfigureAwait(false);
                var result = await repository.StageAsync(command, cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }

    private static void Validate(RawExportR3StageCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.ActorPrincipalId == Guid.Empty
            || command.AttemptId == Guid.Empty
            || command.ObjectCustodyId == Guid.Empty
            || command.ExpectedReservationRevision < 1
            || command.ExpectedEncryptionAttemptRevision < 1
            || command.ExpectedFence < 1
            || command.ExpectedObjectStateRevision < 1)
            throw new ArgumentException("RAW_EXPORT_R3_STAGE_ARGUMENT_INVALID", nameof(command));
    }
}
