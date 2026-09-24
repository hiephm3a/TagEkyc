using Microsoft.EntityFrameworkCore;
using TagEkyc.Application.Ports;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfRawExportJobPackageProjectionReader(TagEkycDbContext db)
    : IRawExportJobPackageProjectionReader
{
    public Task<RawExportJobPackageProjection?> ReadByJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        db.RawExportRecipientPackagePreparations.AsNoTracking()
            .Where(value => value.JobId == jobId)
            .OrderByDescending(value => value.Revision)
            .Select(value => new RawExportJobPackageProjection(
                value.PackageId,
                value.State,
                value.FinalizedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
}
