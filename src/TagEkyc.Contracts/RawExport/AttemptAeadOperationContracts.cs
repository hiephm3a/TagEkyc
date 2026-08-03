namespace TagEkyc.Contracts.RawExport;

public sealed record AttemptAeadChunkRequest(
    Guid AttemptKeyReservationId,
    ReadOnlyMemory<byte> Input,
    ReadOnlyMemory<byte> Nonce,
    ReadOnlyMemory<byte> AssociatedData);

public sealed record AttemptAeadChunkResult(byte[] Output, byte[] AuthenticationTag);

public interface IAttemptAeadEncryptionOperation
{
    Task<AttemptAeadChunkResult> EncryptBoundedChunkAsync(
        AttemptAeadChunkRequest request,
        CancellationToken cancellationToken);
}

public interface IAttemptAeadVerificationOperation
{
    Task<byte[]> DecryptAndVerifyBoundedChunkAsync(
        AttemptAeadChunkRequest request,
        ReadOnlyMemory<byte> authenticationTag,
        CancellationToken cancellationToken);
}
