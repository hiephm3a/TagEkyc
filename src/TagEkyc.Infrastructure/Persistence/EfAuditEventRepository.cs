using Microsoft.EntityFrameworkCore;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfAuditEventRepository(TagEkycDbContext db) : IAuditEventRepository
{
    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        db.AuditEvents.Add(DomainRowMapper.ToRow(auditEvent));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default)
    {
        var rows = await db.AuditEvents.AsNoTracking()
            .Where(candidate => candidate.VerificationSessionId == verificationSessionId)
            .OrderBy(candidate => candidate.OccurredAt)
            .ToArrayAsync(cancellationToken);

        return rows.Select(DomainRowMapper.ToDomain).ToArray();
    }
}
