using Microsoft.EntityFrameworkCore;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfAppendIdempotencyBoundary(TagEkycDbContext db)
    : IAppendIdempotencyRepository, IAppendIdempotencyBoundary
{
    public async Task<AppendIdempotencyRecord?> GetAsync(
        Guid verificationSessionId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var row = await db.AppendIdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate =>
                candidate.VerificationSessionId == verificationSessionId &&
                candidate.IdempotencyKey == idempotencyKey,
                cancellationToken);

        return row is null ? null : DomainRowMapper.ToDomain(row);
    }

    public async Task<AppendIdempotencyApplyResult> TryApplyCaptureArtifactAsync(
        AppendCaptureArtifactWrite write,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (await IsSessionTerminalAsync(write.Session.Id, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new AppendIdempotencyApplyResult(AppendIdempotencyApplyStatus.SessionTerminal, write.Idempotency);
            }

            db.AppendIdempotencyRecords.Add(DomainRowMapper.ToRow(write.Idempotency));
            db.CaptureArtifacts.Add(DomainRowMapper.ToRow(write.Artifact));
            await ApplySessionStateAsync(write.Session.Id, write.FinalState, cancellationToken);
            AddAuditEvents(write.AuditEvents);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new AppendIdempotencyApplyResult(AppendIdempotencyApplyStatus.Applied, write.Idempotency);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            if (await IsSessionTerminalAsync(write.Session.Id, cancellationToken))
            {
                return new AppendIdempotencyApplyResult(AppendIdempotencyApplyStatus.SessionTerminal, write.Idempotency);
            }

            throw;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return await ReplayOrThrowAsync(write.Idempotency, cancellationToken);
        }
    }

    public async Task<AppendIdempotencyApplyResult> TryApplyEvidenceResultAsync(
        AppendEvidenceResultWrite write,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (await IsSessionTerminalAsync(write.Session.Id, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new AppendIdempotencyApplyResult(AppendIdempotencyApplyStatus.SessionTerminal, write.Idempotency);
            }

            db.AppendIdempotencyRecords.Add(DomainRowMapper.ToRow(write.Idempotency));
            db.EvidenceResults.Add(DomainRowMapper.ToRow(write.EvidenceResult));
            await ApplySessionStateAsync(write.Session.Id, write.FinalState, cancellationToken);
            AddAuditEvents(write.AuditEvents);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new AppendIdempotencyApplyResult(AppendIdempotencyApplyStatus.Applied, write.Idempotency);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            if (await IsSessionTerminalAsync(write.Session.Id, cancellationToken))
            {
                return new AppendIdempotencyApplyResult(AppendIdempotencyApplyStatus.SessionTerminal, write.Idempotency);
            }

            throw;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return await ReplayOrThrowAsync(write.Idempotency, cancellationToken);
        }
    }

    private async Task ApplySessionStateAsync(
        Guid verificationSessionId,
        VerificationSessionState finalState,
        CancellationToken cancellationToken)
    {
        var row = await db.Sessions.SingleAsync(candidate => candidate.Id == verificationSessionId, cancellationToken);
        row.State = finalState.ToString();
    }

    private async Task<bool> IsSessionTerminalAsync(Guid verificationSessionId, CancellationToken cancellationToken)
    {
        var row = await db.Sessions
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == verificationSessionId, cancellationToken);
        var state = Enum.Parse<VerificationSessionState>(row.State);
        return state is VerificationSessionState.Completed
            or VerificationSessionState.Cancelled
            or VerificationSessionState.Expired
            or VerificationSessionState.TechnicalTerminal;
    }

    private void AddAuditEvents(IReadOnlyList<AuditEvent> auditEvents)
    {
        foreach (var auditEvent in auditEvents)
        {
            db.AuditEvents.Add(DomainRowMapper.ToRow(auditEvent));
        }
    }

    private async Task<AppendIdempotencyApplyResult> ReplayOrThrowAsync(
        AppendIdempotencyRecord attempted,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var existing = await GetAsync(attempted.VerificationSessionId, attempted.IdempotencyKey, cancellationToken)
            ?? throw new DbUpdateException("Append idempotency write failed before a matching idempotency record could be read.");

        if (!string.Equals(existing.EndpointKind, attempted.EndpointKind, StringComparison.Ordinal) ||
            !string.Equals(existing.SubmissionSlot, attempted.SubmissionSlot, StringComparison.Ordinal))
        {
            return new AppendIdempotencyApplyResult(AppendIdempotencyApplyStatus.SlotMismatch, existing);
        }

        if (!string.Equals(existing.Fingerprint, attempted.Fingerprint, StringComparison.Ordinal))
        {
            return new AppendIdempotencyApplyResult(AppendIdempotencyApplyStatus.PayloadMismatch, existing);
        }

        return new AppendIdempotencyApplyResult(AppendIdempotencyApplyStatus.Deduplicated, existing);
    }
}
