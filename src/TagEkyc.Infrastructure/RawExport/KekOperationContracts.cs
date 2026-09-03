namespace TagEkyc.Infrastructure.RawExport;

internal readonly struct ProviderOperationToken(string value)
{
    internal string Value { get; } = DurableKeyText.IsValid(value)
        && value.Length == 43
        ? value
        : throw new ArgumentException("Invalid provider-operation token.", nameof(value));

    public override string ToString() => "[REDACTED:provider-operation-token]";
}

internal sealed record KekReference(
    string KeyProviderId,
    string KekId,
    int KekVersion,
    string KekFingerprint);

internal interface IAttemptDekCandidate : IDisposable
{
    ReadOnlyMemory<byte> Material { get; }
}

internal sealed record KekWrappedMaterial(
    byte[] Ciphertext,
    byte[] Nonce,
    byte[] Tag,
    string SuiteId,
    int SuiteVersion,
    string ProviderResourceReference,
    string Receipt)
{
    public override string ToString() => "KekWrappedMaterial { [REDACTED] }";
}

internal abstract record KekWrapResult
{
    internal sealed record Wrapped(KekWrappedMaterial Material) : KekWrapResult;
    internal sealed record Unavailable : KekWrapResult;
    internal sealed record OutcomeUnknown : KekWrapResult;
    internal sealed record CorruptOrUnverifiable : KekWrapResult;
}

internal abstract record KekOperationLookup
{
    internal sealed record Found(KekWrappedMaterial Material) : KekOperationLookup;
    internal sealed record PositivelyAbsent(string AbsenceProofReceipt) : KekOperationLookup;
    internal sealed record Unknown : KekOperationLookup;
    internal sealed record CorruptOrUnverifiable : KekOperationLookup;
}

internal interface IKekOperationProvider
{
    Task<KekWrapResult> WrapDekAsync(KekReference reference, ProviderOperationToken providerOperationToken,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint, IAttemptDekCandidate candidate,
        CancellationToken cancellationToken);
    Task<KekOperationLookup> LookupByOperationTokenAsync(ProviderOperationToken providerOperationToken,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken cancellationToken);
    Task<AttemptDekLease> UnwrapDekAsync(KekReference reference, KekWrappedMaterial wrapped,
        ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken cancellationToken);
}

internal sealed record DurableKekProviderCapabilities(
    bool IsDurable,
    bool IsKekQualified,
    bool SupportsRecovery,
    bool SupportsCleanup);

internal interface IDurableKekProviderCapabilitySource
{
    DurableKekProviderCapabilities Capabilities { get; }
}
