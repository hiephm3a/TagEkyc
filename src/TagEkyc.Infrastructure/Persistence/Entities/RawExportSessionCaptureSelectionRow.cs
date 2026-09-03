namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportSessionCaptureSelectionRow
{
    public Guid SessionCaptureSelectionId { get; set; }
    public Guid VerificationSessionId { get; set; }
    public string RawClass { get; set; } = string.Empty;
    public Guid CaptureAcceptanceId { get; set; }
}
