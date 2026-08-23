using System.Reflection;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1C4RecipientPackageReferenceArchTests
{
    [Fact]
    public void C421_cursor_key_lease_is_disposable_and_redacted()
    {
        var leaseType = typeof(RecipientPackageReferenceOptions).Assembly.GetType(
            "TagEkyc.Infrastructure.RawExport.RecipientPackageReferenceKeyLease", throwOnError: true)!;
        Assert.True(typeof(IDisposable).IsAssignableFrom(leaseType),
            $"C421-DISPOSABLE-TYPE actual={leaseType.FullName}");

        var material = Enumerable.Repeat((byte)0xA5, 32).ToArray();
        var lease = Activator.CreateInstance(
            leaseType,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            args: [material],
            culture: null) ?? throw new InvalidOperationException("C421 could not construct the runtime key lease.");
        var materialProperty = leaseType.GetProperty("Material", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("C421 could not observe the runtime key material.");
        var observedMaterial = materialProperty.GetValue(lease);
        Assert.True(observedMaterial?.GetType() == typeof(byte[]),
            $"C421-MATERIAL-TYPE expected={typeof(byte[]).FullName} actual={observedMaterial?.GetType().FullName ?? "<null>"}");
        var liveMaterial = (byte[])observedMaterial!;
        Assert.True(ReferenceEquals(material, liveMaterial), "C421-MATERIAL-IDENTITY observed=different-buffer");
        Assert.True(liveMaterial.Any(value => value != 0),
            $"C421-MATERIAL-PRE-DISPOSE-NONZERO observed={Convert.ToHexString(liveMaterial)}");

        var display = lease.ToString() ?? "<null>";
        Assert.True(string.Equals("RecipientPackageReferenceKeyLease:<redacted>", display, StringComparison.Ordinal),
            $"C421-REDACTED-DISPLAY actual={display}");
        Assert.True(!display.Contains(Convert.ToHexString(material), StringComparison.OrdinalIgnoreCase),
            $"C421-HEX-REDACTION actual={display}");
        Assert.True(!display.Contains(Convert.ToBase64String(material), StringComparison.Ordinal),
            $"C421-BASE64-REDACTION actual={display}");

        ((IDisposable)lease).Dispose();
        Assert.True(material.All(value => value == 0),
            $"C421-ZEROIZATION observed={Convert.ToHexString(material)}");
        Exception? disposedFailure = null;
        try { _ = materialProperty.GetValue(lease); }
        catch (Exception caught) { disposedFailure = caught; }
        Assert.True(disposedFailure?.GetType() == typeof(TargetInvocationException),
            $"C421-POST-DISPOSE-OUTER-EXCEPTION expected={typeof(TargetInvocationException).FullName} actual={disposedFailure?.GetType().FullName ?? "<none>"}");
        var disposed = (TargetInvocationException)disposedFailure!;
        Assert.True(disposed.InnerException?.GetType() == typeof(ObjectDisposedException),
            $"C421-POST-DISPOSE-INNER-EXCEPTION expected={typeof(ObjectDisposedException).FullName} actual={disposed.InnerException?.GetType().FullName ?? "<none>"}");
        ((IDisposable)lease).Dispose();
    }

    [Fact]
    public void C423_c4_has_no_raw_bio_cek_private_key_or_c2_c3_mutation_dependency()
    {
        var names = typeof(RecipientPackageReferenceOptions).Assembly.GetTypes()
            .Where(type => type.Name.Contains("RecipientPackageReference", StringComparison.Ordinal))
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Select(member => member.Name).ToArray();
        var forbiddenNames = names.Where(name => new[] { "RawBio", "Plaintext", "PrivateKey", "UnwrapCek" }
            .Any(forbidden => name.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
            || name is "Finalize" or "FinalizeAsync" or "CreateDelivery" or "CreateDeliveryAsync").ToArray();
        Assert.True(forbiddenNames.Length == 0,
            $"C423-FORBIDDEN-MEMBER-NAMES=[{string.Join(",", forbiddenNames)}]");
    }
}
