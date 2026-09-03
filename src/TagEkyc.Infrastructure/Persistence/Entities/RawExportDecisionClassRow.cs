namespace TagEkyc.Infrastructure.Persistence.Entities;
public sealed class RawExportDecisionClassRow
{ public Guid ExportDecisionId { get; set; } public string ClassKind { get; set; } = string.Empty; public string RawClass { get; set; } = string.Empty; public int Ordinal { get; set; } }
