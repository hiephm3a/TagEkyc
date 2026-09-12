using System.Security.Cryptography;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.UnitTests;

public sealed class Tip88C1C6BA1ContractTests
{
    [Fact]
    public void RootRequestFingerprint_R01Provision_BindsIntentNotGeneratedSecret()
    {
        var operationId = Guid.ParseExact("01000000000040008000000000000001", "N");
        var principalId = Guid.ParseExact("01000000000040008000000000000002", "N");
        var expiry = new DateTimeOffset(2026, 9, 10, 6, 0, 0, TimeSpan.Zero);

        var fingerprint = CaptureRuntimeVerifierCryptography
            .ComputePlatformProvisionRequestFingerprint(operationId, principalId, expiry);

        Assert.Equal(32, fingerprint.Length);
        Assert.Equal("bf365a4bcbfbad1ae637f396887d20c3382dd996260dfd3a7a1b3a55d80429e7", Hex(fingerprint));
        Assert.NotEqual(fingerprint, CaptureRuntimeVerifierCryptography
            .ComputePlatformProvisionRequestFingerprint(Guid.NewGuid(), principalId, expiry));
        Assert.NotEqual(fingerprint, CaptureRuntimeVerifierCryptography
            .ComputePlatformProvisionRequestFingerprint(operationId, Guid.NewGuid(), expiry));
        Assert.NotEqual(fingerprint, CaptureRuntimeVerifierCryptography
            .ComputePlatformProvisionRequestFingerprint(operationId, principalId, expiry.AddTicks(1)));

        var sameInstantWithOffset = expiry.ToOffset(TimeSpan.FromHours(7));
        Assert.Equal(fingerprint, CaptureRuntimeVerifierCryptography
            .ComputePlatformProvisionRequestFingerprint(operationId, principalId, sameInstantWithOffset));

        var parameters = typeof(CaptureRuntimeVerifierCryptography)
            .GetMethod(nameof(CaptureRuntimeVerifierCryptography.ComputePlatformProvisionRequestFingerprint))!
            .GetParameters();
        Assert.Equal(new[] { "operationId", "principalId", "expiresAtUtc" },
            parameters.Select(parameter => parameter.Name));
    }

    [Fact]
    public void RootRequestFingerprint_R02Revoke_BindsTargetRevisionAndDomain()
    {
        var operationId = Guid.ParseExact("02000000000040008000000000000001", "N");
        var credentialId = Guid.ParseExact("02000000000040008000000000000002", "N");

        var fingerprint = CaptureRuntimeVerifierCryptography
            .ComputePlatformRevokeRequestFingerprint(operationId, credentialId, 3);

        Assert.Equal(32, fingerprint.Length);
        Assert.Equal("7793ea47f3170f101bee163eb3e5bf1da5dbdf4133e6f57f5e37cdf58e986d07", Hex(fingerprint));
        Assert.NotEqual(fingerprint, CaptureRuntimeVerifierCryptography
            .ComputePlatformRevokeRequestFingerprint(Guid.NewGuid(), credentialId, 3));
        Assert.NotEqual(fingerprint, CaptureRuntimeVerifierCryptography
            .ComputePlatformRevokeRequestFingerprint(operationId, Guid.NewGuid(), 3));
        Assert.NotEqual(fingerprint, CaptureRuntimeVerifierCryptography
            .ComputePlatformRevokeRequestFingerprint(operationId, credentialId, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CaptureRuntimeVerifierCryptography.ComputePlatformRevokeRequestFingerprint(
                operationId, credentialId, 0));

        var parameters = typeof(CaptureRuntimeVerifierCryptography)
            .GetMethod(nameof(CaptureRuntimeVerifierCryptography.ComputePlatformRevokeRequestFingerprint))!
            .GetParameters();
        Assert.Equal(new[] { "operationId", "credentialId", "expectedRevision" },
            parameters.Select(parameter => parameter.Name));

        Assert.NotEqual(fingerprint, CaptureRuntimeVerifierCryptography
            .ComputePlatformProvisionRequestFingerprint(operationId, credentialId,
                new DateTimeOffset(2026, 9, 10, 6, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void AuthenticationEnvelope_MixedSchemesDenyWithoutFallback()
    {
        Assert.DoesNotContain(typeof(AuthenticatedPlatformOperatorContext).GetProperties(),
            property => property.Name is "ClientApplicationId" or "ApiKeyId");
        Assert.DoesNotContain(typeof(AuthenticatedCaptureRuntimeContext).GetProperties(),
            property => property.Name is "ClientApplicationId" or "ApiKeyId" or "PrincipalId");
        Assert.DoesNotContain(typeof(CaptureRuntimeSignedRequest).GetProperties(),
            property => property.Name.Contains("ApiKey", StringComparison.Ordinal)
                || property.Name.Contains("PlatformOperator", StringComparison.Ordinal));
    }

    [Fact]
    public void VerifierSecretCodec_CanonicalTailBitsAndGoldenVectors()
    {
        const string masterText = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
        Span<byte> master = stackalloc byte[32];
        Assert.True(CaptureRuntimeVerifierCryptography.TryDecodeCanonicalSecret(masterText, master));

        var platform = CaptureRuntimeVerifierCryptography.DeriveDomainKey(
            master, CaptureRuntimeVerifierPepperDomain.PlatformCredential);
        var bootstrap = CaptureRuntimeVerifierCryptography.DeriveDomainKey(
            master, CaptureRuntimeVerifierPepperDomain.BootstrapDigest);
        var capability = CaptureRuntimeVerifierCryptography.DeriveDomainKey(
            master, CaptureRuntimeVerifierPepperDomain.CapabilityDigest);

        try
        {
            Assert.Equal("4db8994a75704c012def93ba380e2e6e63191f4901bedac22e8ef9876d6f00bb", Hex(platform));
            Assert.Equal("f091805ec2529b08b4661530f80befd7462824ca19d901324188de655f1a58d3", Hex(bootstrap));
            Assert.Equal("eea3404eea1c83d037a2e459bed78d13ba21862f97220f86bb229ed2ed1bc95f", Hex(capability));

            AssertDigest(platform, CaptureRuntimeVerifierPepperDomain.PlatformCredential,
                "ICEiIyQlJicoKSorLC0uLzAxMjM0NTY3ODk6Ozw9Pj8",
                "c5a2f56f1325e67b4deb97fa5d5d5ed21a238d0e3c122a5fc30f16f91a72b473");
            AssertDigest(bootstrap, CaptureRuntimeVerifierPepperDomain.BootstrapDigest,
                "QEFCQ0RFRkdISUpLTE1OT1BRUlNUVVZXWFlaW1xdXl8",
                "ac1d78c9278e787d899465772567e26ee48967fad8b4c05350937a1c0805dec9");
            AssertDigest(capability, CaptureRuntimeVerifierPepperDomain.CapabilityDigest,
                "YGFiY2RlZmdoaWprbG1ub3BxcnN0dXZ3eHl6e3x9fn8",
                "e249239c0a2e4532c78c7d006d576f9819293264107dc91c4f1a8b04493315a3");

            Span<byte> rejected = stackalloc byte[32];
            foreach (var alias in new[] { masterText[..^1] + "9", masterText[..^1] + "-", masterText[..^1] + "_" })
            {
                rejected.Clear();
                Assert.False(CaptureRuntimeVerifierCryptography.TryDecodeCanonicalSecret(alias, rejected));
            }

            Assert.False(CaptureRuntimeVerifierCryptography.TryDecodeCanonicalSecret(masterText + "\n", stackalloc byte[32]));
            Assert.False(CaptureRuntimeVerifierCryptography.TryDecodeCanonicalSecret(masterText + "=", stackalloc byte[32]));
        }
        finally
        {
            CaptureRuntimeVerifierCryptography.Zero(master);
            CryptographicOperations.ZeroMemory(platform);
            CryptographicOperations.ZeroMemory(bootstrap);
            CryptographicOperations.ZeroMemory(capability);
        }
    }

    [Fact]
    public void RuntimeCaptureContract_ObjectGraphContainsNoCallerIdentitySelectors() =>
        AssertNoCallerIdentitySelectors(typeof(CaptureRuntimeCaptureArtifactRequest));

    [Fact]
    public void RuntimeEvidenceContract_ObjectGraphContainsNoCallerIdentitySelectors() =>
        AssertNoCallerIdentitySelectors(typeof(CaptureRuntimeEvidenceResultRequest));

    private static void AssertDigest(byte[] key, CaptureRuntimeVerifierPepperDomain domain,
        string canonicalSecret, string expectedHex)
    {
        Span<byte> secret = stackalloc byte[32];
        Assert.True(CaptureRuntimeVerifierCryptography.TryDecodeCanonicalSecret(canonicalSecret, secret));
        var digest = CaptureRuntimeVerifierCryptography.ComputeDigest(key, domain, secret);
        try
        {
            Assert.Equal(expectedHex, Hex(digest));
        }
        finally
        {
            CaptureRuntimeVerifierCryptography.Zero(secret);
            CryptographicOperations.ZeroMemory(digest);
        }
    }

    private static string Hex(ReadOnlySpan<byte> value) => Convert.ToHexString(value).ToLowerInvariant();

    private static void AssertNoCallerIdentitySelectors(Type root)
    {
        var forbidden = new HashSet<string>(StringComparer.Ordinal)
        {
            "ClientApplicationId", "ApiKeyId", "CaptureAgentId", "DeviceId",
            "DeviceInstallationId", "CredentialId", "CredentialGeneration"
        };
        var visited = new HashSet<Type>();
        var pending = new Stack<Type>();
        pending.Push(root);
        while (pending.TryPop(out var type))
        {
            if (!visited.Add(type) || type == typeof(string) || type.IsPrimitive || type.IsEnum)
            {
                continue;
            }

            foreach (var property in type.GetProperties())
            {
                Assert.DoesNotContain(property.Name, forbidden);
                var child = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (child.IsArray)
                {
                    child = child.GetElementType()!;
                }
                else if (child.IsGenericType)
                {
                    foreach (var argument in child.GetGenericArguments())
                    {
                        pending.Push(argument);
                    }
                    continue;
                }
                pending.Push(child);
            }
        }
    }
}
