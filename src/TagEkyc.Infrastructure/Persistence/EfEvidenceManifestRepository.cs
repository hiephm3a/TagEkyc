using Microsoft.EntityFrameworkCore;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.InternalAudit.Manifest;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfEvidenceManifestRepository(TagEkycDbContext db) : IInternalEvidenceManifestRepository
{
    public async Task AppendAsync(EvidenceManifestDto manifest, CancellationToken cancellationToken = default)
    {
        db.EvidenceManifests.Add(DomainRowMapper.ToRow(manifest));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<EvidenceManifestDto?> GetByPackageAsync(Guid evidencePackageId, CancellationToken cancellationToken = default)
    {
        var packageId = evidencePackageId.ToString("N");
        var row = await db.EvidenceManifests.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.EvidencePackageId == packageId, cancellationToken);
        return row is null ? null : DomainRowMapper.ToDomain(row);
    }
}
