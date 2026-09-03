// Copied from SignFlow ProtectedValues (Codex_SignFlow) — TagEkyc-owned fork;
// do not add a SignFlow project reference.
namespace TagEkyc.Infrastructure.ProtectedValues;

internal sealed class FixtureContentCommitmentCatalog :
    IProtectedValueCatalog
{
    internal const string FixtureKeyId =
        "fixture-content-commitment";

    internal const int FixtureKeyVersion = 1;

    internal const string FixtureConfigurationPath =
        "TagEkyc:RawExport:ContentCommitment:" +
        "FixtureKeys:fixture-content-commitment:1";

    public FixtureContentCommitmentCatalog()
    {
    }

    internal static ProtectedValueRequest CreateRequest(
        string keyId,
        int keyVersion)
    {
        return new ProtectedValueRequest(
            ProtectedValuePurpose.ContentCommitmentHmac,
            new ProtectedValueId(
                $"{keyId}:{keyVersion}"));
    }

    public ValueTask<ProtectedValueDescriptor?> FindAsync(
        ProtectedValueRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var expectedRequest = CreateRequest(
            FixtureKeyId,
            FixtureKeyVersion);
        if (request != expectedRequest)
        {
            return ValueTask.FromResult<ProtectedValueDescriptor?>(null);
        }

        return ValueTask.FromResult<ProtectedValueDescriptor?>(
            new ProtectedValueDescriptor(
                expectedRequest,
                new ProtectedValueReference(
                    $"config:{FixtureConfigurationPath}")));
    }
}
