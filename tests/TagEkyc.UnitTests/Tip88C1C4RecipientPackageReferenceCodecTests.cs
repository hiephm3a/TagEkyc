using System.Security.Cryptography;
using System.Text;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C4RecipientPackageReferenceCodecTests
{
    [Fact]
    public void C405_cursor_absolute_vector_and_canonical_wire_shape_are_byte_exact()
    {
        var key = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var value = new RecipientPackageReferenceCursor(
            "c4-cursor-01", 1,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"), 50,
            DateTimeOffset.Parse("2026-08-19T12:34:56.7890120Z", System.Globalization.CultureInfo.InvariantCulture),
            Guid.Parse("33333333-3333-3333-3333-333333333333"), 1787142900, 1787143800);
        var token = RecipientPackageReferenceCursorCodec.Encode(value, key);
        Assert.Equal(175, token.Length);
        Assert.Equal("QzRSMQEMYzQtY3Vyc29yLTAxAAAAAQABERERERERERERERERERERESIiIiIiIiIiIiIiIiIiIiIAMgje_e5G9_zIMzMzMzMzMzMzMzMzMzMzMwAAAABqhaL0AAAAAGqFpng.AD_dtILRjPs-fuxHzkZmpmFJYhCp_1X9gADzdsTUv8M", token);
        var parsed = RecipientPackageReferenceCursorCodec.ParseStructure(token);
        try
        {
            Assert.Equal(98, parsed.Payload?.Length);
            Assert.Equal("ffbb3a933b712974dff4959ad02e856fbe4417a3eb2ff795cdb34b657ff11eaa",
                Convert.ToHexString(SHA256.HashData(BuildPreimage(parsed.Payload!))).ToLowerInvariant());
            Assert.True(RecipientPackageReferenceCursorCodec.Verify(parsed, key));
            Assert.Equal(value, parsed.Cursor);
        }
        finally { RecipientPackageReferenceCursorCodec.Release(parsed); }
    }

    private static byte[] BuildPreimage(byte[] payload) =>
        Encoding.ASCII.GetBytes(RecipientPackageReferenceCursorCodec.MacLabel).Concat([byte.MinValue]).Concat(payload).ToArray();
}
