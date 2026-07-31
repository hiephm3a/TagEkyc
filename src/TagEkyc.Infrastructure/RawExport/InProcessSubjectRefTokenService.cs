using System.Security.Cryptography;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.ProtectedValues;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class InProcessSubjectRefTokenService :
    ISubjectRefTokenService
{
    private readonly IProtectedValueResolver _resolver;

    public InProcessSubjectRefTokenService(
        IProtectedValueResolver resolver)
    {
        _resolver =
            resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public async ValueTask<SubjectRefTokenResult> ComputeAsync(
        SubjectTokenKeySelector selector,
        ReadOnlyMemory<byte> lpPayload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);
        cancellationToken.ThrowIfCancellationRequested();

        ProtectedValueResolution resolution;
        try
        {
            resolution = await _resolver.ResolveAsync(
                FixtureSubjectTokenCatalog.CreateRequest(
                    selector.KeyId,
                    selector.KeyVersion),
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return SubjectRefTokenResult.Failed(
                SubjectRefTokenFailure.ProviderFailure);
        }

        if (resolution.State != ProtectedValueResolutionState.Found
            || resolution.MaterialLease is null)
        {
            resolution.MaterialLease?.Dispose();
            return SubjectRefTokenResult.Failed(
                SubjectRefTokenFailure.ProviderFailure);
        }

        using var lease = resolution.MaterialLease;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var token = HMACSHA256.HashData(
                lease.Material.Span,
                lpPayload.Span);
            return SubjectRefTokenResult.Success(token);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return SubjectRefTokenResult.Failed(
                SubjectRefTokenFailure.ProviderFailure);
        }
    }
}
