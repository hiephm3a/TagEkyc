namespace TagEkyc.Contracts.RawExport;

public sealed record AttemptKeyReference(
    Guid AttemptKeyReservationId,
    string KeyProviderId,
    string KekId,
    int KekVersion,
    string KekFingerprint);

public interface IAttemptDekLease : IDisposable
{
    ReadOnlyMemory<byte> Material { get; }
}

public enum AttemptKeyFailure
{
    InvalidReference,
    ReferenceMismatch,
    ReservationConflict,
    ProviderFailure,
}

public sealed class AttemptKeyResult
{
    private AttemptKeyResult(
        IAttemptDekLease? dek,
        byte[]? wrappedDekMetadata,
        AttemptKeyFailure? failure)
    {
        Dek = dek;
        WrappedDekMetadata = wrappedDekMetadata;
        Failure = failure;
    }

    public bool IsSuccess => Dek is not null;

    public IAttemptDekLease? Dek { get; }

    public byte[]? WrappedDekMetadata { get; }

    public AttemptKeyFailure? Failure { get; }

    public static AttemptKeyResult Succeeded(
        IAttemptDekLease dek,
        byte[] wrappedDekMetadata)
    {
        ArgumentNullException.ThrowIfNull(dek);
        ArgumentNullException.ThrowIfNull(wrappedDekMetadata);
        return new(dek, (byte[])wrappedDekMetadata.Clone(), null);
    }

    public static AttemptKeyResult Failed(AttemptKeyFailure failure) =>
        new(null, null, failure);
}

public interface IAttemptKeyProvider
{
    Task<AttemptKeyResult> CreateOrGetAttemptKeyAsync(
        AttemptKeyReference reference,
        CancellationToken cancellationToken);
}
