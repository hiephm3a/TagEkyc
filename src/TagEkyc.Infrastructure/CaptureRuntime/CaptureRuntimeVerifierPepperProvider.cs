using System.Security.Cryptography;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;

namespace TagEkyc.Infrastructure.CaptureRuntime;

public enum CaptureRuntimeVerifierPepperSecretRefStatus
{
    Success,
    ReferenceInvalid,
    MaterialUnavailable,
    MaterialInvalid,
}

public sealed record CaptureRuntimeVerifierPepperSecretRefResolution(
    CaptureRuntimeVerifierPepperSecretRefStatus Status,
    CaptureRuntimeVerifierPepperMaterialLease? Material);

public sealed class CaptureRuntimeVerifierPepperMaterialLease : IDisposable
{
    private byte[]? _canonicalAscii;

    internal CaptureRuntimeVerifierPepperMaterialLease(byte[] canonicalAscii) =>
        _canonicalAscii = canonicalAscii;

    public ReadOnlyMemory<byte> CanonicalAscii =>
        _canonicalAscii ?? ReadOnlyMemory<byte>.Empty;

    public void Dispose()
    {
        var material = Interlocked.Exchange(ref _canonicalAscii, null);
        if (material is not null)
        {
            CryptographicOperations.ZeroMemory(material);
        }
    }
}

public static class CaptureRuntimeVerifierPepperSecretRefResolver
{
    private const int CanonicalLength = 43;

    public static CaptureRuntimeVerifierPepperSecretRefResolution Resolve(string secretRef)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
        {
            return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.ReferenceInvalid);
        }

        var separator = secretRef.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0)
        {
            return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.ReferenceInvalid);
        }

        var scheme = secretRef[..separator];
        var target = secretRef[(separator + 1)..];
        if (string.Equals(scheme, "env", StringComparison.Ordinal))
        {
            return ResolveEnvironment(target);
        }

        if (string.Equals(scheme, "file", StringComparison.Ordinal))
        {
            return ResolveFile(target);
        }

        return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.ReferenceInvalid);
    }

    private static CaptureRuntimeVerifierPepperSecretRefResolution ResolveEnvironment(string variableName)
    {
        if (string.IsNullOrWhiteSpace(variableName))
        {
            return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.ReferenceInvalid);
        }

        var value = Environment.GetEnvironmentVariable(variableName);
        if (value is null)
        {
            return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.MaterialUnavailable);
        }

        if (value.Length != CanonicalLength)
        {
            return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.MaterialInvalid);
        }

        var bytes = new byte[CanonicalLength];
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character > 0x7f || !IsBase64Url(character))
            {
                CryptographicOperations.ZeroMemory(bytes);
                return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.MaterialInvalid);
            }

            bytes[index] = (byte)character;
        }

        if (!IsCanonicalTail(bytes[^1]))
        {
            CryptographicOperations.ZeroMemory(bytes);
            return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.MaterialInvalid);
        }

        return Success(bytes);
    }

    private static CaptureRuntimeVerifierPepperSecretRefResolution ResolveFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
        {
            return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.ReferenceInvalid);
        }

        if (Directory.Exists(path))
        {
            return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.ReferenceInvalid);
        }

        if (!File.Exists(path))
        {
            return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.MaterialUnavailable);
        }

        var bytes = new byte[CanonicalLength];
        try
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1,
                FileOptions.SequentialScan);

            var offset = 0;
            while (offset < bytes.Length)
            {
                var read = stream.Read(bytes, offset, bytes.Length - offset);
                if (read == 0)
                {
                    CryptographicOperations.ZeroMemory(bytes);
                    return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.MaterialInvalid);
                }

                offset += read;
            }

            if (stream.ReadByte() != -1 || !bytes.All(value => IsBase64Url((char)value)) ||
                !IsCanonicalTail(bytes[^1]))
            {
                CryptographicOperations.ZeroMemory(bytes);
                return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.MaterialInvalid);
            }

            return Success(bytes);
        }
        catch (UnauthorizedAccessException)
        {
            CryptographicOperations.ZeroMemory(bytes);
            return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.MaterialUnavailable);
        }
        catch (IOException)
        {
            CryptographicOperations.ZeroMemory(bytes);
            return Failure(CaptureRuntimeVerifierPepperSecretRefStatus.MaterialUnavailable);
        }
    }

    private static bool IsBase64Url(char value) =>
        value is >= 'A' and <= 'Z'
        or >= 'a' and <= 'z'
        or >= '0' and <= '9'
        or '-' or '_';

    private static bool IsCanonicalTail(byte value) =>
        "AEIMQUYcgkosw048"u8.IndexOf(value) >= 0;

    private static CaptureRuntimeVerifierPepperSecretRefResolution Success(byte[] material) =>
        new(CaptureRuntimeVerifierPepperSecretRefStatus.Success,
            new CaptureRuntimeVerifierPepperMaterialLease(material));

    private static CaptureRuntimeVerifierPepperSecretRefResolution Failure(
        CaptureRuntimeVerifierPepperSecretRefStatus status) => new(status, null);
}

public sealed class CaptureRuntimeVerifierPepperProvider : ICaptureRuntimeVerifierPepperSource
{
    private readonly IReadOnlyDictionary<int, string> _secretRefs;

    public CaptureRuntimeVerifierPepperProvider(CaptureRuntimeVerifierPepperOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.CurrentVersion <= 0 || options.Versions is null || options.Versions.Count == 0)
        {
            throw new ArgumentException("Capture Runtime verifier-pepper configuration is incomplete.", nameof(options));
        }

        var secretRefs = new Dictionary<int, string>();
        foreach (var version in options.Versions)
        {
            if (version is null || version.Version <= 0 || string.IsNullOrWhiteSpace(version.SecretRef) ||
                !IsStructurallyValidSecretRef(version.SecretRef) ||
                !secretRefs.TryAdd(version.Version, version.SecretRef))
            {
                throw new ArgumentException("Capture Runtime verifier-pepper version metadata is invalid.", nameof(options));
            }

            var syntaxResolution = CaptureRuntimeVerifierPepperSecretRefResolver.Resolve(version.SecretRef);
            syntaxResolution.Material?.Dispose();
            if (syntaxResolution.Status == CaptureRuntimeVerifierPepperSecretRefStatus.ReferenceInvalid)
            {
                throw new ArgumentException("Capture Runtime verifier-pepper SecretRef is invalid.", nameof(options));
            }
        }

        if (!secretRefs.ContainsKey(options.CurrentVersion))
        {
            throw new ArgumentException("Capture Runtime verifier-pepper current version is not configured.", nameof(options));
        }

        CurrentVersion = options.CurrentVersion;
        _secretRefs = secretRefs;
    }

    public int CurrentVersion { get; }

    public ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(
        int version,
        CaptureRuntimeVerifierPepperDomain domain,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (version <= 0 || !Enum.IsDefined(domain) || !_secretRefs.TryGetValue(version, out var secretRef))
        {
            return ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(null);
        }

        var resolution = CaptureRuntimeVerifierPepperSecretRefResolver.Resolve(secretRef);
        if (resolution.Status != CaptureRuntimeVerifierPepperSecretRefStatus.Success ||
            resolution.Material is null)
        {
            resolution.Material?.Dispose();
            return ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(null);
        }

        using (resolution.Material)
        {
            Span<byte> master = stackalloc byte[CaptureRuntimeVerifierCryptography.SecretByteLength];
            try
            {
                if (!CaptureRuntimeVerifierCryptography.TryDecodeCanonicalSecret(
                        resolution.Material.CanonicalAscii.Span, master))
                {
                    return ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(null);
                }

                var domainKey = CaptureRuntimeVerifierCryptography.DeriveDomainKey(master, domain);
                return ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(
                    new CaptureRuntimeVerifierPepperLease(version, domain, domainKey));
            }
            finally
            {
                CaptureRuntimeVerifierCryptography.Zero(master);
            }
        }
    }

    private static bool IsStructurallyValidSecretRef(string secretRef)
    {
        if (secretRef.StartsWith("env:", StringComparison.Ordinal))
        {
            return !string.IsNullOrWhiteSpace(secretRef[4..]);
        }

        return secretRef.StartsWith("file:", StringComparison.Ordinal) &&
               Path.IsPathFullyQualified(secretRef[5..]) &&
               !string.IsNullOrWhiteSpace(secretRef[5..]);
    }

    private sealed class CaptureRuntimeVerifierPepperLease(
        int version,
        CaptureRuntimeVerifierPepperDomain domain,
        byte[] key) : ICaptureRuntimeVerifierPepperLease
    {
        private byte[]? _key = key;

        public int Version { get; } = version;
        public CaptureRuntimeVerifierPepperDomain Domain { get; } = domain;
        public ReadOnlyMemory<byte> Key => _key ?? ReadOnlyMemory<byte>.Empty;

        public void Dispose()
        {
            var material = Interlocked.Exchange(ref _key, null);
            if (material is not null)
            {
                CryptographicOperations.ZeroMemory(material);
            }
        }
    }
}
