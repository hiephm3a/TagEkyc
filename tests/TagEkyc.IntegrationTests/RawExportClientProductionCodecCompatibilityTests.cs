using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.RawExport;
using TagEkyc.RawExport.Client;

namespace TagEkyc.IntegrationTests;

public sealed class RawExportClientProductionCodecCompatibilityTests
{
    private static readonly Guid SessionId = Guid.Parse("22000000-0000-0000-0000-000000000001");
    private static readonly Guid RecipientId = Guid.Parse("22000000-0000-0000-0000-000000000002");
    private static readonly Guid PermitId = Guid.Parse("22000000-0000-0000-0000-000000000003");
    private static readonly Guid JobId = Guid.Parse("22000000-0000-0000-0000-000000000004");
    private static readonly Guid PackageId = Guid.Parse("22000000-0000-0000-0000-000000000005");
    private static readonly Guid DeliveryId = Guid.Parse("22000000-0000-0000-0000-000000000006");

    [Fact]
    public async Task Standalone_client_decrypts_package_written_by_production_assembly_and_crypto_codecs()
    {
        byte[] portrait = [1, 3, 5, 7, 9];
        byte[] selfie = [2, 4, 6, 8, 10, 12];
        using var recipient = RSA.Create(3072);
        var package = await ProductionPackageAsync(recipient, portrait, selfie);
        var handler = new FlowHandler(package);
        var client = new TagEkycRawExportClient(
            new HttpClient(handler),
            new TagEkycRawExportClientOptions(
                new Uri("https://tag.example.test"), "X-API-Key", "api-secret",
                Guid.Parse("22000000-0000-0000-0000-000000000007"), 1,
                RecipientId, "recipient-key", 1, TimeSpan.Zero, 1),
            new PrivateKeySource(recipient.ExportPkcs8PrivateKey()));

        using var lease = await client.AcquireAsync(new(SessionId, Guid.NewGuid()));

        Assert.Equal(portrait, lease.ChipDg2Portrait.ToArray());
        Assert.Equal(selfie, lease.LiveSelfieImage.ToArray());
        Assert.Equal(JobId, lease.JobId);
        Assert.Equal(PackageId, lease.PackageId);
    }

    private static async Task<byte[]> ProductionPackageAsync(RSA recipient, byte[] portrait, byte[] selfie)
    {
        var assemblyId = RawExportAssemblyCodec.AssemblyId(JobId);
        var descriptors = new[]
        {
            Descriptor(0, "ChipDg2Portrait", portrait),
            Descriptor(1, "LiveSelfieImage", selfie),
        };
        var header = new RawExportAssemblyHeader(
            assemblyId,
            Guid.Parse("22000000-0000-0000-0000-000000000008"),
            DateTimeOffset.Parse("2026-09-23T00:00:00Z"),
            "EncryptedExportPacket",
            JobId,
            1,
            PermitId,
            Guid.Parse("22000000-0000-0000-0000-000000000007"),
            1,
            "Signing",
            RecipientId,
            SHA256.HashData("subject"u8),
            SessionId,
            descriptors);
        var plaintextItems = new[]
        {
            Plaintext(descriptors[0], portrait),
            Plaintext(descriptors[1], selfie),
        };
        using var assembly = new MemoryStream();
        await RawExportAssemblyCodec.WriteAssemblyAsync(header, plaintextItems, assembly, default);
        var assemblyBytes = assembly.ToArray();
        var assemblyDigest = SHA256.HashData(assemblyBytes);
        var spki = recipient.ExportSubjectPublicKeyInfo();
        var request = new C2AssemblyPreparationRequest(
            Guid.Parse("22000000-0000-0000-0000-000000000009"),
            assemblyId,
            SHA256.HashData("assembly-fingerprint"u8),
            SHA256.HashData("manifest"u8),
            assemblyDigest,
            SHA256.HashData("authentication"u8),
            assemblyBytes.Length,
            RecipientId);
        var reservation = new RecipientPackageReserveResult(
            "Reserved", 1, "Reserved", PackageId,
            SHA256.HashData("package-equality"u8),
            "recipient-key", 1, SHA256.HashData(spki), spki, 1,
            DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1),
            SHA256.HashData("provider-token"u8), SHA256.HashData("object-binding"u8), null);
        var encrypted = await new RecipientPackageCryptoService().EncryptAsync(
            request,
            reservation,
            async (destination, token) => await destination.WriteAsync(assemblyBytes, token),
            default);
        await using var spool = encrypted.Spool;
        await using var source = spool.OpenRead();
        using var output = new MemoryStream();
        await source.CopyToAsync(output);
        CryptographicOperations.ZeroMemory(assemblyBytes);
        return output.ToArray();
    }

    private static RawExportAssemblyItemDescriptor Descriptor(int ordinal, string rawClass, byte[] value) => new(
        ordinal,
        rawClass,
        Guid.NewGuid(),
        Guid.NewGuid(),
        1,
        "application/octet-stream",
        value.Length,
        1,
        "commitment-key",
        1,
        SHA256.HashData(value));

    private static RawExportAssemblyPlaintextItem Plaintext(
        RawExportAssemblyItemDescriptor descriptor,
        byte[] value) => new(
            descriptor,
            async (sink, token) => await sink(value, token));

    private sealed class PrivateKeySource(byte[] key) : ITagEkycRecipientPrivateKeySource
    {
        public ValueTask<TagEkycRecipientPrivateKeyLease> AcquireAsync(
            string keyId,
            int keyVersion,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new TagEkycRecipientPrivateKeyLease((byte[])key.Clone()));
    }

    private sealed class FlowHandler(byte[] package) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/ekyc/raw-export/authorizations")
                return Task.FromResult(Json(new
                {
                    decisionId = Guid.NewGuid(), outcome = "Authorized", primaryCause = (string?)null,
                    permitId = PermitId, permitExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
                    authorizedRawClasses = new[] { "ChipDg2Portrait", "LiveSelfieImage" },
                }));
            if (path == "/api/ekyc/raw-export/jobs")
                return Task.FromResult(Json(new { jobId = JobId, bindStatus = "NewJob" }, HttpStatusCode.Created));
            if (path == $"/api/ekyc/raw-export/jobs/{JobId:D}")
                return Task.FromResult(Json(new
                {
                    jobId = JobId, verificationSessionId = SessionId,
                    recipientClientApplicationId = RecipientId, state = "PackageSealed", revision = 4,
                    jobExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
                    rawClasses = new[] { "ChipDg2Portrait", "LiveSelfieImage" },
                    packageId = PackageId, packageState = "Finalized",
                    packageFinalizedAtUtc = DateTimeOffset.UtcNow,
                }));
            if (path == $"/api/ekyc/raw-export/packages/{PackageId:D}/deliveries")
                return Task.FromResult(Json(new
                {
                    deliveryId = DeliveryId, packageId = PackageId, state = "Authorized",
                    authorizedAtUtc = DateTimeOffset.UtcNow,
                    authorizationExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
                    streamAttemptCount = 0, serverStreamCompletedAtUtc = (DateTimeOffset?)null,
                    encryptedPackageLength = package.Length,
                    packageCiphertextDigest = Convert.ToHexString(SHA256.HashData(package)).ToLowerInvariant(),
                    deliveryReceiptDigest = (string?)null,
                }, HttpStatusCode.Created));
            if (path == $"/api/ekyc/raw-export/deliveries/{DeliveryId:D}/content")
            {
                var content = new ByteArrayContent(package);
                content.Headers.ContentLength = package.Length;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage Json(object value, HttpStatusCode status = HttpStatusCode.OK) => new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json"),
        };
    }
}
