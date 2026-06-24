using System.Text;
using System.Text.Json;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Testing;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.Common;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

public sealed class Tip68ProdHsmSigningTests
{
    [Fact]
    public async Task Shared_construction_matches_the_localdev_jws_header_and_payload()
    {
        using var signer = new LocalDevEs256JwsEvidenceSigner(new LocalDevEs256JwsEvidenceSignerOptions
        {
            KeyId = "tagekyc-es256-2026-v1",
        });
        var request = ProofRequest();

        var envelope = await signer.SignProofAsync(request, CancellationToken.None);
        var parts = envelope.SignatureValue.Split('.');

        Assert.Equal(3, parts.Length);
        Assert.Equal(
            Es256JwsEvidenceSignatureBuilder.BuildProtectedHeaderJson("tagekyc-es256-2026-v1"),
            Encoding.UTF8.GetString(Base64UrlDecode(parts[0])));
        Assert.Equal(
            Es256JwsEvidenceSignatureBuilder.BuildProofClaimJson(request, envelope.SignedAt),
            Encoding.UTF8.GetString(Base64UrlDecode(parts[1])));
        Assert.Equal(
            $"{parts[0]}.{parts[1]}",
            Es256JwsEvidenceSignatureBuilder.BuildSigningInput(
                Es256JwsEvidenceSignatureBuilder.BuildProtectedHeaderJson("tagekyc-es256-2026-v1"),
                Es256JwsEvidenceSignatureBuilder.BuildProofClaimJson(request, envelope.SignedAt)));
    }

    [Fact]
    public void Pkcs11_options_and_config_failures_do_not_echo_pin()
    {
        const string secretPin = "tip68-super-secret-pin";
        var options = new Pkcs11Es256JwsEvidenceSignerOptions
        {
            Pin = secretPin,
            KeyLabel = "tagekyc-es256-2026-v1",
            KeyObjectId = "7469703638",
            Kid = "tagekyc-es256-2026-v1",
        };

        Assert.DoesNotContain(secretPin, options.ToString(), StringComparison.Ordinal);

        var exception = Assert.Throws<InvalidOperationException>(() => new Pkcs11Es256JwsEvidenceSigner(options));
        Assert.DoesNotContain(secretPin, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_backend_fails_closed_on_host_startup()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("TagEkyc:EvidenceSigning:Backend", "BogusBackend");
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Invalid evidence signing backend configuration", exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("localdev-es256-v1", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RequireHardwareSigner_fails_closed_on_host_startup_without_pkcs11_backend()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("TagEkyc:EvidenceSigning:RequireHardwareSigner", "true");
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Invalid evidence signing backend configuration", exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("localdev-es256-v1", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pkcs11_backend_missing_config_fails_closed_on_host_startup_without_echoing_pin()
    {
        const string secretPin = "tip68-host-secret-pin";
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("TagEkyc:EvidenceSigning:Backend", EvidenceSigningBackends.Pkcs11);
                builder.UseSetting("TagEkyc:EvidenceSigning:Pkcs11:Pin", secretPin);
                builder.UseSetting("TagEkyc:EvidenceSigning:Pkcs11:KeyLabel", "tagekyc-es256-2026-v1");
                builder.UseSetting("TagEkyc:EvidenceSigning:Pkcs11:KeyObjectId", "7469703638");
                builder.UseSetting("TagEkyc:EvidenceSigning:Pkcs11:Kid", "tagekyc-es256-2026-v1");
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("PKCS#11 signing requires a library path", exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(secretPin, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Pkcs11_kid_and_token_object_id_are_distinct_config_fields()
    {
        var options = new Pkcs11Es256JwsEvidenceSignerOptions
        {
            LibraryPath = "/usr/lib/softhsm/libsofthsm2.so",
            TokenLabel = "tagekyc-tip68",
            Pin = "1234",
            KeyLabel = "token-key-label",
            KeyObjectId = "746f6b656e2d6f626a656374",
            Kid = "tagekyc-es256-2026-v1",
        };

        var rendered = options.ToString();

        Assert.Contains("KeyObjectId = 746f6b656e2d6f626a656374", rendered, StringComparison.Ordinal);
        Assert.Contains("Kid = tagekyc-es256-2026-v1", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("Pin = 1234", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Pkcs11_public_key_point_parser_accepts_raw_uncompressed_points_with_high_bit_x()
    {
        var point = new byte[65];
        point[0] = 0x04;
        point[1] = 0x90;
        for (var i = 2; i < point.Length; i++)
        {
            point[i] = (byte)i;
        }

        var parse = typeof(Pkcs11Es256JwsEvidenceSigner).GetMethod(
            "ParseUncompressedEcPoint",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PKCS#11 EC point parser is missing.");

        var parsed = ((byte[] X, byte[] Y))parse.Invoke(null, [point])!;

        Assert.Equal(point[1..33], parsed.X);
        Assert.Equal(point[33..65], parsed.Y);
    }

    [Fact]
    public void Persistence_mapper_tolerates_future_signature_scheme_on_read()
    {
        var row = new EvidenceManifestRow
        {
            EvidencePackageId = Guid.Parse("11111111-1111-5111-8111-111111111111"),
            SessionGuid = Guid.Parse("22222222-2222-5222-8222-222222222222"),
            VerificationSessionId = "22222222222252228222222222222222",
            PackageVersion = EvidenceCanonicalization.PackageVersion,
            CanonicalizationScheme = EvidenceCanonicalization.CanonicalizationScheme,
            HashAlgorithm = EvidenceCanonicalization.HashAlgorithm,
            ManifestHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            PackageHash = "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            EvidenceRefsJson = "[]",
            AuditEventRefsJson = "[]",
            ResultRef = Guid.Parse("33333333-3333-5333-8333-333333333333"),
            EvidencePackageSignatureStatus = SignaturePlaceholderStatusDto.Signed.ToString(),
            SignatureFormat = EvidenceSignatureDefaults.FormatJws,
            SignatureScheme = "jws-es256-x5c-v1",
            SignatureAlgorithm = EvidenceSignatureDefaults.AlgorithmEs256,
            KeyId = "tagekyc-es256-2026-v2",
            SignedAt = DateTimeOffset.UtcNow,
            SignatureValue = "header.payload.signature",
            PublicKeyJwk = "{\"crv\":\"P-256\",\"kty\":\"EC\",\"x\":\"x\",\"y\":\"y\"}",
            PublicKeyFingerprint = "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var mapper = typeof(EfEvidenceManifestRepository).Assembly
            .GetType("TagEkyc.Infrastructure.Persistence.DomainRowMapper", throwOnError: true)!;
        var toDomain = mapper.GetMethod("ToDomain", [typeof(EvidenceManifestRow)])
            ?? throw new InvalidOperationException("DomainRowMapper.ToDomain(EvidenceManifestRow) is missing.");
        var manifest = toDomain.Invoke(null, [row])
            ?? throw new InvalidOperationException("DomainRowMapper returned null.");

        Assert.Equal(
            "jws-es256-x5c-v1",
            manifest.GetType().GetProperty("SignatureScheme")?.GetValue(manifest));
    }

    [Fact]
    public async Task Reference_verifier_fails_closed_for_future_unknown_signature_scheme()
    {
        using var signer = new LocalDevEs256JwsEvidenceSigner(new LocalDevEs256JwsEvidenceSignerOptions
        {
            KeyId = "tagekyc-es256-2026-v1",
        });
        var request = ProofRequest();
        var envelope = await signer.SignProofAsync(request, CancellationToken.None);
        var payload = JsonDocument.Parse(Base64UrlDecode(envelope.SignatureValue.Split('.')[1])).RootElement;
        var view = new EvidencePackageVerificationViewDto(
            EvidenceSignatureDefaults.ProofVersionNeutralV1,
            request.Purpose,
            request.SessionId,
            request.IdentityRef,
            request.PackageId,
            request.PackageVersion,
            request.CanonicalizationScheme,
            request.HashAlgorithm,
            VerificationResultDto.Passed,
            AssuranceLevelDto.High,
            [RequiredCheckTypeDto.DocumentNfc, RequiredCheckTypeDto.FaceMatch],
            [RequiredCheckTypeDto.DocumentNfc, RequiredCheckTypeDto.FaceMatch],
            [
                new EvidenceProofEngineRefDto("DocumentNfc", "bbbbbbbbbbbb5bbb8bbbbbbbbbbbbbbb", "nfc", "2", "DocumentNfc"),
                new EvidenceProofEngineRefDto("FaceMatch", "aaaaaaaaaaaa5aaa8aaaaaaaaaaaaaaaa", "face", "1", "FaceMatch"),
            ],
            envelope.SignedAt,
            request.Challenge,
            "client-ref-tip68",
            request.SignedManifestHash,
            payload.GetProperty("resultHash").GetString()!,
            EvidenceSignatureDefaults.ResultHashAlgorithmSha256,
            EvidenceSignatureDefaults.ResultHashCanonicalizationSchemeJcsV1,
            envelope.SignatureValue,
            envelope.SignatureFormat,
            "jws-es256-x5c-v1",
            envelope.SignatureAlgorithm,
            envelope.KeyId,
            envelope.PublicKeyJwk!,
            envelope.PublicKeyFingerprint!);

        Assert.False(Tip67BReferenceVerifier.Verify(
            view,
            "tagekyc-es256-2026-v1",
            view.PublicKeyFingerprint,
            request.Challenge));
    }

    private static EvidenceProofSignatureRequest ProofRequest() =>
        new(
            "11111111111151118111111111111111",
            EvidenceCanonicalization.PackageVersion,
            EvidenceCanonicalization.CanonicalizationScheme,
            EvidenceCanonicalization.HashAlgorithm,
            EvidenceSignatureDefaults.PurposeNeutralEkycProof,
            "22222222222252228222222222222222",
            "sha256:3333333333333333333333333333333333333333333333333333333333333333",
            "Passed",
            "High",
            ["DocumentNfc", "FaceMatch"],
            ["FaceMatch", "DocumentNfc"],
            [
                new EvidenceProofEngineRef("FaceMatch", "aaaaaaaaaaaa5aaa8aaaaaaaaaaaaaaaa", "face", "1", "FaceMatch"),
                new EvidenceProofEngineRef("DocumentNfc", "bbbbbbbbbbbb5bbb8bbbbbbbbbbbbbbb", "nfc", "2", "DocumentNfc"),
            ],
            "opaque challenge: tip68",
            "sha256:4444444444444444444444444444444444444444444444444444444444444444");

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
