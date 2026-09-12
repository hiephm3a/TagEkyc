using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using TagEkyc.Api;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1C6BA1ArchitectureTests
{
    [Fact]
    public void A1_28_CaptureRuntimeVerifier_HasNoClientApiKeyPepperDependency()
    {
        var root = FindRepositoryRoot();
        var files = Directory.GetFiles(Path.Combine(root, "src/TagEkyc.Application/CaptureRuntime"), "*.cs")
            .Concat(Directory.GetFiles(Path.Combine(root, "src/TagEkyc.Infrastructure/CaptureRuntime"), "*.cs"))
            .Concat(new[] { Path.Combine(root, "src/TagEkyc.Infrastructure/Auth/PlatformOperatorCredentialAuthenticator.cs"),
                Path.Combine(root, "src/TagEkyc.Infrastructure/Auth/CaptureRuntimeRequestAuthenticator.cs") }).ToArray();
        Assert.NotEmpty(files);
        static bool Forbidden(string source) => System.Text.RegularExpressions.Regex.IsMatch(source,
            @"\b(ApiKeyHasher|ApiKeyStoreOptions|ApiKeyProvisioningService|ManagedApiKeyParser)\b|TagEkyc:ApiKeyStore");
        foreach (var file in files)
            Assert.False(Forbidden(File.ReadAllText(file)), $"Client API-key pepper dependency in {file}");
        // Mutation controls prove the guard itself detects the two likely ways
        // to accidentally reuse the existing Client credential pepper authority.
        Assert.True(Forbidden("ApiKeyHasher.Hash(clientPepper, presentedKey)"));
        Assert.True(Forbidden("configuration[\"TagEkyc:ApiKeyStore:PepperSecretRef\"]"));
        Assert.False(Forbidden("ICaptureRuntimeVerifierPepperSource CaptureRuntimeVerifierCryptography"));
    }

    private const string WrittenMetadataDigest = "70d40b8925e302727a705dd0c5d2069164056d64857f0cc5b229a47c0c6c6c2d";
    private const string IngressBinding = "ingress=IngressMetadataSha256=" + WrittenMetadataDigest;
    private static readonly (string Name, string Value, string Changed)[] MetadataVector =
    [
        ("X-TagEkyc-Agent-Configuration-Revision", "7", "8"),
        ("X-TagEkyc-Verification-Session-Id", "11111111111141118111111111111111", "11111111111141118111111111111112"),
        ("X-TagEkyc-Capture-Artifact-Id", "22222222222242228222222222222222", "22222222222242228222222222222223"),
        ("X-TagEkyc-Capture-Revision", "3", "4"),
        ("X-TagEkyc-Raw-Class", "ChipDg2Portrait", "LiveSelfieImage"),
        ("Idempotency-Key", "33333333333343338333333333333333", "33333333333343338333333333333334"),
        ("X-TagEkyc-Captured-At-Utc", "2026-09-10T01:02:00.0000000+00:00", "2026-09-10T01:02:01.0000000+00:00"),
        ("X-TagEkyc-Retention-Started-At-Utc", "2026-09-10T01:02:00.0000000+00:00", "2026-09-10T01:02:01.0000000+00:00"),
        ("X-TagEkyc-Retention-Expires-At-Utc", "2026-09-10T01:03:00.0000000+00:00", "2026-09-10T01:03:01.0000000+00:00"),
        ("X-TagEkyc-Retention-Budget-Seconds", "60", "59")
    ];

    [Fact]
    public void Architecture_ExcludesAgentBrokerDeploymentAndSdkMutation()
    {
        var infrastructure = typeof(TagEkycDbContext).Assembly;
        var captureRuntimeTypes = infrastructure.GetTypes()
            .Where(type => type.FullName?.Contains("CaptureRuntime", StringComparison.Ordinal) is true)
            .ToArray();

        Assert.NotEmpty(captureRuntimeTypes);
        Assert.All(captureRuntimeTypes, type =>
            Assert.StartsWith("TagEkyc.Infrastructure.", type.Namespace, StringComparison.Ordinal));
        Assert.DoesNotContain(infrastructure.GetReferencedAssemblies(), reference =>
            reference.Name is "TagEkyc.Api" or "TagEkyc.CaptureAgent");
    }

    [Fact]
    public void Foundation_migration_and_model_are_registered_as_the_latest_EF_boundary()
    {
        var migration = typeof(TagEkycDbContext).Assembly.GetType(
            "TagEkyc.Infrastructure.Persistence.Migrations.Tip88C1C6BA1Foundation",
            throwOnError: true)!;
        Assert.NotNull(migration.GetCustomAttribute<Microsoft.EntityFrameworkCore.Infrastructure.DbContextAttribute>());

        var dbSetNames = typeof(TagEkycDbContext).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.PropertyType.IsGenericType &&
                property.PropertyType.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.DbSet<>))
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("CaptureRuntimeRegistrations", dbSetNames);
        Assert.Contains("CaptureRuntimeCredentialGenerations", dbSetNames);
        Assert.Contains("CaptureRuntimeRequestNonces", dbSetNames);
        Assert.Contains("CaptureRuntimeCutoverStates", dbSetNames);
    }

    [Fact]
    public void Crt1_GoldenVectorsMatchClientAndServer()
    {
        var body = new CountingStream();
        var request = CreateRequest(body);

        Assert.True(CaptureRuntimeCrt1RequestParser.TryCreate(
            request, "RawIngress", "image/jpeg", 17,
            new string('a', 64), IngressBinding,
            out var parsed));

        var expected = string.Join('\n',
            "TAG-EKYC-CRT1",
            "POST",
            "/api/ekyc/raw-export/source-ingress",
            "00112233445546778899aabbccddeeff",
            "7",
            "2026-09-10T01:02:03.4567890Z",
            "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8",
            "image/jpeg",
            "17",
            new string('a', 64),
            IngressBinding) + "\n";

        var baselinePreimage = parsed!.ExactSignedPreimage.ToArray();
        Assert.Equal(Encoding.UTF8.GetBytes(expected), baselinePreimage);
        Assert.Equal(0, body.ReadCount);

        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicVerifierSpki = signer.ExportSubjectPublicKeyInfo();
        var expectedThumbprint = SHA256.HashData(publicVerifierSpki);
        var signature = signer.SignData(
            baselinePreimage,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        Assert.True(InvokeProductionCrt1Verify(
            null, publicVerifierSpki, expectedThumbprint, baselinePreimage, signature));

        // Preserve the same eight signed atoms. Route-rejected mutations also
        // exercise the production cryptographic boundary on exact baseline bytes.
        var mutations = new (int Line, string Changed)[]
        {
            (1, "PUT"), (2, "/api/ekyc/raw-export/source-ingresT"),
            (5, "2026-09-10T01:02:03.4567891Z"),
            (6, "AQECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8"),
            (7, "application/cbor"), (8, "18"), (9, new string('b', 64)),
            (10, "ingress=IngressMetadataSha256=" + new string('b', 64))
        };
        Assert.Equal(8, mutations.Length);
        foreach (var mutation in mutations)
        {
            var lines = expected.Split('\n');
            lines[mutation.Line] = mutation.Changed;
            var changedPreimage = Encoding.UTF8.GetBytes(string.Join('\n', lines));
            Assert.NotEqual(baselinePreimage, changedPreimage);
            Assert.False(InvokeProductionCrt1Verify(
                null, publicVerifierSpki, expectedThumbprint, changedPreimage, signature));
            var changedBody = new CountingStream();
            var changed = CreateRequest(changedBody);
            changed.Method = lines[1];
            changed.Path = lines[2];
            changed.Headers[CaptureRuntimeCrt1RequestParser.TimestampHeader] = lines[5];
            changed.Headers[CaptureRuntimeCrt1RequestParser.NonceHeader] = lines[6];
            changed.ContentType = lines[7];
            changed.ContentLength = long.Parse(lines[8], System.Globalization.CultureInfo.InvariantCulture);
            var accepted = CaptureRuntimeCrt1RequestParser.TryCreate(changed, "RawIngress", lines[7],
                changed.ContentLength.Value, lines[9], lines[10], out var changedParsed);
            if (mutation.Line is 2 or 10) Assert.False(accepted);
            else
            {
                Assert.True(accepted);
                Assert.Equal(changedPreimage, changedParsed!.ExactSignedPreimage.ToArray());
                Assert.False(InvokeProductionCrt1Verify(null, publicVerifierSpki, expectedThumbprint,
                    changedParsed.ExactSignedPreimage.Span, signature));
            }
            Assert.Equal(0, changedBody.ReadCount);
        }

        Assert.Equal(10, MetadataVector.Length);
        Assert.True(CaptureRuntimeCrt1RequestParser.TryComputeIngressMetadataDigest(request, out var baselineDigest));
        Assert.Equal(WrittenMetadataDigest, baselineDigest);
        foreach (var field in MetadataVector)
        {
            var changedBody = new CountingStream();
            var changed = CreateRequest(changedBody);
            changed.Headers[field.Name] = field.Changed;
            Assert.True(CaptureRuntimeCrt1RequestParser.TryComputeIngressMetadataDigest(changed, out var changedDigest));
            Assert.NotEqual(WrittenMetadataDigest, changedDigest);
            Assert.True(CaptureRuntimeCrt1RequestParser.TryCreate(changed, "RawIngress", "image/jpeg", 17,
                new string('a', 64), "ingress=IngressMetadataSha256=" + changedDigest, out var changedParsed));
            Assert.NotEqual(baselinePreimage, changedParsed!.ExactSignedPreimage.ToArray());
            Assert.False(InvokeProductionCrt1Verify(null, publicVerifierSpki, expectedThumbprint,
                changedParsed.ExactSignedPreimage.Span, signature));
            Assert.Equal(0, changedBody.ReadCount);
        }

        var writtenMetadataPreimage = string.Concat(MetadataVector.Select(field => $"{field.Name}={field.Value}\n"));
        Assert.Equal(521, Encoding.UTF8.GetByteCount(writtenMetadataPreimage));
        Assert.Equal(WrittenMetadataDigest, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(writtenMetadataPreimage))).ToLowerInvariant());
        var reordered = MetadataVector.ToArray();
        (reordered[0], reordered[1]) = (reordered[1], reordered[0]);
        var wrongOrderDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Concat(reordered.Select(field => $"{field.Name}={field.Value}\n"))))).ToLowerInvariant();
        Assert.NotEqual(WrittenMetadataDigest, wrongOrderDigest);
        Assert.False(CaptureRuntimeCrt1RequestParser.TryCreate(request, "RawIngress", "image/jpeg", 17,
            new string('a', 64), "ingress=IngressMetadataSha256=" + wrongOrderDigest, out _));
        Assert.False(InvokeProductionCrt1Verify(null, publicVerifierSpki, expectedThumbprint,
            Encoding.UTF8.GetBytes(expected.Replace(WrittenMetadataDigest, wrongOrderDigest, StringComparison.Ordinal)), signature));
        foreach (var field in MetadataVector) request.Headers.Remove(field.Name);
        foreach (var field in reordered) request.Headers[field.Name] = field.Value;
        Assert.True(CaptureRuntimeCrt1RequestParser.TryComputeIngressMetadataDigest(request, out var reorderedArrivalDigest));
        Assert.Equal(WrittenMetadataDigest, reorderedArrivalDigest);

        var missingFinalLf = baselinePreimage[..^1];
        var crlfFinal = baselinePreimage[..^1].Concat(new byte[] { (byte)'\r', (byte)'\n' }).ToArray();
        var extraFinalLf = baselinePreimage.Concat(new byte[] { (byte)'\n' }).ToArray();
        Assert.False(InvokeProductionCrt1Verify(
            null, publicVerifierSpki, expectedThumbprint, missingFinalLf, signature));
        Assert.False(InvokeProductionCrt1Verify(
            null, publicVerifierSpki, expectedThumbprint, crlfFinal, signature));
        Assert.False(InvokeProductionCrt1Verify(
            null, publicVerifierSpki, expectedThumbprint, extraFinalLf, signature));

        request.Method = "DELETE";
        request.Path = "/changed";
        request.Headers[CaptureRuntimeCrt1RequestParser.TimestampHeader] = "2026-09-10T01:02:03.4567891Z";
        request.Headers[CaptureRuntimeCrt1RequestParser.NonceHeader] = "AQECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
        request.ContentType = "application/cbor";
        request.ContentLength = 18;
        parsed.Nonce[0] ^= 0xff;
        parsed.Signature[0] ^= 0xff;
        Assert.Equal(baselinePreimage, parsed.ExactSignedPreimage.ToArray());

        var secondBody = new CountingStream();
        Assert.True(CaptureRuntimeCrt1RequestParser.TryCreate(
            CreateRequest(secondBody), "RawIngress", "image/jpeg", 17,
            new string('a', 64), IngressBinding,
            out var independentlyParsed));
        Assert.Equal(0, secondBody.ReadCount);
        Assert.True(MemoryMarshal.TryGetArray(parsed.ExactSignedPreimage, out ArraySegment<byte> firstStorage));
        Assert.True(MemoryMarshal.TryGetArray(independentlyParsed!.ExactSignedPreimage, out ArraySegment<byte> secondStorage));
        Assert.NotSame(firstStorage.Array, secondStorage.Array);
    }

    [Theory]
    [InlineData("2026-09-10T01:02:03Z", "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8")]
    [InlineData("2026-09-10T01:02:03.4567890+00:00", "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8")]
    [InlineData("2026-09-10T01:02:03.4567890Z", "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh9")]
    [InlineData("2026-09-10T01:02:03.4567890Z", "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh-")]
    [InlineData("2026-09-10T01:02:03.4567890Z", "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh_")]
    public void Crt1_NoncanonicalTimestampOrNonceLexemeIsRejected(string timestamp, string nonce)
    {
        var request = CreateRequest(new CountingStream());
        request.Headers[CaptureRuntimeCrt1RequestParser.TimestampHeader] = timestamp;
        request.Headers[CaptureRuntimeCrt1RequestParser.NonceHeader] = nonce;

        Assert.False(CaptureRuntimeCrt1RequestParser.TryCreate(
            request, "RawIngress", "image/jpeg", 17,
            new string('a', 64), IngressBinding, out _));
    }

    [Fact]
    public void Crt1_IngressMetadataRejectsIncompleteAmbiguousOrForeignBindingsBeforeConsumption()
    {
        Assert.Equal(10, MetadataVector.Length);
        foreach (var field in MetadataVector)
        {
            foreach (var invalidKind in new[] { "missing", "empty", "duplicate" })
            {
                var body = new CountingStream();
                var request = CreateRequest(body);
                if (invalidKind == "missing") request.Headers.Remove(field.Name);
                if (invalidKind == "empty") request.Headers[field.Name] = string.Empty;
                if (invalidKind == "duplicate") request.Headers[field.Name] = new Microsoft.Extensions.Primitives.StringValues([field.Value, field.Value]);
                Assert.False(CaptureRuntimeCrt1RequestParser.TryComputeIngressMetadataDigest(request, out var digest));
                Assert.True(string.IsNullOrEmpty(digest));
                Assert.False(CaptureRuntimeCrt1RequestParser.TryCreate(request, "RawIngress", "image/jpeg", 17,
                    new string('a', 64), IngressBinding, out _));
                Assert.Equal(0, body.ReadCount);
            }
        }
        var extraBody = new CountingStream();
        var extra = CreateRequest(extraBody);
        extra.Headers["X-TagEkyc-Unratified-Metadata"] = "extra";
        Assert.False(CaptureRuntimeCrt1RequestParser.TryComputeIngressMetadataDigest(extra, out var extraDigest));
        Assert.True(string.IsNullOrEmpty(extraDigest));
        Assert.Equal(0, extraBody.ReadCount);
        var cleanBody = new CountingStream();
        var clean = CreateRequest(cleanBody);
        Assert.False(CaptureRuntimeCrt1RequestParser.TryCreate(clean, "RawIngress", "image/jpeg", 17,
            new string('a', 64), string.Empty, out _));
        clean.Path = "/api/ekyc/capture-runtime/executions/reconcile";
        Assert.False(CaptureRuntimeCrt1RequestParser.TryCreate(clean, "Reconcile", "image/jpeg", 17,
            new string('a', 64), IngressBinding, out _));
        Assert.Equal(0, cleanBody.ReadCount);
    }

    [Fact]
    public void Crt1_PreimageHasOneApiBuilderAndAuthenticatorConsumesExactBytes()
    {
        var root = FindRepositoryRoot();
        var builders = Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("TAG-EKYC-CRT1", StringComparison.Ordinal) &&
                       source.Contains("Encoding.UTF8.GetBytes", StringComparison.Ordinal);
            })
            .ToArray();
        Assert.Single(builders);
        Assert.EndsWith("CaptureRuntimeEndpoints.cs", builders[0], StringComparison.Ordinal);

        var authenticator = File.ReadAllText(Path.Combine(root, "src", "TagEkyc.Infrastructure", "Auth",
            "CaptureRuntimeRequestAuthenticator.cs"));
        Assert.Contains("request.ExactSignedPreimage.Span", authenticator, StringComparison.Ordinal);
        Assert.DoesNotContain("TAG-EKYC-CRT1", authenticator, StringComparison.Ordinal);
    }

    private static HttpRequest CreateRequest(Stream body)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/ekyc/raw-export/source-ingress";
        context.Request.ContentType = "image/jpeg";
        context.Request.ContentLength = 17;
        context.Request.Body = body;
        context.Request.Headers[CaptureRuntimeCrt1RequestParser.CredentialIdHeader] = "00112233445546778899aabbccddeeff";
        context.Request.Headers[CaptureRuntimeCrt1RequestParser.CredentialGenerationHeader] = "7";
        context.Request.Headers[CaptureRuntimeCrt1RequestParser.TimestampHeader] = "2026-09-10T01:02:03.4567890Z";
        context.Request.Headers[CaptureRuntimeCrt1RequestParser.NonceHeader] = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
        context.Request.Headers[CaptureRuntimeCrt1RequestParser.SignatureHeader] =
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        foreach (var field in MetadataVector) context.Request.Headers[field.Name] = field.Value;
        context.Request.Headers["X-TagEkyc-Plaintext-Sha256"] = new string('a', 64);
        return context.Request;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException();
    }

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Verify")]
    private static extern bool InvokeProductionCrt1Verify(
        CaptureRuntimeRequestAuthenticator? owner,
        byte[] publicVerifierSpki,
        byte[] expectedThumbprint,
        ReadOnlySpan<byte> exactPreimage,
        byte[] signature);

    private sealed class CountingStream : MemoryStream
    {
        public int ReadCount { get; private set; }
        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCount++;
            return base.Read(buffer, offset, count);
        }
        public override int Read(Span<byte> buffer)
        {
            ReadCount++;
            return base.Read(buffer);
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return base.ReadAsync(buffer, cancellationToken);
        }
    }
}
