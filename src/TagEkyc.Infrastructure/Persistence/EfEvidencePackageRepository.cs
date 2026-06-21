using Microsoft.EntityFrameworkCore;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfEvidencePackageRepository(TagEkycDbContext db) : IEvidencePackageRepository
{
    public async Task AppendAsync(EvidencePackage evidencePackage, CancellationToken cancellationToken = default)
    {
        db.EvidencePackages.Add(DomainRowMapper.ToRow(evidencePackage));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<EvidencePackage?> GetAsync(Guid evidencePackageId, CancellationToken cancellationToken = default)
    {
        var row = await db.EvidencePackages.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == evidencePackageId, cancellationToken);
        return row is null ? null : DomainRowMapper.ToDomain(row);
    }

    public async Task<EvidencePackage?> GetBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default)
    {
        var row = await db.EvidencePackages.AsNoTracking()
            .Where(candidate => candidate.VerificationSessionId == verificationSessionId)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : DomainRowMapper.ToDomain(row);
    }
}
