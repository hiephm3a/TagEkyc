using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

// BP19 component only: freeze the exact live disposition surface specified by
// the broker-pipeline companion. This is not a producer/mapper proof; the
// unknown numeric result and invalid-shape paths remain separately blocked.
public sealed class Tip88C1C6BA3R2R6StageCatalogueTests
{
    [Fact]
    public void R2R6StageDispositionCatalogueMatchesCompanionContract()
    {
        Exact<RawExportR2WriterDisposition>(
            "PreCustodyRejected", "PreCustodyRetryable", "PreCustodyTerminalKey",
            "PreCustodyOperatorRequired", "PendingVerification", "ReconciliationRequired",
            "NotArmed", "CustodyStateConflict");
        Exact<RawExportR2VerificationDisposition>(
            "Verified", "VerificationIndeterminateRetry", "VerificationFailedRequiresCleanup");
        Exact<RawExportR3StageDisposition>(
            "Staged", "ExistingMatch", "NotFound", "StateConflict", "SourceRetentionNotAuthorized");
        Exact<SourceCommitDisposition>(
            "Committed", "ExistingMatch", "NotFound", "StateConflict", "SourceRetentionNotAuthorized");
        Exact<SourcePublishDisposition>(
            "Available", "ExistingMatch", "NotFound", "StateConflict", "SourceRetentionNotAuthorized");
        Exact<SourceCleanupReadDisposition>(
            "ItemAvailable", "NoPendingItem", "NotFound", "StateConflict");
        Exact<SourceCleanupCompleteDisposition>(
            "Completed", "ExistingMatch", "NotFound", "ResourceNotTerminal", "StateConflict");
        Exact<SourceCleanupFinalizeDisposition>(
            "Completed", "ExistingMatch", "CleanupPending", "NotFound", "StateConflict");
    }

    private static void Exact<T>(params string[] expected) where T : struct, Enum
    {
        var actual = Enum.GetNames<T>();
        Assert.Equal(expected.Length, actual.Length);
        Assert.Equal(expected.Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));
        Assert.Equal(actual.Length, Enum.GetValues<T>().Select(value => Convert.ToInt64(value)).Distinct().Count());
    }
}
