// Copied from SignFlow ProtectedValues (Codex_SignFlow) — TagEkyc-owned fork;
// do not add a SignFlow project reference.
using System.Diagnostics;
using System.Security.Cryptography;

namespace TagEkyc.Infrastructure.ProtectedValues;

[DebuggerDisplay("ProtectedValueMaterialLease: <redacted>")]
internal sealed class ProtectedValueMaterialLease : IDisposable
{
    internal const int AbsoluteMaximumBytes = 1024 * 1024;

    private byte[]? _buffer;

    private ProtectedValueMaterialLease(byte[] buffer)
    {
        _buffer = buffer;
    }

    ~ProtectedValueMaterialLease()
    {
        Release();
    }

    internal ReadOnlyMemory<byte> Material
    {
        get
        {
            var buffer = Volatile.Read(ref _buffer);
            return buffer is null
                ? throw new ObjectDisposedException(
                    nameof(ProtectedValueMaterialLease))
                : buffer;
        }
    }

    internal static ProtectedValueMaterialLease CreateOwned(byte[]? material)
    {
        ArgumentNullException.ThrowIfNull(material);
        if (material.Length is 0 or > AbsoluteMaximumBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(material),
                "Material length is outside the allowed range.");
        }

        return new ProtectedValueMaterialLease(material);
    }

    public void Dispose()
    {
        Release();
        GC.SuppressFinalize(this);
    }

    public override string ToString() =>
        "ProtectedValueMaterialLease:<redacted>";

    private void Release()
    {
        var buffer = Interlocked.Exchange(ref _buffer, null);
        if (buffer is not null)
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }
}
