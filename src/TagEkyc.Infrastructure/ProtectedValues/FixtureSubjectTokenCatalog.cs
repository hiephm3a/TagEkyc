namespace TagEkyc.Infrastructure.ProtectedValues;

internal sealed class FixtureSubjectTokenCatalog :
    IProtectedValueCatalog
{
    internal const string FixtureKeyId =
        "fixture-subject-ref-token";

    internal const int FixtureKeyVersion = 1;

    internal const string FixtureConfigurationPath =
        "TagEkyc:RawExport:SubjectRefToken:" +
        "FixtureKeys:fixture-subject-ref-token:1";

    internal static ProtectedValueRequest CreateRequest(
        string keyId,
        int keyVersion) =>
        new(
            ProtectedValuePurpose.SubjectRefTokenHmac,
            new ProtectedValueId($"{keyId}:{keyVersion}"));

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
