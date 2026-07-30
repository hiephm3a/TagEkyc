using System.Text;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1AAcceptanceSurfaceArchitectureTests
{
    internal static readonly string[] IntendedIdentifiers =
    [
        "raw_export_capture_acceptance_events",
        "raw_export_session_capture_selections",
        "pk_raw_export_capture_acceptance_events",
        "uq_raw_export_capture_acceptance_revision",
        "uq_raw_export_capture_acceptance_artifact",
        "pk_raw_export_session_capture_selections",
        "uq_raw_export_session_capture_selection_class",
        "fk_raw_export_capture_acceptance_session",
        "fk_raw_export_capture_acceptance_artifact",
        "fk_raw_export_session_selection_acceptance",
        "IX_raw_export_capture_acceptance_events_CaptureArtifactId",
        "IX_raw_export_session_capture_selections_CaptureAcceptanceId",
        "tr_raw_export_capture_acceptance_events_insert_guard",
        "tr_raw_export_session_capture_selections_insert_guard",
        "tr_raw_export_capture_acceptance_events_append_only",
        "tr_raw_export_session_capture_selections_append_only",
        "enforce_raw_export_capture_acceptance_insert",
        "raw_export_append_capture_acceptance",
        "raw_export_select_session_capture_acceptance",
        "CaptureAcceptanceId",
        "VerificationSessionId",
        "ClientApplicationId",
        "RawClass",
        "CaptureArtifactId",
        "CaptureRevision",
        "SessionChallengeHash",
        "AcceptedEvidenceRef",
        "AcceptedAtUtc",
        "AcceptancePolicyId",
        "AcceptancePolicyVersion",
        "SessionCaptureSelectionId",
    ];

    [Fact]
    public void C1A_all_intended_identifiers_are_at_most_63_utf8_bytes()
    {
        var overlong = IntendedIdentifiers
            .Select(identifier => new
            {
                Identifier = identifier,
                Bytes = Encoding.UTF8.GetByteCount(identifier),
            })
            .Where(candidate => candidate.Bytes > 63)
            .ToArray();

        Assert.Empty(overlong);
    }
}
