using System.Security.Cryptography;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C2RecipientPackageCodecTests
{
    [Fact]
    public void C205_package_key_and_provider_binding_match_absolute_vector()
    {
        var equality = RecipientPackageCodec.PackageEqualityFingerprint(
            Guid.ParseExact(new string('1', 32), "N"),
            Guid.ParseExact(new string('3', 32), "N"),
            Bytes("44", 32), Bytes("55", 32), Bytes("66", 32), Bytes("77", 32),
            Guid.ParseExact(new string('7', 32), "N"), "recipient-rsa-2026-v1", 1, Bytes("88", 32), 5);
        Assert.Equal("556411F661E7D2F0A32334A6666F0A9873CDCDB5C9E5CC9F8BE5267BA09DF1A8", Convert.ToHexString(equality));
        var packageId = RecipientPackageCodec.PackageId(Guid.ParseExact(new string('1', 32), "N"), equality);
        Assert.Equal(Guid.Parse("ad8b18c9-0119-5c71-bf0b-208e69cd6cba"), packageId);
        Assert.Equal("raw-export/c2-package/v1/ad8b18c901195c71bf0b208e69cd6cba", RecipientPackageCodec.ObjectKey(packageId));

        var provider = Provider();
        var endpoint = RecipientPackageCodec.ProviderEndpointFingerprint(provider);
        Assert.Equal("B4F942AFACDA0BB25A8FF90BE8ACBD004FBFF3B3433A526024AB6BFB5E32A890", Convert.ToHexString(endpoint));
        var binding = RecipientPackageCodec.ObjectBindingDigest(
            provider.ProviderConfigurationId, endpoint, provider.BucketName, RecipientPackageCodec.ObjectKey(packageId));
        Assert.Equal("19EEB2893CA64A56D69C8B39F98F27CAAAE826248B3BE07AAAD45215B85C30B2", Convert.ToHexString(binding));
    }

    [Fact]
    public void C207_operation_token_and_conditional_evidence_are_all_member_sensitive()
    {
        var token = Enumerable.Range(0x20, 32).Select(value => (byte)value).ToArray();
        var digest = RecipientPackageCodec.ProviderOperationTokenDigest(token);
        Assert.Equal("72DBB7336C76780023F83DA4C355F2EEEA85733B13D3477697917790C1229084", Convert.ToHexString(digest));
        token[0] ^= 1;
        Assert.NotEqual(digest, RecipientPackageCodec.ProviderOperationTokenDigest(token));

        var conditional = RecipientPackageCodec.ConditionalCreateEvidenceDigest(
            Convert.FromHexString("19eeb2893ca64a56d69c8b39f98f27caaae826248b3be07aaad45215b85c30b2"),
            1641,
            Convert.FromHexString("d9cfc7a064ea0108b6db3bfe2069f8afda6d8dda1b6c444d9036f8fe217f75ad"),
            SHA256.HashData("c2-vector-etag"u8));
        Assert.Equal("2A0DFA94BC2FBE31671D54B743E7EA0BF3AF20E36F6C1AA7DCAD766AF6CD6D59", Convert.ToHexString(conditional));
        var changed = RecipientPackageCodec.ConditionalCreateEvidenceDigest(
            Convert.FromHexString("19eeb2893ca64a56d69c8b39f98f27caaae826248b3be07aaad45215b85c30b2"),
            1642,
            Convert.FromHexString("d9cfc7a064ea0108b6db3bfe2069f8afda6d8dda1b6c444d9036f8fe217f75ad"),
            SHA256.HashData("c2-vector-etag"u8));
        Assert.NotEqual(conditional, changed);
    }

    [Fact]
    public void C209_fixed_bounds_and_configuration_have_no_mutable_object_prefix()
    {
        Assert.Equal(1_048_576, RecipientPackageOptions.FramePlaintextBytes);
        Assert.Equal(32, RecipientPackageOptions.MaximumDataFrameCount);
        Assert.Equal(1_848, RecipientPackageOptions.MaximumHeaderLength);
        Assert.Equal(33_554_432, RecipientPackageOptions.MaximumCompleteAssemblyLength);
        Assert.Equal(33_557_106, RecipientPackageOptions.MaximumEncryptedPackageLength);
        Assert.DoesNotContain(typeof(RecipientPackageProviderConfiguration).GetProperties(),
            property => property.Name.Contains("Prefix", StringComparison.OrdinalIgnoreCase));
    }

    private static RecipientPackageProviderConfiguration Provider() => new(
        "c2-vector-minio-v1", new Uri("http://127.0.0.1:9000/"), "tagekyc-c2-vector", true, "us-east-1",
        new("writer", "secret"), new("reader", "secret"), new("lifecycle", "secret"), new("posture", "secret"), true);
    private static byte[] Bytes(string value, int count) => Convert.FromHexString(string.Concat(Enumerable.Repeat(value, count)));
}
