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

public sealed class CaptureRuntimeStartup(ICaptureRuntimeStartupDependencyReader dependencies,
    ICaptureRuntimeVerifierPepperSource peppers, ICaptureRuntimeA3Readiness? a3 = null,
    ICaptureRuntimeRawIngressAdmission? admission = null)
{
    public async Task<CaptureRuntimeRouteSelection> SelectAsync(DateTimeOffset now, CancellationToken ct)
    {
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
}
