using TagEkyc.Application.Ports;

namespace TagEkyc.Application.CaptureRuntime;

public enum CaptureRuntimeRouteState { Prepared, Activated }
public sealed record CaptureRuntimeRouteSelection(CaptureRuntimeRouteState State, long Revision);
public sealed record CaptureRuntimeStartupDependencies(string Profile, string State, long Revision,
    DateTimeOffset PreparedAtUtc, DateTimeOffset? ActivatedAtUtc, Guid? ActivatedByCredentialId,
    IReadOnlyList<int> RequiredPepperVersions);

public interface ICaptureRuntimeStartupDependencyReader
{
    Task<CaptureRuntimeStartupDependencies> ReadAsync(DateTimeOffset now, CancellationToken ct);
}

// Implemented by the real A3 composition, not by an always-ready A1 adapter.
public interface ICaptureRuntimeA3Readiness
{
    Task<bool> IsReadyAsync(CancellationToken ct);
}

public interface ICaptureRuntimeAssemblyTopology
{
    string Current { get; }
}

public sealed record CaptureRuntimeAssemblyTopology(string Current) : ICaptureRuntimeAssemblyTopology;

public sealed record CaptureRuntimeActivationEvidenceSeal(
    int FormatVersion,
    long EvidenceRevision,
    string ApprovedPartitionSha256,
    string ApprovedLedgerSha256,
    int ApprovedAuthorityOpenRowCount,
    string BuildPartitionSha256,
    string BuildLedgerSha256,
    int BuildAuthorityOpenRowCount,
    string RatificationManifestSha256,
    string RatificationRecordSha256,
    string? ApprovedAssemblyTopology = null,
    string? BuildAssemblyTopology = null,
    string? ScopeDecisionSha256 = null,
    string? OwnershipTableSha256 = null,
    string? ApprovedSiteTransportQualificationSha256 = null,
    string? BuildSiteTransportQualificationSha256 = null,
    bool ApprovedSiteTransportQualificationRequired = false,
    bool BuildSiteTransportQualificationRequired = false,
    int ApprovedSiteTransportQualificationPolicyVersion = 0,
    int BuildSiteTransportQualificationPolicyVersion = 0);

public interface ICaptureRuntimeActivationEvidenceSealProvider
{
    CaptureRuntimeActivationEvidenceSeal? Current { get; }
}

public sealed record CaptureRuntimeSiteTransportQualification(
    int FormatVersion,
    string QualificationId,
    string SiteId,
    string EndpointOrigin,
    string DeploymentRevision,
    string Status,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset ValidUntilUtc,
    int AgentBodySendsWhileBOrR1Held,
    int ServerApplicationBodyReadsWhileBOrR1Held,
    int RawPostCount,
    bool KestrelContinueRelayedAfterCommit,
    bool EarlyOrIntermediaryContinueObserved,
    bool ApplicationPrebufferObserved,
    bool HiddenRetryObserved,
    string RecordSha256);

public interface ICaptureRuntimeSiteTransportQualificationProvider
{
    CaptureRuntimeSiteTransportQualification? Current { get; }
}

public sealed record CaptureRuntimeSiteTransportQualificationSettings(
    string SiteId,
    string EndpointOrigin,
    string DeploymentRevision,
    TimeSpan ExpiryWarningLeadTime);

public interface ICaptureRuntimeSiteTransportQualificationSettingsProvider
{
    CaptureRuntimeSiteTransportQualificationSettings? Current { get; }
}

public enum CaptureRuntimeSiteTransportQualificationState
{
    NotRequired,
    Qualified,
    Expiring,
    Missing,
    Invalid,
    Expired
}

public sealed record CaptureRuntimeSiteTransportQualificationEvaluation(
    CaptureRuntimeSiteTransportQualificationState State,
    string Code,
    DateTimeOffset? ValidUntilUtc = null)
{
    public bool AllowsRawIngress => State is CaptureRuntimeSiteTransportQualificationState.Qualified
        or CaptureRuntimeSiteTransportQualificationState.Expiring;
}

public static class CaptureRuntimeSiteTransportQualificationPolicy
{
    public const int CurrentVersion = 1;
    public const string InvalidCode = "CAPTURE_RUNTIME_SITE_RAW_INGRESS_TRANSPORT_QUALIFICATION_INVALID";
    public const string ExpiredCode = "CAPTURE_RUNTIME_SITE_RAW_INGRESS_TRANSPORT_QUALIFICATION_EXPIRED";
    public const string ExpiringCode = "CAPTURE_RUNTIME_SITE_RAW_INGRESS_TRANSPORT_QUALIFICATION_EXPIRING";

    public static CaptureRuntimeSiteTransportQualificationEvaluation Evaluate(
        CaptureRuntimeActivationEvidenceSeal? seal,
        CaptureRuntimeSiteTransportQualificationSettings? settings,
        CaptureRuntimeSiteTransportQualification? qualification,
        DateTimeOffset now)
    {
        if (seal is null || seal.ApprovedAuthorityOpenRowCount != 0)
            return new(CaptureRuntimeSiteTransportQualificationState.NotRequired, string.Empty);
        if (!seal.ApprovedSiteTransportQualificationRequired ||
            !seal.BuildSiteTransportQualificationRequired ||
            seal.ApprovedSiteTransportQualificationPolicyVersion != CurrentVersion ||
            seal.BuildSiteTransportQualificationPolicyVersion != CurrentVersion)
            return new(CaptureRuntimeSiteTransportQualificationState.Invalid, InvalidCode);
        if (settings is null || qualification is null)
            return new(CaptureRuntimeSiteTransportQualificationState.Missing, InvalidCode);

        var expectedOrigin = NormalizeHttpsOrigin(settings.EndpointOrigin);
        var observedOrigin = NormalizeHttpsOrigin(qualification.EndpointOrigin);
        if (expectedOrigin is null || observedOrigin is null || expectedOrigin != observedOrigin ||
            string.IsNullOrWhiteSpace(settings.SiteId) ||
            string.IsNullOrWhiteSpace(settings.DeploymentRevision) ||
            qualification.FormatVersion != 1 ||
            qualification.Status != "PASS" ||
            qualification.SiteId != settings.SiteId ||
            qualification.DeploymentRevision != settings.DeploymentRevision ||
            string.IsNullOrWhiteSpace(qualification.QualificationId) ||
            !IsSha256(qualification.RecordSha256) ||
            qualification.ObservedAtUtc > now ||
            qualification.ValidUntilUtc <= qualification.ObservedAtUtc ||
            qualification.AgentBodySendsWhileBOrR1Held != 0 ||
            qualification.ServerApplicationBodyReadsWhileBOrR1Held != 0 ||
            qualification.RawPostCount != 1 ||
            !qualification.KestrelContinueRelayedAfterCommit ||
            qualification.EarlyOrIntermediaryContinueObserved ||
            qualification.ApplicationPrebufferObserved ||
            qualification.HiddenRetryObserved)
            return new(CaptureRuntimeSiteTransportQualificationState.Invalid, InvalidCode,
                qualification.ValidUntilUtc);
        if (qualification.ValidUntilUtc <= now)
            return new(CaptureRuntimeSiteTransportQualificationState.Expired, ExpiredCode,
                qualification.ValidUntilUtc);
        var warningLead = settings.ExpiryWarningLeadTime < TimeSpan.Zero
            ? TimeSpan.Zero : settings.ExpiryWarningLeadTime;
        return qualification.ValidUntilUtc - now <= warningLead
            ? new(CaptureRuntimeSiteTransportQualificationState.Expiring, ExpiringCode,
                qualification.ValidUntilUtc)
            : new(CaptureRuntimeSiteTransportQualificationState.Qualified, string.Empty,
                qualification.ValidUntilUtc);
    }

    private static string? NormalizeHttpsOrigin(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) || uri.AbsolutePath != "/") return null;
        return new UriBuilder(uri.Scheme, uri.IdnHost, uri.IsDefaultPort ? -1 : uri.Port).Uri
            .GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    private static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F');
}

public sealed class CaptureRuntimeStartup(ICaptureRuntimeStartupDependencyReader dependencies,
    ICaptureRuntimeVerifierPepperSource peppers, ICaptureRuntimeA3Readiness? a3 = null,
    ICaptureRuntimeRawIngressAdmission? admission = null,
    ICaptureRuntimeActivationEvidenceSealProvider? activationEvidence = null,
    ICaptureRuntimeAssemblyTopology? assemblyTopology = null,
    ICaptureRuntimeSiteTransportQualificationProvider? siteTransportQualification = null,
    ICaptureRuntimeSiteTransportQualificationSettingsProvider? siteTransportSettings = null)
{
    public async Task<CaptureRuntimeRouteSelection> SelectAsync(DateTimeOffset now, CancellationToken ct)
    {
        // These providers are consumed by the live per-request gate, not by
        // startup. Retaining them in this composition keeps older callers and
        // test wiring source-compatible while making hot qualification possible.
        _ = siteTransportQualification;
        _ = siteTransportSettings;
        var row = await dependencies.ReadAsync(now, ct);
        if (row.Profile != "Managed" || row.Revision <= 0 || row.PreparedAtUtc == default ||
            row.PreparedAtUtc > now || row.RequiredPepperVersions is null ||
            row.RequiredPepperVersions.Any(v => v <= 0) ||
            !row.RequiredPepperVersions.SequenceEqual(row.RequiredPepperVersions.Distinct().Order()))
            throw NotReady();
        var state = row.State switch
        {
            "Prepared" when row.ActivatedAtUtc is null && row.ActivatedByCredentialId is null => CaptureRuntimeRouteState.Prepared,
            "Activated" when row.ActivatedAtUtc is { } at && at >= row.PreparedAtUtc && at <= now &&
                row.ActivatedByCredentialId is { } actor && actor != Guid.Empty => CaptureRuntimeRouteState.Activated,
            _ => throw NotReady()
        };
        if (state == CaptureRuntimeRouteState.Activated)
        {
            var seal = activationEvidence?.Current;
            if (!IsValid(seal, assemblyTopology?.Current)) throw ActivationEvidenceInvalid();
            if (seal!.ApprovedAuthorityOpenRowCount != 0) throw ActivationEvidenceIncomplete();
            // Site evidence is intentionally enforced by the per-request raw-ingress
            // gate.  Keeping the host and its health endpoint alive is what makes an
            // initial qualification or a hot record renewal possible without a
            // per-site build or restart.  Invalid seal/policy bytes still fail above.
        }
        var current = peppers.CurrentVersion;
        if (current <= 0) throw NotReady();
        foreach (var version in row.RequiredPepperVersions.Append(current).Distinct().Order())
        foreach (var domain in new[] { CaptureRuntimeVerifierPepperDomain.PlatformCredential,
            CaptureRuntimeVerifierPepperDomain.BootstrapDigest, CaptureRuntimeVerifierPepperDomain.CapabilityDigest })
        {
            using var lease = await peppers.TryResolveAsync(version, domain, ct);
            if (lease is null || lease.Version != version || lease.Domain != domain || lease.Key.Length != 32)
                throw NotReady();
        }
        if (state == CaptureRuntimeRouteState.Activated && (admission is null || a3 is null || !await a3.IsReadyAsync(ct)))
            throw NotReady();
        return new(state, row.Revision);
    }

    private static InvalidOperationException NotReady() => new("CAPTURE_RUNTIME_STARTUP_NOT_READY");
    private static InvalidOperationException ActivationEvidenceIncomplete() =>
        new("CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE");
    private static InvalidOperationException ActivationEvidenceInvalid() =>
        new("CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INVALID");
    private static bool IsValid(CaptureRuntimeActivationEvidenceSeal? seal, string? effectiveAssemblyTopology) =>
        seal is not null &&
        (seal.FormatVersion == 1 && seal.ApprovedAuthorityOpenRowCount > 0 ||
         seal.FormatVersion == 2 &&
         seal.ApprovedAssemblyTopology is "Disabled" or "DurableWorker" &&
         seal.BuildAssemblyTopology == seal.ApprovedAssemblyTopology &&
         effectiveAssemblyTopology == seal.ApprovedAssemblyTopology &&
         IsSha256(seal.ScopeDecisionSha256) &&
         IsSha256(seal.OwnershipTableSha256) &&
         (seal.ApprovedAuthorityOpenRowCount > 0 ||
          seal.ApprovedSiteTransportQualificationRequired &&
          seal.BuildSiteTransportQualificationRequired == seal.ApprovedSiteTransportQualificationRequired &&
          seal.ApprovedSiteTransportQualificationPolicyVersion ==
              CaptureRuntimeSiteTransportQualificationPolicy.CurrentVersion &&
          seal.BuildSiteTransportQualificationPolicyVersion ==
              seal.ApprovedSiteTransportQualificationPolicyVersion)) &&
        seal.EvidenceRevision > 0 &&
        seal.ApprovedAuthorityOpenRowCount >= 0 &&
        seal.BuildAuthorityOpenRowCount >= 0 &&
        IsSha256(seal.ApprovedPartitionSha256) &&
        IsSha256(seal.ApprovedLedgerSha256) &&
        IsSha256(seal.BuildPartitionSha256) &&
        IsSha256(seal.BuildLedgerSha256) &&
        IsSha256(seal.RatificationManifestSha256) &&
        IsSha256(seal.RatificationRecordSha256) &&
        seal.ApprovedPartitionSha256 == seal.BuildPartitionSha256 &&
        seal.ApprovedLedgerSha256 == seal.BuildLedgerSha256 &&
        seal.ApprovedAuthorityOpenRowCount == seal.BuildAuthorityOpenRowCount;

    private static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F');
}
