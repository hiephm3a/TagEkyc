namespace TagEkyc.Infrastructure.Persistence.Entities;
public sealed class RawExportDecisionEligibilityCauseRow
{ public Guid ExportDecisionId { get; set; } public int Ordinal { get; set; } public string Cause { get; set; } = string.Empty; }
