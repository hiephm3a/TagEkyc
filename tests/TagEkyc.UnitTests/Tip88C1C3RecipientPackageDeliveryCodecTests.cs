using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C3RecipientPackageDeliveryCodecTests
{
    [Fact]
    public void C304_idempotency_digest_delivery_id_and_equality_vectors_are_exact()
    {
        var digest = RecipientPackageDeliveryCodec.IdempotencyKeyDigest("C3-Key-01");
        Assert.Equal("146769E1BF7A74ED89C039E8ED331E7DDEEC0314B009EE73AE7F3D137605BB4C", Convert.ToHexString(digest));
        var canonical = TagEkyc.Application.VerificationSessions.EvidenceCanonicalization.Canonicalize(new
        {
            idempotencyKeyDigest = Convert.ToHexString(digest).ToLowerInvariant(),
            recipientClientApplicationId = "33333333333333333333333333333333",
        });
        var labeled = System.Text.Encoding.UTF8.GetBytes($"tip-88c1-c3-delivery-id-v1\n{canonical}");
        Assert.Equal("8D07A60A5783B6337C61373DC75C022336E640CDC3CCAA1D6C8693251AB5E12F",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(labeled)));
        Assert.Equal(Guid.Parse("0aa6078d-8357-53b6-bc61-373dc75c0223"),
            RecipientPackageDeliveryCodec.DeliveryId(Guid.Parse("33333333-3333-3333-3333-333333333333"), digest));
        Assert.NotEqual(digest, RecipientPackageDeliveryCodec.IdempotencyKeyDigest(" C3-Key-01"));
        Assert.NotEqual(digest, RecipientPackageDeliveryCodec.IdempotencyKeyDigest("C3-Key-01 "));
        Assert.DoesNotContain(typeof(TagEkyc.Infrastructure.Persistence.Entities.RawExportRecipientPackageDeliveryRow).GetProperties(),
            property => property.PropertyType == typeof(string)
                && property.Name.Contains("Idempotency", StringComparison.Ordinal));
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(labeled);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(digest);
    }
}
