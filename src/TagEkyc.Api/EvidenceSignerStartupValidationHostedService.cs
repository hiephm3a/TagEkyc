using TagEkyc.Application.Ports;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.Api;

internal sealed class EvidenceSignerStartupValidationHostedService(
    IEvidenceSigner evidenceSigner,
    IEs256JwksProvider jwksProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (evidenceSigner is IEvidenceSignerStartupValidator validator)
        {
            validator.ValidateStartup(cancellationToken);
        }

        jwksProvider.ValidateStartup(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
