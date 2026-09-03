using System.Security.Cryptography;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.ProtectedValues;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class InProcessContentCommitmentService :
    IContentCommitmentService
{
    private readonly IProtectedValueResolver _resolver;

    public InProcessContentCommitmentService(
        IProtectedValueResolver resolver)
    {
        _resolver =
            resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public async ValueTask<ContentCommitmentResult> ComputeAsync(
        CommitmentKeySelector selector,
        ReadOnlyMemory<byte> lpPayload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);
        cancellationToken.ThrowIfCancellationRequested();

        ProtectedValueResolution resolution;
        try
        {
            resolution = await _resolver.ResolveAsync(
                FixtureContentCommitmentCatalog.CreateRequest(
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
            return ContentCommitmentResult.Failed(
                ContentCommitmentFailure.ProviderFailure);
        }

        if (resolution.State != ProtectedValueResolutionState.Found
            || resolution.MaterialLease is null)
        {
            resolution.MaterialLease?.Dispose();
            return ContentCommitmentResult.Failed(
                ContentCommitmentFailure.ProviderFailure);
        }

        using var lease = resolution.MaterialLease;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var mac = HMACSHA256.HashData(
                lease.Material.Span,
                lpPayload.Span);
            return ContentCommitmentResult.Success(mac);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return ContentCommitmentResult.Failed(
                ContentCommitmentFailure.ProviderFailure);
        }
    }
}
