using Microsoft.EntityFrameworkCore;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfVerificationDecisionRepository(TagEkycDbContext db) : IVerificationDecisionRepository
{
    public async Task AppendAsync(VerificationDecision decision, CancellationToken cancellationToken = default)
    {
        db.VerificationDecisions.Add(DomainRowMapper.ToRow(decision));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VerificationDecision>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default)
    {
        var rows = await db.VerificationDecisions.AsNoTracking()
            .Where(candidate => candidate.VerificationSessionId == verificationSessionId)
            .OrderBy(candidate => candidate.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return rows.Select(DomainRowMapper.ToDomain).ToArray();
    }

    public async Task<VerificationDecision?> GetAsync(Guid verificationDecisionId, CancellationToken cancellationToken = default)
    {
        var row = await db.VerificationDecisions.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == verificationDecisionId, cancellationToken);
        return row is null ? null : DomainRowMapper.ToDomain(row);
    }
}
