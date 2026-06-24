using TagEkyc.Application.Ports;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.Api;

internal sealed class EvidenceSignerStartupValidationHostedService(IEvidenceSigner evidenceSigner) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (evidenceSigner is Pkcs11Es256JwsEvidenceSigner pkcs11Signer)
        {
            pkcs11Signer.ValidateToken(cancellationToken);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
