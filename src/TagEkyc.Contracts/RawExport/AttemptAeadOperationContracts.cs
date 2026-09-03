namespace TagEkyc.Contracts.RawExport;

public sealed record AttemptAeadChunkRequest(
    Guid AttemptKeyReservationId,
    ReadOnlyMemory<byte> Input,
    ReadOnlyMemory<byte> Nonce,
    ReadOnlyMemory<byte> AssociatedData);

public sealed record AttemptAeadChunkResult(byte[] Output, byte[] AuthenticationTag);

public enum AttemptAeadVerificationOutcome
{
    Verified,
    KeyAccessIndeterminate,
    AuthenticationFailed,
}

public sealed class AttemptAeadVerificationResult
{
    private AttemptAeadVerificationResult(
        AttemptAeadVerificationOutcome outcome,
        byte[]? output)
    {
        if ((outcome == AttemptAeadVerificationOutcome.Verified) != (output is not null))
        {
            throw new ArgumentException(
                "Verified AEAD results alone must contain output.",
                nameof(output));
        }

        Outcome = outcome;
        Output = output;
    }

    public AttemptAeadVerificationOutcome Outcome { get; }

    public byte[]? Output { get; }

    public static AttemptAeadVerificationResult Verified(byte[] output)
    {
        ArgumentNullException.ThrowIfNull(output);
        return new(AttemptAeadVerificationOutcome.Verified, output);
    }

    public static AttemptAeadVerificationResult KeyAccessIndeterminate() =>
        new(AttemptAeadVerificationOutcome.KeyAccessIndeterminate, null);

    public static AttemptAeadVerificationResult AuthenticationFailed() =>
        new(AttemptAeadVerificationOutcome.AuthenticationFailed, null);

    public override string ToString() =>
        $"AttemptAeadVerificationResult:{Outcome}:<redacted>";
}

public interface IAttemptAeadEncryptionOperation
{
    Task<AttemptAeadChunkResult> EncryptBoundedChunkAsync(
        AttemptAeadChunkRequest request,
        CancellationToken cancellationToken);
}

public interface IAttemptAeadVerificationOperation
{
    Task<AttemptAeadVerificationResult> DecryptAndVerifyBoundedChunkAsync(
        AttemptAeadChunkRequest request,
        ReadOnlyMemory<byte> authenticationTag,
        CancellationToken cancellationToken);
}
