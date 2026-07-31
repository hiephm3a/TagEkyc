using System.Diagnostics;
using System.Security.Cryptography;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

[DebuggerDisplay("AttemptDekLease: <redacted>")]
internal sealed class AttemptDekLease : IAttemptDekLease
{
    private byte[]? buffer;

    private AttemptDekLease(byte[] buffer)
    {
        this.buffer = buffer;
    }

    ~AttemptDekLease()
    {
        Release();
    }

    public ReadOnlyMemory<byte> Material =>
        Volatile.Read(ref buffer) is { } value
            ? value
            : throw new ObjectDisposedException(nameof(AttemptDekLease));

    internal static AttemptDekLease CreateOwned(byte[] material)
    {
        ArgumentNullException.ThrowIfNull(material);
        if (material.Length != 32)
        {
            throw new ArgumentException(
                "An attempt DEK must contain exactly 32 bytes.",
                nameof(material));
        }

        return new(material);
    }

    public void Dispose()
    {
        Release();
        GC.SuppressFinalize(this);
    }

    public override string ToString() => "AttemptDekLease:<redacted>";

    private void Release()
    {
        var owned = Interlocked.Exchange(ref buffer, null);
        if (owned is not null)
        {
            CryptographicOperations.ZeroMemory(owned);
        }
    }
}
