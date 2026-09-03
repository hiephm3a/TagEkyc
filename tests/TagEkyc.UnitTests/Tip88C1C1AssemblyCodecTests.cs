using System.Security.Cryptography;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C1AssemblyCodecTests
{
    [Fact]
    public async Task C1_absolute_assembly_manifest_authentication_and_c2_vectors_match()
    {
        var jobId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var attemptId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var assemblyId = RawExportAssemblyCodec.AssemblyId(jobId);
        var created = DateTimeOffset.Parse("2026-08-15T15:00:00.123456Z");
        var items = new[]
        {
            Item(0, "ChipDg2Portrait", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "cccccccc-cccc-cccc-cccc-cccccccccccc", 1, 3, 0x11),
            Item(1, "LiveSelfieImage", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "dddddddd-dddd-dddd-dddd-dddddddddddd", 2, 2, 0x22),
        };
        var header = new RawExportAssemblyHeader(
            assemblyId,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            created,
            "EncryptedRawVaultRetained",
            jobId,
            1,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            1,
            "SubjectRawBiometricExport",
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            Enumerable.Range(0, 32).Select(value => (byte)value).ToArray(),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            items);
        var sources = new[]
        {
            Source(items[0], [1, 2, 3]),
            Source(items[1], [4, 5]),
        };

        var headerBytes = RawExportAssemblyCodec.SerializeHeader(header);
        var length = RawExportAssemblyCodec.CompleteLength(headerBytes.Length, items);
        var assemblyDigest = await RawExportAssemblyCodec.ComputeAssemblyDigestAsync(header, sources, default);
        var manifestDigest = RawExportAssemblyCodec.ManifestDigest(
            header, attemptId, 7, DateTimeOffset.Parse("2026-08-15T15:30:00Z"),
            1, "fixture-subject-token", 1,
            "fixture-assembly-authentication", 1, assemblyDigest);
        var authPayload = RawExportAssemblyAuthenticationService.BuildAuthenticationPayload(manifestDigest);
        var auth = HMACSHA256.HashData(Enumerable.Range(0, 32).Select(value => (byte)value).ToArray(), authPayload);
        var fingerprint = RawExportAssemblyCodec.AssemblyFingerprint(
            assemblyId, jobId, attemptId, 7, manifestDigest,
            "fixture-assembly-authentication", 1, auth);
        var preparationId = RawExportAssemblyCodec.C2PreparationId(assemblyId, fingerprint);
        var preparationFingerprint = RawExportAssemblyCodec.PreparationFingerprint(
            preparationId, assemblyId, fingerprint, manifestDigest, assemblyDigest, length);

        Assert.Equal(Guid.Parse("aea6e706-6f9d-58ff-b4c3-9a2dc40c43a6"), assemblyId);
        Assert.Equal(1521, headerBytes.Length);
        Assert.Equal(1574, length);
        AssertHex("901a70447c9fe9f7a2884cd473359afef12b2e5d5b17ac1f74b49a7535f25c6f", assemblyDigest);
        AssertHex("eb3ad4b3ddc4b49e33c8e4eff34a5e50f8574a438a70e95fd79d4ec88e7fc41b", manifestDigest);
        Assert.Equal(75, authPayload.Length);
        AssertHex("d73771a5d97f1cd152836d597a5c3ddfbc7ecda007bbd2bbdc41f27996f96be2", auth);
        AssertHex("09dba4977031ec99b7fe89a7b942b13508185015ffe2d45820850a2fd591757f", fingerprint);
        Assert.Equal(Guid.Parse("8e22d1f0-079a-5804-9c7d-63c47190644d"), preparationId);
        AssertHex("3285f039e1fe9ec64e1ebca30e7c86e0b608cd5fd06bafbf76381ee4bd75dd4f", preparationFingerprint);
    }

    private static RawExportAssemblyItemDescriptor Item(
        int ordinal, string rawClass, string source, string capture,
        int revision, long length, byte commitment) =>
        new(ordinal, rawClass, Guid.Parse(source), Guid.Parse(capture), revision,
            "image/jpeg", length, 1, "fixture-content-commitment", 1,
            Enumerable.Repeat(commitment, 32).ToArray());

    private static RawExportAssemblyPlaintextItem Source(
        RawExportAssemblyItemDescriptor descriptor,
        byte[] plaintext) =>
        new(descriptor, async (consume, token) =>
        {
            try
            {
                await consume(plaintext, token);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        });

    private static void AssertHex(string expected, byte[] actual) =>
        Assert.Equal(expected, Convert.ToHexString(actual).ToLowerInvariant());
}
