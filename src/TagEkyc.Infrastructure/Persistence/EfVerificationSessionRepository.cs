using Microsoft.EntityFrameworkCore;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfVerificationSessionRepository(TagEkycDbContext db) : IVerificationSessionRepository
{
    public async Task AddAsync(VerificationSession session, CancellationToken cancellationToken = default)
    {
        db.Sessions.Add(DomainRowMapper.ToRow(session));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<VerificationSession?> GetAsync(Guid verificationSessionId, CancellationToken cancellationToken = default)
    {
        var row = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == verificationSessionId, cancellationToken);
        return row is null ? null : DomainRowMapper.ToDomain(row);
    }

    public async Task<VerificationSession?> GetByExternalSessionIdAsync(
        Guid clientApplicationId,
        string externalSessionId,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.ClientApplicationId == clientApplicationId &&
                candidate.ExternalSessionId == externalSessionId,
            cancellationToken);
        return row is null ? null : DomainRowMapper.ToDomain(row);
    }

    public async Task SetStateAsync(
        Guid verificationSessionId,
        VerificationSessionState state,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Sessions.SingleAsync(candidate => candidate.Id == verificationSessionId, cancellationToken);
        row.State = state.ToString();
        await db.SaveChangesAsync(cancellationToken);
    }
}
