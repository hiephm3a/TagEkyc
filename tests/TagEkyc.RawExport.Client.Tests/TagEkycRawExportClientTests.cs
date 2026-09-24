namespace TagEkyc.RawExport.Client.Tests;

using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public sealed class TagEkycRawExportClientTests
{
    private static readonly Guid SessionId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid RecipientId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid PermitId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid JobId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid PackageId = Guid.Parse("10000000-0000-0000-0000-000000000005");
    private static readonly Guid DeliveryId = Guid.Parse("10000000-0000-0000-0000-000000000006");
    private static readonly byte[] Portrait = [1, 2, 3, 4];
    private static readonly byte[] Selfie = [5, 6, 7, 8, 9];

    [Fact]
    public async Task AcquireAsync_UsesPublicControlPlaneAndReturnsZeroizingRawLease()
    {
        using var rsa = RSA.Create(2048);
        var package = PackageProducer.Create(rsa, Portrait, Selfie);
        var handler = new FlowHandler(package);
        var client = Client(handler, rsa.ExportPkcs8PrivateKey());

        using var result = await client.AcquireAsync(new(SessionId, Guid.NewGuid()));

        Assert.Equal(Portrait, result.ChipDg2Portrait.ToArray());
        Assert.Equal(Selfie, result.LiveSelfieImage.ToArray());
        Assert.Equal(SessionId, result.VerificationSessionId);
        Assert.Equal(JobId, result.JobId);
        Assert.Equal(PackageId, result.PackageId);
        Assert.Equal([
            "/api/ekyc/raw-export/authorizations",
            "/api/ekyc/raw-export/jobs",
            $"/api/ekyc/raw-export/jobs/{JobId:D}",
            $"/api/ekyc/raw-export/packages/{PackageId:D}/deliveries",
            $"/api/ekyc/raw-export/deliveries/{DeliveryId:D}/content",
        ], handler.Paths);
        Assert.Equal(3, handler.IdempotencyKeys.Count);
        Assert.All(handler.ApiKeys, value => Assert.Equal("api-secret", value));

        result.Dispose();
        Assert.Throws<ObjectDisposedException>(() => result.ChipDg2Portrait.ToArray());
    }

    [Fact]
    public async Task AcquireAsync_RejectsCiphertextTamperingEvenWhenDeliveryDigestMatchesTamperedBytes()
    {
        using var rsa = RSA.Create(2048);
        var package = PackageProducer.Create(rsa, Portrait, Selfie);
        package[^20] ^= 0x40;
        var client = Client(new FlowHandler(package), rsa.ExportPkcs8PrivateKey());

        var exception = await Assert.ThrowsAsync<TagEkycRawExportClientException>(
            () => client.AcquireAsync(new(SessionId, Guid.NewGuid())));

        Assert.Equal("RAW_EXPORT_PACKAGE_INVALID", exception.Code);
    }

    [Theory]
    [InlineData("SomeOtherRawClass")]
    [InlineData("LiveSelfieImage")]
    public async Task AcquireAsync_RejectsExtraOrDuplicateAssemblyClass(string thirdClass)
    {
        using var rsa = RSA.Create(2048);
        var package = PackageProducer.Create(rsa, Portrait, Selfie, thirdClass);
        var client = Client(new FlowHandler(package), rsa.ExportPkcs8PrivateKey());

        var exception = await Assert.ThrowsAsync<TagEkycRawExportClientException>(
            () => client.AcquireAsync(new(SessionId, Guid.NewGuid())));

        Assert.Equal("RAW_EXPORT_PACKAGE_INVALID", exception.Code);
    }

    private static TagEkycRawExportClient Client(HttpMessageHandler handler, byte[] privateKey) => new(
        new HttpClient(handler),
        new TagEkycRawExportClientOptions(
            new Uri("https://tag.example.test"),
            "X-API-Key",
            "api-secret",
            Guid.Parse("10000000-0000-0000-0000-000000000007"),
            3,
            RecipientId,
            "recipient-key",
            2,
            TimeSpan.Zero,
            2),
        new FixedPrivateKeySource(privateKey));

    private sealed class FixedPrivateKeySource(byte[] key) : ITagEkycRecipientPrivateKeySource
    {
        public ValueTask<TagEkycRecipientPrivateKeyLease> AcquireAsync(
            string keyId,
            int keyVersion,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal("recipient-key", keyId);
            Assert.Equal(2, keyVersion);
            return ValueTask.FromResult(new TagEkycRecipientPrivateKeyLease((byte[])key.Clone()));
        }
    }

    private sealed class FlowHandler(byte[] package) : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];
        public List<string> IdempotencyKeys { get; } = [];
        public List<string> ApiKeys { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            Paths.Add(path);
            if (request.Headers.TryGetValues("Idempotency-Key", out var idempotency))
                IdempotencyKeys.Add(idempotency.Single());
            ApiKeys.Add(request.Headers.GetValues("X-API-Key").Single());
            if (path == "/api/ekyc/raw-export/authorizations")
            {
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                Assert.Contains("ChipDg2Portrait", body, StringComparison.Ordinal);
                Assert.Contains("LiveSelfieImage", body, StringComparison.Ordinal);
                return Json(new
                {
                    decisionId = Guid.NewGuid(), outcome = "Authorized", primaryCause = (string?)null,
                    permitId = PermitId, permitExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
                    authorizedRawClasses = new[] { "ChipDg2Portrait", "LiveSelfieImage" },
                });
            }
            if (path == "/api/ekyc/raw-export/jobs")
                return Json(new { jobId = JobId, bindStatus = "NewJob" }, HttpStatusCode.Created);
            if (path == $"/api/ekyc/raw-export/jobs/{JobId:D}")
                return Json(new
                {
                    jobId = JobId, verificationSessionId = SessionId,
                    recipientClientApplicationId = RecipientId, state = "PackageSealed", revision = 4,
                    jobExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
                    rawClasses = new[] { "ChipDg2Portrait", "LiveSelfieImage" },
                    packageId = PackageId, packageState = "Finalized",
                    packageFinalizedAtUtc = DateTimeOffset.UtcNow,
                });
            if (path == $"/api/ekyc/raw-export/packages/{PackageId:D}/deliveries")
                return Json(new
                {
                    deliveryId = DeliveryId, packageId = PackageId, state = "Authorized",
                    authorizedAtUtc = DateTimeOffset.UtcNow,
                    authorizationExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
                    streamAttemptCount = 0, serverStreamCompletedAtUtc = (DateTimeOffset?)null,
                    encryptedPackageLength = package.Length,
                    packageCiphertextDigest = Convert.ToBase64String(SHA256.HashData(package))
                        .TrimEnd('=').Replace('+', '-').Replace('/', '_'),
                    deliveryReceiptDigest = (string?)null,
                }, HttpStatusCode.Created);
            if (path == $"/api/ekyc/raw-export/deliveries/{DeliveryId:D}/content")
            {
                var content = new ByteArrayContent(package);
                content.Headers.ContentLength = package.Length;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(object value, HttpStatusCode status = HttpStatusCode.OK) => new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json"),
        };
    }

    private static class PackageProducer
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        internal static byte[] Create(
            RSA recipient,
            byte[] portrait,
            byte[] selfie,
            string? thirdClass = null)
        {
            var assembly = Assembly(portrait, selfie, thirdClass);
            var cek = RandomNumberGenerator.GetBytes(32);
            var noncePrefix = RandomNumberGenerator.GetBytes(8);
            try
            {
                var wrapped = recipient.Encrypt(cek, RSAEncryptionPadding.OaepSHA256);
                var header = JsonSerializer.SerializeToUtf8Bytes(new
                {
                    contentEncryptionAlgorithm = "A256GCM-FRAME-V1",
                    keyWrapAlgorithm = "RSA-OAEP-256",
                    signatureAlgorithm = "none",
                    packageProfile = "tip-88c1-c2-package-profile-v1",
                    packageId = PackageId,
                    recipientClientApplicationId = RecipientId,
                    recipientKeyId = "recipient-key",
                    recipientKeyVersion = 2,
                    completeAssemblyLength = assembly.Length,
                    noncePrefix = Base64Url(noncePrefix),
                    wrappedCek = Base64Url(wrapped),
                }, JsonOptions);
                using var result = new MemoryStream();
                result.Write("TIP-88C1-C2-PACKAGE-V1"u8);
                WriteUInt32(result, checked((uint)header.Length));
                result.Write(header);
                var envelope = result.ToArray();
                var digest = SHA256.HashData(envelope);
                using var aes = new AesGcm(cek, 16);
                var ciphertext = new byte[assembly.Length];
                var tag = new byte[16];
                var nonce = Nonce(noncePrefix, 0);
                var aad = Aad("tip-88c1-c2-frame-aad-v1",
                    Convert.ToHexString(digest).ToLowerInvariant(), "0",
                    assembly.Length.ToString(CultureInfo.InvariantCulture), "data");
                aes.Encrypt(nonce, assembly, ciphertext, tag, aad);
                WriteUInt32(result, 0);
                WriteUInt32(result, checked((uint)assembly.Length));
                result.Write(ciphertext);
                result.Write(tag);
                var completionTag = new byte[16];
                var completionNonce = Nonce(noncePrefix, uint.MaxValue);
                var completionAad = Aad("tip-88c1-c2-completion-aad-v1",
                    Convert.ToHexString(digest).ToLowerInvariant(), "1",
                    assembly.Length.ToString(CultureInfo.InvariantCulture));
                aes.Encrypt(completionNonce, ReadOnlySpan<byte>.Empty, Span<byte>.Empty, completionTag, completionAad);
                WriteUInt32(result, uint.MaxValue);
                WriteUInt64(result, checked((ulong)assembly.Length));
                WriteUInt32(result, 1);
                result.Write(completionTag);
                return result.ToArray();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(assembly);
                CryptographicOperations.ZeroMemory(cek);
                CryptographicOperations.ZeroMemory(noncePrefix);
            }
        }

        private static byte[] Assembly(byte[] portrait, byte[] selfie, string? thirdClass)
        {
            var items = new List<object>
            {
                new { ordinal = 0, rawClass = "ChipDg2Portrait", plaintextLength = portrait.Length },
                new { ordinal = 1, rawClass = "LiveSelfieImage", plaintextLength = selfie.Length },
            };
            if (thirdClass is not null)
                items.Add(new { ordinal = 2, rawClass = thirdClass, plaintextLength = 1 });
            var header = JsonSerializer.SerializeToUtf8Bytes(new
            {
                jobId = JobId,
                verificationSessionId = SessionId,
                recipientClientApplicationId = RecipientId,
                items,
            }, JsonOptions);
            using var result = new MemoryStream();
            result.Write("TIP-88C1-ASSEMBLY-V1"u8);
            WriteUInt32(result, checked((uint)header.Length));
            result.Write(header);
            WriteItem(result, 0, portrait);
            WriteItem(result, 1, selfie);
            if (thirdClass is not null) WriteItem(result, 2, [0x5a]);
            return result.ToArray();
        }

        private static void WriteItem(Stream stream, uint ordinal, byte[] value)
        {
            WriteUInt32(stream, ordinal);
            WriteUInt64(stream, checked((ulong)value.Length));
            stream.Write(value);
        }

        private static byte[] Aad(string domain, params string[] fields)
        {
            using var stream = new MemoryStream();
            WriteField(stream, domain);
            foreach (var field in fields) WriteField(stream, field);
            return stream.ToArray();
        }

        private static void WriteField(Stream stream, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value.Normalize(NormalizationForm.FormC));
            WriteUInt32(stream, checked((uint)bytes.Length));
            stream.Write(bytes);
        }

        private static byte[] Nonce(byte[] prefix, uint ordinal)
        {
            var value = new byte[12];
            prefix.CopyTo(value, 0);
            BinaryPrimitives.WriteUInt32BigEndian(value.AsSpan(8), ordinal);
            return value;
        }

        private static string Base64Url(byte[] value) =>
            Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static void WriteUInt32(Stream stream, uint value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            stream.Write(bytes);
        }

        private static void WriteUInt64(Stream stream, ulong value)
        {
            Span<byte> bytes = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
            stream.Write(bytes);
        }
    }
}
