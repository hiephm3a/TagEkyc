using Microsoft.EntityFrameworkCore;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfEvidenceResultRepository(TagEkycDbContext db) : IEvidenceResultRepository
{
    public async Task AppendAsync(EvidenceResult evidenceResult, CancellationToken cancellationToken = default)
    {
        db.EvidenceResults.Add(DomainRowMapper.ToRow(evidenceResult));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EvidenceResult>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default)
    {
        var rows = await db.EvidenceResults.AsNoTracking()
            .Where(candidate => candidate.VerificationSessionId == verificationSessionId)
            .OrderBy(candidate => candidate.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return rows.Select(DomainRowMapper.ToDomain).ToArray();
    }
}
