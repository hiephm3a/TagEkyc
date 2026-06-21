using Microsoft.EntityFrameworkCore;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfCaptureArtifactRepository(TagEkycDbContext db) : ICaptureArtifactRepository
{
    public async Task AppendAsync(CaptureArtifact artifact, CancellationToken cancellationToken = default)
    {
        db.CaptureArtifacts.Add(DomainRowMapper.ToRow(artifact));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CaptureArtifact>> ListBySessionAsync(Guid verificationSessionId, CancellationToken cancellationToken = default)
    {
        var rows = await db.CaptureArtifacts.AsNoTracking()
            .Where(candidate => candidate.VerificationSessionId == verificationSessionId)
            .OrderBy(candidate => candidate.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return rows.Select(DomainRowMapper.ToDomain).ToArray();
    }
}
