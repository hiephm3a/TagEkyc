using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfVerificationFinalizationBoundary(
    TagEkycDbContext db,
    EfPersistenceFaultInjector? faultInjector = null)
    : IVerificationFinalizationBoundary
{
    public async Task<VerificationFinalizationWriteResult> TryFinalizeAsync(
        VerificationFinalizationWrite write,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var currentRow = await db.Sessions.SingleOrDefaultAsync(candidate => candidate.Id == write.ExpectedSession.Id, cancellationToken);
        if (currentRow is null)
        {
            return new VerificationFinalizationWriteResult(VerificationFinalizationWriteStatus.NotFound, Session: null);
        }

        var current = DomainRowMapper.ToDomain(currentRow);
        if (current.State == VerificationSessionState.Completed)
        {
            return new VerificationFinalizationWriteResult(VerificationFinalizationWriteStatus.AlreadyCompleted, current);
        }

        if (!MatchesExpectedSession(current, write.ExpectedSession) ||
            current.State is VerificationSessionState.Expired
                or VerificationSessionState.Cancelled
                or VerificationSessionState.TechnicalTerminal ||
            !HasCompleteSnapshot(write.CompletedSession))
        {
            return new VerificationFinalizationWriteResult(VerificationFinalizationWriteStatus.StateMismatch, current);
        }

        ApplySession(currentRow, write.CompletedSession);
        faultInjector?.ThrowIfFinalizationSessionUpdated();

        db.VerificationDecisions.Add(DomainRowMapper.ToRow(write.Decision));
        db.EvidencePackages.Add(DomainRowMapper.ToRow(write.EvidencePackage));
        db.EvidenceManifests.Add(DomainRowMapper.ToRow(write.Manifest));
        db.AuditEvents.Add(DomainRowMapper.ToRow(write.CompletionAuditEvent));

        try
        {
            if (faultInjector is not null)
            {
                await faultInjector.WaitBeforeFinalizationSaveAsync(cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new VerificationFinalizationWriteResult(
                VerificationFinalizationWriteStatus.StateMismatch,
                Session: null);
        }

        return new VerificationFinalizationWriteResult(
            VerificationFinalizationWriteStatus.Applied,
            write.CompletedSession);
    }

    public async Task<VerificationFinalizationWriteResult> TryCancelAsync(
        VerificationCancellationWrite write,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await CancelInTransactionAsync(write, cancellationToken);
        }
        catch (Exception exception) when (exception is NpgsqlException or DbUpdateException)
        {
            // Includes failure to open the connection/begin B; no exception details
            // become public cancellation responses.
            return new(VerificationFinalizationWriteStatus.NotReady, null);
        }
    }

    private async Task<VerificationFinalizationWriteResult> CancelInTransactionAsync(
        VerificationCancellationWrite write, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var audit = write.CancellationAuditEvent;
            var result = await db.Database.SqlQueryRaw<CancelSqlResult>("""
                SELECT result_code AS "ResultCode" FROM tagekyc.capture_runtime_cancel_session_with_capability(
                    @client,@session,@reason,@request,@correlation,@now,@prefix,@audit)
                """, new NpgsqlParameter("client", audit.ClientApplicationId),
                new NpgsqlParameter("session", write.ExpectedSession.Id),
                new NpgsqlParameter("reason", audit.EventPayloadRef ?? string.Empty),
                new NpgsqlParameter("request", write.CancelledSession.RequestId),
                new NpgsqlParameter("correlation", write.CancelledSession.CorrelationId),
                new NpgsqlParameter("now", audit.OccurredAt),
                new NpgsqlParameter("prefix", audit.ActorId ?? string.Empty),
                new NpgsqlParameter("audit", audit.Id)).SingleAsync(cancellationToken);
            // SQL owns session/capability/audit mutation. Read fresh database state: a
            // concurrent loser must return the winner's metadata, never its candidate.
            var row = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(
                candidate => candidate.Id == write.ExpectedSession.Id &&
                    candidate.ClientApplicationId == audit.ClientApplicationId, cancellationToken);
            var current = row is null ? null : DomainRowMapper.ToDomain(row);
            var status = result.ResultCode switch
            {
                "APPLIED" or "AVAILABLE" when current?.State == VerificationSessionState.Cancelled
                    => VerificationFinalizationWriteStatus.Applied,
                "RESOURCE_NOT_AVAILABLE" => VerificationFinalizationWriteStatus.NotFound,
                "INVALID_INPUT" => VerificationFinalizationWriteStatus.InvalidRequest,
                "DENIED" => VerificationFinalizationWriteStatus.AccessDenied,
                "CONFLICT" => VerificationFinalizationWriteStatus.StateMismatch,
                _ => VerificationFinalizationWriteStatus.NotReady
            };
            if (status == VerificationFinalizationWriteStatus.NotReady)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(status, null);
            }
            // In particular, preserve authorized expiry-first work even when the
            // requested cancellation receives a typed terminal denial.
            await transaction.CommitAsync(cancellationToken);
            return new(status, status == VerificationFinalizationWriteStatus.AccessDenied ? null : current);
        }
        catch (Exception exception) when (exception is NpgsqlException or DbUpdateException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return new(VerificationFinalizationWriteStatus.NotReady, null);
        }
    }

    private sealed class CancelSqlResult
    {
        public string ResultCode { get; set; } = string.Empty;
    }

    private static void ApplySession(Entities.VerificationSessionRow row, VerificationSession session)
    {
        var replacement = DomainRowMapper.ToRow(session);
        row.State = replacement.State;
        row.Result = replacement.Result;
        row.AssuranceLevel = replacement.AssuranceLevel;
        row.FinalDecisionId = replacement.FinalDecisionId;
        row.EvidencePackageId = replacement.EvidencePackageId;
        row.EvidencePackageHash = replacement.EvidencePackageHash;
        row.ManifestHash = replacement.ManifestHash;
        row.RequestId = replacement.RequestId;
        row.CorrelationId = replacement.CorrelationId;
        row.CompletedAt = replacement.CompletedAt;
    }

    private static bool HasCompleteSnapshot(VerificationSession session) =>
        session.State == VerificationSessionState.Completed &&
        session.FinalDecisionId is not null &&
        session.EvidencePackageId is not null &&
        session.EvidencePackageHash is not null &&
        session.ManifestHash is not null &&
        session.CompletedAt is not null &&
        !string.IsNullOrWhiteSpace(session.RequestId) &&
        !string.IsNullOrWhiteSpace(session.CorrelationId);

    private static bool MatchesExpectedSession(VerificationSession current, VerificationSession expected) =>
        current.Id == expected.Id &&
        current.ClientApplicationId == expected.ClientApplicationId &&
        current.SubjectRef == expected.SubjectRef &&
        current.Profile == expected.Profile &&
        current.Purpose == expected.Purpose &&
        current.RequiredChecks.SetEquals(expected.RequiredChecks) &&
        current.ExternalSessionId == expected.ExternalSessionId &&
        current.ClientReference == expected.ClientReference &&
        current.Challenge == expected.Challenge &&
        current.RequestId == expected.RequestId &&
        current.CorrelationId == expected.CorrelationId &&
        current.State == expected.State &&
        current.Result == expected.Result &&
        current.AssuranceLevel == expected.AssuranceLevel &&
        current.FinalDecisionId == expected.FinalDecisionId &&
        current.EvidencePackageId == expected.EvidencePackageId &&
        current.EvidencePackageHash == expected.EvidencePackageHash &&
        current.ManifestHash == expected.ManifestHash &&
        MatchesMetadata(current.Metadata, expected.Metadata) &&
        current.ExpiresAt == expected.ExpiresAt &&
        current.CreatedAt == expected.CreatedAt &&
        current.CompletedAt == expected.CompletedAt;

    private static bool MatchesMetadata(DataBoundaryMetadata current, DataBoundaryMetadata expected) =>
        current.PolicySnapshotId == expected.PolicySnapshotId &&
        current.RetentionClass == expected.RetentionClass &&
        current.DeletionEligibility == expected.DeletionEligibility &&
        current.LegalHoldStatus == expected.LegalHoldStatus &&
        current.PurgeBlockReason == expected.PurgeBlockReason &&
        current.AccessAuditRequired == expected.AccessAuditRequired &&
        current.Actor == expected.Actor &&
        current.DecisionBoundary.PolicySnapshotId == expected.DecisionBoundary.PolicySnapshotId &&
        current.DecisionBoundary.RequiredCheckSetPolicy.PolicySnapshotId == expected.DecisionBoundary.RequiredCheckSetPolicy.PolicySnapshotId &&
        current.DecisionBoundary.RequiredCheckSetPolicy.RequiredChecks.SetEquals(
            expected.DecisionBoundary.RequiredCheckSetPolicy.RequiredChecks);
}
