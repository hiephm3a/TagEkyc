namespace TagEkyc.Infrastructure.Persistence.Entities;
public sealed class RawExportDecisionFulfillmentRefRow
{ public Guid ExportDecisionId { get; set; } public string RequirementType { get; set; } = string.Empty; public int Ordinal { get; set; } public Guid FulfillmentEventId { get; set; } public int Revision { get; set; } public string ArtifactRef { get; set; } = string.Empty; public string ArtifactVersion { get; set; } = string.Empty; public DateTimeOffset? ValidUntilUtc { get; set; } }
