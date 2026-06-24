using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.Common;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

public sealed class Tip68SoftHsmE2ETests(Tip68SoftHsmFixture softHsm) : IClassFixture<Tip68SoftHsmFixture>
{
    [Fact]
    public async Task Pkcs11_signproof_against_softhsm_produces_reference_verifiable_neutral_proof()
    {
        var request = ProofRequest();
        var signer = softHsm.CreateSigner();

        var envelope = await signer.SignProofAsync(request, CancellationToken.None);
        var view = BuildVerificationView(request, envelope, envelope.SignatureScheme);

        Assert.Equal(EvidenceSignatureDefaults.FormatJws, envelope.SignatureFormat);
        Assert.Equal(EvidenceSignatureDefaults.SchemeJwsEs256V1, envelope.SignatureScheme);
        Assert.Equal(EvidenceSignatureDefaults.AlgorithmEs256, envelope.SignatureAlgorithm);
        Assert.Equal(softHsm.Kid, envelope.KeyId);
        Assert.DoesNotContain("\"d\"", envelope.PublicKeyJwk!, StringComparison.Ordinal);
        Assert.True(Tip67BReferenceVerifier.Verify(
            view,
            softHsm.Kid,
            envelope.PublicKeyFingerprint!,
            request.Challenge));
    }

    [Fact]
    public async Task Pkcs11_and_localdev_share_byte_identical_protected_header_and_payload()
    {
        var request = ProofRequest();
        var pkcs11Signer = softHsm.CreateSigner();
        var pkcs11Envelope = await pkcs11Signer.SignProofAsync(request, CancellationToken.None);
        var pkcs11Parts = pkcs11Envelope.SignatureValue.Split('.');

        using var localDevSigner = new LocalDevEs256JwsEvidenceSigner(new LocalDevEs256JwsEvidenceSignerOptions
        {
            KeyId = softHsm.Kid,
        });
        var payloadJson = Es256JwsEvidenceSignatureBuilder.BuildProofClaimJson(request, pkcs11Envelope.SignedAt);
        var localDevJws = InvokeLocalDevSignPayload(localDevSigner, payloadJson);
        var localDevParts = localDevJws.Split('.');

        Assert.Equal(3, pkcs11Parts.Length);
        Assert.Equal(3, localDevParts.Length);
        Assert.Equal(localDevParts[0], pkcs11Parts[0]);
        Assert.Equal(localDevParts[1], pkcs11Parts[1]);
        Assert.NotEqual(localDevParts[2], pkcs11Parts[2]);
    }

    [Fact]
    public void Pkcs11_validate_token_self_test_passes_against_softhsm()
    {
        softHsm.CreateSigner().ValidateToken(CancellationToken.None);
    }

    [Fact]
    public void Pkcs11_validate_token_fails_closed_when_public_key_does_not_match_private_key()
    {
        var signer = softHsm.CreateMismatchedSigner();

        var exception = Assert.Throws<InvalidOperationException>(() => signer.ValidateToken(CancellationToken.None));

        Assert.Contains("does not match", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pkcs11_concurrent_signproof_smoke_test_opens_token_and_all_jws_verify()
    {
        var request = ProofRequest();
        var signer = softHsm.CreateSigner();
        var envelopes = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => signer.SignProofAsync(request, CancellationToken.None)));

        foreach (var envelope in envelopes)
        {
            Assert.True(Tip67BReferenceVerifier.Verify(
                BuildVerificationView(request, envelope, envelope.SignatureScheme),
                softHsm.Kid,
                envelope.PublicKeyFingerprint!,
                request.Challenge));
        }
    }

    private static string InvokeLocalDevSignPayload(LocalDevEs256JwsEvidenceSigner signer, string payloadJson)
    {
        var signPayload = typeof(LocalDevEs256JwsEvidenceSigner).GetMethod(
            "SignPayload",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LocalDev SignPayload method is missing.");
        return (string)signPayload.Invoke(signer, [payloadJson])!;
    }

    private static EvidencePackageVerificationViewDto BuildVerificationView(
        EvidenceProofSignatureRequest request,
        EvidenceSignatureEnvelope envelope,
        string signatureScheme)
    {
        var payload = JsonDocument.Parse(Base64UrlDecode(envelope.SignatureValue.Split('.')[1])).RootElement;
        return new EvidencePackageVerificationViewDto(
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
            signatureScheme,
            envelope.SignatureAlgorithm,
            envelope.KeyId,
            envelope.PublicKeyJwk!,
            envelope.PublicKeyFingerprint!);
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

public sealed class Tip68SoftHsmFixture : IAsyncLifetime
{
    private static readonly byte[] EcPublicKeyParams = Convert.FromHexString("06082A8648CE3D030107");
    private static readonly byte[] KeyObjectId = Encoding.ASCII.GetBytes("tip68-es256");
    private static readonly byte[] MismatchKeyObjectId = Encoding.ASCII.GetBytes("tip68-mismatch");
    private static readonly byte[] MismatchTempKeyObjectId = Encoding.ASCII.GetBytes("tip68-mismatch-temp");

    private readonly Pkcs11InteropFactories factories = new();
    private string? previousSoftHsmConf;

    public string TokenLabel { get; } = $"tagekyc-tip68-{Guid.NewGuid():N}"[..24];

    public string KeyLabel { get; } = "tagekyc-tip68-es256";

    public string MismatchKeyLabel { get; } = "tagekyc-tip68-mismatch";

    public string MismatchTempKeyLabel { get; } = "tagekyc-tip68-mismatch-temp";

    public string Kid { get; } = "tagekyc-es256-2026-v1";

    public string UserPin { get; } = $"u{Guid.NewGuid():N}"[..16];

    public string SoPin { get; } = $"s{Guid.NewGuid():N}"[..16];

    public string TokenDirectory { get; } = Path.Combine(Path.GetTempPath(), $"tagekyc-tip68-softhsm-{Guid.NewGuid():N}");

    public string ConfigPath => Path.Combine(TokenDirectory, "softhsm2.conf");

    public string ModulePath { get; private set; } = string.Empty;

    public string SoftHsmUtilPath { get; private set; } = string.Empty;

    public string SoftHsmVersion { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        ModulePath = ResolveSoftHsmModulePath();
        SoftHsmUtilPath = ResolveSoftHsmUtilPath();
        AddSoftHsmToProcessPath(ModulePath, SoftHsmUtilPath);
        SoftHsmVersion = await RunProcessAsync(SoftHsmUtilPath, ["--version"], new Dictionary<string, string>());
        await RunProvisioningScriptAsync();

        previousSoftHsmConf = Environment.GetEnvironmentVariable("SOFTHSM2_CONF", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("SOFTHSM2_CONF", ConfigPath, EnvironmentVariableTarget.Process);

        GenerateMatchingKeyPair(KeyLabel, KeyObjectId);
        GenerateMismatchedKeyPair();
    }

    public Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("SOFTHSM2_CONF", previousSoftHsmConf, EnvironmentVariableTarget.Process);
        try
        {
            if (Directory.Exists(TokenDirectory))
            {
                Directory.Delete(TokenDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return Task.CompletedTask;
    }

    public Pkcs11Es256JwsEvidenceSigner CreateSigner() =>
        new(CreateOptions(KeyLabel, KeyObjectId));

    public Pkcs11Es256JwsEvidenceSigner CreateMismatchedSigner() =>
        new(CreateOptions(MismatchKeyLabel, MismatchKeyObjectId));

    private Pkcs11Es256JwsEvidenceSignerOptions CreateOptions(string keyLabel, byte[] keyObjectId) =>
        new()
        {
            LibraryPath = ModulePath,
            TokenLabel = TokenLabel,
            Pin = UserPin,
            KeyLabel = keyLabel,
            KeyObjectId = Convert.ToHexString(keyObjectId).ToLowerInvariant(),
            Kid = Kid,
        };

    private async Task RunProvisioningScriptAsync()
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Tip68SoftHsmProvisioning.ps1");
        if (!File.Exists(scriptPath))
        {
            scriptPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "Tip68SoftHsmProvisioning.ps1"));
        }

        if (!File.Exists(scriptPath))
        {
            throw new InvalidOperationException("TIP-68 SoftHSM provisioning script is missing.");
        }

        var env = new Dictionary<string, string>
        {
            ["TAG_EKYC_SOFTHSM_USER_PIN"] = UserPin,
            ["TAG_EKYC_SOFTHSM_SO_PIN"] = SoPin,
            ["SOFTHSM2_MODULE"] = ModulePath,
            ["PATH"] = BuildSoftHsmPath(ModulePath, SoftHsmUtilPath),
        };
        await RunProcessAsync(
            "powershell",
            [
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                scriptPath,
                "-TokenDirectory",
                TokenDirectory,
                "-TokenLabel",
                TokenLabel,
                "-ModulePath",
                ModulePath,
                "-SoftHsmUtil",
                SoftHsmUtilPath,
                "-KeyLabel",
                KeyLabel,
                "-KeyObjectIdHex",
                Convert.ToHexString(KeyObjectId).ToLowerInvariant(),
                "-Kid",
                Kid,
            ],
            env);
    }

    private void GenerateMatchingKeyPair(string keyLabel, byte[] keyObjectId)
    {
        using var context = OpenSession();
        GenerateKeyPair(context.Session, keyLabel, keyObjectId);
    }

    private void GenerateMismatchedKeyPair()
    {
        using var context = OpenSession();
        GenerateKeyPair(context.Session, MismatchKeyLabel, MismatchKeyObjectId);
        GenerateKeyPair(context.Session, MismatchTempKeyLabel, MismatchTempKeyObjectId);

        var mismatchPublic = FindOneObject(context.Session, PublicKeyTemplate(MismatchKeyLabel, MismatchKeyObjectId));
        var tempPublic = FindOneObject(context.Session, PublicKeyTemplate(MismatchTempKeyLabel, MismatchTempKeyObjectId));
        var tempAttributes = context.Session.GetAttributeValue(tempPublic, [CKA.CKA_EC_PARAMS, CKA.CKA_EC_POINT]);
        var tempParams = tempAttributes.Single(attribute => attribute.Type == (ulong)CKA.CKA_EC_PARAMS).GetValueAsByteArray();
        var tempPoint = tempAttributes.Single(attribute => attribute.Type == (ulong)CKA.CKA_EC_POINT).GetValueAsByteArray();

        context.Session.DestroyObject(mismatchPublic);
        context.Session.CreateObject(
        [
            factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_PUBLIC_KEY),
            factories.ObjectAttributeFactory.Create(CKA.CKA_KEY_TYPE, CKK.CKK_EC),
            factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true),
            factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, MismatchKeyLabel),
            factories.ObjectAttributeFactory.Create(CKA.CKA_ID, MismatchKeyObjectId),
            factories.ObjectAttributeFactory.Create(CKA.CKA_VERIFY, true),
            factories.ObjectAttributeFactory.Create(CKA.CKA_EC_PARAMS, tempParams),
            factories.ObjectAttributeFactory.Create(CKA.CKA_EC_POINT, tempPoint),
        ]);
    }

    private void GenerateKeyPair(ISession session, string keyLabel, byte[] keyObjectId)
    {
        var mechanism = factories.MechanismFactory.Create(CKM.CKM_EC_KEY_PAIR_GEN);
        var publicTemplate = new List<IObjectAttribute>
        {
            factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true),
            factories.ObjectAttributeFactory.Create(CKA.CKA_PRIVATE, false),
            factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, keyLabel),
            factories.ObjectAttributeFactory.Create(CKA.CKA_ID, keyObjectId),
            factories.ObjectAttributeFactory.Create(CKA.CKA_VERIFY, true),
            factories.ObjectAttributeFactory.Create(CKA.CKA_EC_PARAMS, EcPublicKeyParams),
        };
        var privateTemplate = new List<IObjectAttribute>
        {
            factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true),
            factories.ObjectAttributeFactory.Create(CKA.CKA_PRIVATE, true),
            factories.ObjectAttributeFactory.Create(CKA.CKA_SENSITIVE, true),
            factories.ObjectAttributeFactory.Create(CKA.CKA_EXTRACTABLE, false),
            factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, keyLabel),
            factories.ObjectAttributeFactory.Create(CKA.CKA_ID, keyObjectId),
            factories.ObjectAttributeFactory.Create(CKA.CKA_SIGN, true),
        };

        session.GenerateKeyPair(mechanism, publicTemplate, privateTemplate, out _, out _);
    }

    private SigningContext OpenSession()
    {
        var library = factories.Pkcs11LibraryFactory.LoadPkcs11Library(factories, ModulePath, AppType.MultiThreaded);
        var slot = library.GetSlotList(SlotsType.WithTokenPresent)
            .Single(candidate => string.Equals(candidate.GetTokenInfo().Label.Trim(), TokenLabel, StringComparison.Ordinal));
        var session = slot.OpenSession(SessionType.ReadWrite);
        var loggedIn = false;

        try
        {
            session.Login(CKU.CKU_USER, UserPin);
            loggedIn = true;
            return new SigningContext(library, session, loggedIn);
        }
        catch
        {
            if (loggedIn)
            {
                session.Logout();
            }

            session.Dispose();
            library.Dispose();
            throw;
        }
    }

    private List<IObjectAttribute> PublicKeyTemplate(string keyLabel, byte[] keyObjectId) =>
    [
        factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_PUBLIC_KEY),
        factories.ObjectAttributeFactory.Create(CKA.CKA_KEY_TYPE, CKK.CKK_EC),
        factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL, keyLabel),
        factories.ObjectAttributeFactory.Create(CKA.CKA_ID, keyObjectId),
    ];

    private static IObjectHandle FindOneObject(ISession session, List<IObjectAttribute> template)
    {
        var objects = session.FindAllObjects(template);
        return objects.Count switch
        {
            1 => objects[0],
            0 => throw new InvalidOperationException("Expected PKCS#11 object was not found."),
            _ => throw new InvalidOperationException("Expected PKCS#11 object was ambiguous."),
        };
    }

    private static string ResolveSoftHsmModulePath()
    {
        var env = Environment.GetEnvironmentVariable("SOFTHSM2_MODULE");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
        {
            return env;
        }

        var candidates = new[]
        {
            Path.Combine(Path.GetTempPath(), "SoftHSM2-2.5.0-portable", "SoftHSM2", "lib", "softhsm2-x64.dll"),
            @"C:\SoftHSM2\lib\softhsm2-x64.dll",
            @"C:\Program Files\SoftHSM2\lib\softhsm2-x64.dll",
            @"C:\Program Files (x86)\SoftHSM2\lib\softhsm2.dll",
        };

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException("SoftHSM2 PKCS#11 module DLL not found. Install SoftHSM2 for Windows or set SOFTHSM2_MODULE.");
    }

    private static string ResolveSoftHsmUtilPath()
    {
        var env = Environment.GetEnvironmentVariable("SOFTHSM2_UTIL");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
        {
            return env;
        }

        var tempPortable = Path.Combine(Path.GetTempPath(), "SoftHSM2-2.5.0-portable", "SoftHSM2", "bin", "softhsm2-util.exe");
        if (File.Exists(tempPortable))
        {
            return tempPortable;
        }

        return "softhsm2-util";
    }

    private static void AddSoftHsmToProcessPath(string modulePath, string utilPath)
    {
        Environment.SetEnvironmentVariable(
            "PATH",
            BuildSoftHsmPath(modulePath, utilPath),
            EnvironmentVariableTarget.Process);
    }

    private static string BuildSoftHsmPath(string modulePath, string utilPath)
    {
        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathParts = currentPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();
        foreach (var path in new[]
        {
            Path.GetDirectoryName(modulePath),
            Path.GetDirectoryName(utilPath),
        })
        {
            if (!string.IsNullOrWhiteSpace(path) &&
                !pathParts.Any(existing => string.Equals(existing, path, StringComparison.OrdinalIgnoreCase)))
            {
                pathParts.Insert(0, path);
            }
        }

        return string.Join(Path.PathSeparator, pathParts);
    }

    private static async Task<string> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string> environment)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var pair in environment)
        {
            startInfo.Environment[pair.Key] = pair.Value;
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process '{fileName}'.");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Process '{fileName}' failed with exit code {process.ExitCode}.{Environment.NewLine}{stdout}{stderr}");
        }

        return stdout.Trim();
    }

    private sealed class SigningContext(IPkcs11Library library, ISession session, bool loggedIn) : IDisposable
    {
        public ISession Session => session;

        public void Dispose()
        {
            if (loggedIn)
            {
                session.Logout();
            }

            session.Dispose();
            library.Dispose();
        }
    }
}
