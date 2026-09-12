using Microsoft.EntityFrameworkCore;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class RawExportCaptureAcceptanceResolver(TagEkycDbContext db)
    : IRawExportCaptureAcceptanceResolver
{
    public async Task<RawExportResolvedCaptureAcceptance?> ResolveExactAsync(
        AuthenticatedClientContext caller,
        RawExportSourceIngressMetadata metadata,
        CancellationToken cancellationToken)
    {
        var matches = await (from row in db.RawExportCaptureAcceptanceEvents.AsNoTracking()
            join artifact in db.CaptureArtifacts.AsNoTracking() on row.CaptureArtifactId equals artifact.Id
            where row.ClientApplicationId == caller.ClientApplicationId
                && row.VerificationSessionId == metadata.VerificationSessionId
                && row.CaptureArtifactId == metadata.CaptureArtifactId
                && row.CaptureRevision == metadata.CaptureRevision
                && row.RawClass == metadata.RawClass
                && artifact.VerificationSessionId == metadata.VerificationSessionId
                && artifact.CaptureAgentId != null
                && artifact.DeviceId != null
                && caller.AllowedCaptureAgentIds != null
                && caller.AllowedCaptureAgentIds.Contains(artifact.CaptureAgentId)
            select new RawExportResolvedCaptureAcceptance(
                row.CaptureAcceptanceId,
                row.SessionChallengeHash,
                artifact.CaptureAgentId,
                artifact.DeviceId))
            .Take(2)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return matches.Count == 1 ? matches[0] : null;
    }
}
