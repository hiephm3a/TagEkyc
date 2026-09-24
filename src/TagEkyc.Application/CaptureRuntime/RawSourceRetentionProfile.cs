using System.Text;

namespace TagEkyc.Application.CaptureRuntime;

public sealed record RawSourceRetentionProfile(
    Guid ClientApplicationId, Guid PolicyId, int PolicyVersion, string[] RawClasses,
    string ControllerIdentity, string StableDataScopeId, string RetentionPolicyId,
    int RetentionPolicyVersion, string RetentionClass, string RevocationPolicyId,
    string PurgePolicyId, string LegalHoldPolicyId, int MaximumRetentionSeconds)
{
    public bool IsValid() =>
        ClientApplicationId != Guid.Empty && PolicyId != Guid.Empty && PolicyVersion > 0 &&
        RetentionPolicyVersion > 0 && MaximumRetentionSeconds is >= 1 and <= 31536000 &&
        RawClasses is ["ChipDg2Portrait", "LiveSelfieImage"] &&
        TextValid(ControllerIdentity, 128) && TextValid(StableDataScopeId, 128) &&
        TextValid(RetentionPolicyId, 128) && TextValid(RetentionClass, 64) &&
        TextValid(RevocationPolicyId, 128) && TextValid(PurgePolicyId, 128) &&
        TextValid(LegalHoldPolicyId, 128);

    internal static bool TextValid(string? value, int maximumBytes) =>
        !string.IsNullOrWhiteSpace(value) && Encoding.UTF8.GetByteCount(value) <= maximumBytes &&
        !value.Any(c => c is '\0' or '\r' or '\n');

    public RawSourceRetentionProfile Copy() => this with { RawClasses = RawClasses.ToArray() };
}

public interface IRawSourceRetentionProfileProvider
{
    RawSourceRetentionProfile? Find(Guid clientApplicationId);
    bool IsReady { get; }
}
