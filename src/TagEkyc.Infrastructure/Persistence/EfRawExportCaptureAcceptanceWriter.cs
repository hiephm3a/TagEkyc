using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.RawExport;
using TagEkyc.Contracts.TrustedAdapter;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class EfRawExportCaptureAcceptanceWriter(TagEkycDbContext db)
    : IRawExportCaptureAcceptanceWriter
{
    public async Task<RawCaptureAcceptanceDto?> BindAcceptedEvidenceAsync(
        Guid bindingId,
        Guid verificationSessionId,
        Guid evidenceResultId,
        RawExportCaptureAcceptancePolicy? configuredPolicy,
        CancellationToken cancellationToken)
    {
        if (bindingId == Guid.Empty || verificationSessionId == Guid.Empty || evidenceResultId == Guid.Empty)
            throw new ArgumentException("Raw capture acceptance identity is invalid.");
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Raw capture acceptance requires its owning append transaction.");

        var policyId = new NpgsqlParameter("policy_id", NpgsqlDbType.Text)
            { Value = configuredPolicy?.AcceptancePolicyId ?? (object)DBNull.Value };
        var policyVersion = new NpgsqlParameter("policy_version", NpgsqlDbType.Integer)
            { Value = configuredPolicy?.AcceptancePolicyVersion ?? (object)DBNull.Value };
        try
        {
            var rows = await db.Database.SqlQueryRaw<AcceptanceSqlResult>("""
                SELECT "CaptureArtifactId", "CaptureAcceptanceId", "CaptureRevision", "RawClass"
                FROM tagekyc.raw_export_accept_runtime_evidence(
                    @binding_id,@session_id,@evidence_id,@policy_id,@policy_version)
                """,
                new NpgsqlParameter("binding_id", bindingId),
                new NpgsqlParameter("session_id", verificationSessionId),
                new NpgsqlParameter("evidence_id", evidenceResultId),
                policyId, policyVersion)
                .Take(2)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (rows.Count > 1)
                throw new RawExportCaptureAcceptanceConflictException();
            return rows.Count == 0 ? null : new(rows[0].CaptureArtifactId,
                rows[0].CaptureAcceptanceId, rows[0].CaptureRevision, rows[0].RawClass);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.RaiseException
            && exception.MessageText is "RAW_EXPORT_CAPTURE_ACCEPTANCE_EVIDENCE_INVALID"
                or "RAW_EXPORT_CAPTURE_ACCEPTANCE_EVIDENCE_INELIGIBLE"
                or "RAW_EXPORT_CAPTURE_ACCEPTANCE_CONFLICT")
        {
            throw new RawExportCaptureAcceptanceConflictException();
        }
    }

    private sealed class AcceptanceSqlResult
    {
        public Guid CaptureArtifactId { get; set; }
        public Guid CaptureAcceptanceId { get; set; }
        public int CaptureRevision { get; set; }
        public string RawClass { get; set; } = string.Empty;
    }
}
