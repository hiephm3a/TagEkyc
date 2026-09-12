using System.Security.Cryptography;
using System.Buffers.Binary;
using System.Globalization;
using System.Text.Json;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.TrustedAdapter;

namespace TagEkyc.Application.CaptureRuntime;

public sealed class CaptureRuntimeExecutionApplicationService(
    ICaptureRuntimeExecutionGateway gateway, ICaptureRuntimeVerifierPepperSource peppers,
    ICaptureRuntimeAppendGateway append)
    : ICaptureRuntimeExecutionService
{
    public async Task<SessionOperationResult<CaptureCapabilityResponse>> IssueOrReplaceCapabilityAsync(
        AuthenticatedClientContext actor, Guid verificationSessionId, CaptureCapabilityRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (verificationSessionId == Guid.Empty || idempotencyKey == Guid.Empty || exactRequestBody.IsEmpty ||
            !(request.Action == "Issue" && request.CurrentCapabilityId is null && request.ExpectedRevision is null ||
              request.Action == "Replace" && request.CurrentCapabilityId is { } id && id != Guid.Empty &&
              request.ExpectedRevision > 0))
            return Invalid<CaptureCapabilityResponse>();
        if (actor.ClientApplicationId == Guid.Empty) return Denied<CaptureCapabilityResponse>();
        var secret = RandomNumberGenerator.GetBytes(32);
        byte[]? digest = null;
        try
        {
            var version = peppers.CurrentVersion;
            using var lease = await peppers.TryResolveAsync(version,
                CaptureRuntimeVerifierPepperDomain.CapabilityDigest, cancellationToken);
            if (version <= 0 || lease is null || lease.Version != version ||
                lease.Domain != CaptureRuntimeVerifierPepperDomain.CapabilityDigest || lease.Key.Length != 32)
                return NotReady<CaptureCapabilityResponse>();
            digest = CaptureRuntimeVerifierCryptography.ComputeDigest(lease.Key.Span,
                CaptureRuntimeVerifierPepperDomain.CapabilityDigest, secret);
            var encoded = new char[CaptureRuntimeVerifierCryptography.CanonicalTextLength];
            CaptureRuntimeVerifierCryptography.EncodeCanonicalSecret(secret, encoded);
            var prefix = new string(encoded, 0, 12);
            Array.Clear(encoded);
            var fingerprint = CaptureRuntimeHttpFingerprint.Compute(
                request.Action == "Issue" ? "R20a" : "R20b",
                $"/api/ekyc/verification-sessions/{verificationSessionId:N}/capture-capabilities",
                exactRequestBody, idempotencyKey, actor.ClientApplicationId.ToByteArray(bigEndian: true));
            var result = await gateway.IssueOrReplaceCapabilityAsync(new(
                actor.ClientApplicationId, verificationSessionId, request, idempotencyKey,
                Guid.NewGuid(), prefix, digest, version, fingerprint, DateTimeOffset.UtcNow), cancellationToken);
            if (result.ResultCode == "CREATED" && result.SecretAvailable &&
                result.CapabilityId is { } capability && capability != Guid.Empty &&
                result.ExpiresAtUtc is { } expiry && result.State == "ActiveUnbound" && result.Revision > 0)
                return SessionOperationResult<CaptureCapabilityResponse>.Success(new(
                    capability, CaptureRuntimeVerifierCryptography.EncodeCanonicalSecret(secret),
                    expiry, result.State, result.Revision.Value));
            return MapFailure<CaptureCapabilityResponse>(result.ResultCode);
        }
        catch (OperationCanceledException) { throw; }
        catch (CryptographicException) { return NotReady<CaptureCapabilityResponse>(); }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
            if (digest is not null) CryptographicOperations.ZeroMemory(digest);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeBindingResponse>> BindAsync(
        AuthenticatedCaptureRuntimeContext actor, CaptureRuntimeBindRequest request,
        Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody,
        CancellationToken cancellationToken = default)
    {
        if (request.CaptureCapabilityId == Guid.Empty || request.BindOperationId == Guid.Empty ||
            idempotencyKey != request.BindOperationId || exactRequestBody.IsEmpty)
            return Invalid<CaptureRuntimeBindingResponse>();
        var secret = new byte[32];
        byte[]? digest = null;
        try
        {
            if (!CaptureRuntimeVerifierCryptography.TryDecodeCanonicalSecret(request.CaptureCapabilitySecret, secret))
                return Invalid<CaptureRuntimeBindingResponse>();
            var resolution = await gateway.ResolveCapabilityVerifierAsync(request.CaptureCapabilityId, cancellationToken);
            if (!resolution.IsSuccess)
                return SessionOperationResult<CaptureRuntimeBindingResponse>.Failure(
                    resolution.Error!.Code, resolution.Error.Message, resolution.Error.StatusCode);
            var verifier = resolution.Value!;
            using var lease = await peppers.TryResolveAsync(verifier.PepperVersion,
                CaptureRuntimeVerifierPepperDomain.CapabilityDigest, cancellationToken);
            if (lease is null || lease.Version != verifier.PepperVersion ||
                lease.Domain != CaptureRuntimeVerifierPepperDomain.CapabilityDigest || lease.Key.Length != 32)
                return NotReady<CaptureRuntimeBindingResponse>();
            digest = CaptureRuntimeVerifierCryptography.ComputeDigest(lease.Key.Span,
                CaptureRuntimeVerifierPepperDomain.CapabilityDigest, secret);
            var verified = CaptureRuntimeVerifierCryptography.FixedTimeEquals(verifier.Digest, digest);
            if (!verified) return Denied<CaptureRuntimeBindingResponse>();
            var partition = new byte[56];
            actor.CaptureAgentId.TryWriteBytes(partition.AsSpan(0, 16), bigEndian: true, out _);
            actor.DeviceInstallationId.TryWriteBytes(partition.AsSpan(16, 16), bigEndian: true, out _);
            actor.CredentialId.TryWriteBytes(partition.AsSpan(32, 16), bigEndian: true, out _);
            BinaryPrimitives.WriteInt64BigEndian(partition.AsSpan(48), actor.CredentialGeneration);
            var fingerprint = CaptureRuntimeHttpFingerprint.Compute("R21",
                "/api/ekyc/capture-runtime/executions/bind", exactRequestBody, idempotencyKey,
                partition, secret);
            // The existing bind function materializes expiry and commits its exact denied result in B.
            return await gateway.BindCapabilityAsync(new(actor, request.CaptureCapabilityId, true,
                request.BindOperationId, fingerprint, DateTimeOffset.UtcNow), cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (CryptographicException) { return NotReady<CaptureRuntimeBindingResponse>(); }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
            if (digest is not null) CryptographicOperations.ZeroMemory(digest);
        }
    }

    public Task<SessionOperationResult<CaptureRuntimeBindingResponse>> ReconcileAsync(
        AuthenticatedCaptureRuntimeContext actor, CaptureRuntimeReconcileRequest request,
        CancellationToken cancellationToken = default) =>
        request.CaptureCapabilityId == Guid.Empty || request.BindOperationId == Guid.Empty
            ? Task.FromResult(Invalid<CaptureRuntimeBindingResponse>())
            : gateway.ReconcileBindingAsync(actor, request, cancellationToken);

    public Task<SessionOperationResult<CaptureRuntimeConfigurationResponse>> ResolveConfigurationAsync(
        AuthenticatedCaptureRuntimeContext actor, CancellationToken cancellationToken = default) =>
        gateway.ResolveConfigurationAsync(actor, cancellationToken);

    public static (byte[] Utf8Document, string ETag) SerializeConfiguration(CaptureRuntimeConfigurationResponse value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("CaptureAgentId", value.CaptureAgentId.ToString("N"));
            writer.WriteString("ConfigurationId", value.ConfigurationId.ToString("N"));
            writer.WriteNumber("ConfigurationRevision", value.ConfigurationRevision);
            writer.WriteString("EffectiveAtUtc", value.EffectiveAtUtc.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
            writer.WriteString("ExpiresAtUtc", value.ExpiresAtUtc.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
            writer.WriteBoolean("RawExportEnabled", value.RawExportEnabled);
            writer.WriteNumber("PlaintextBudgetSeconds", value.PlaintextBudgetSeconds);
            writer.WriteNumber("RawExportSourceClaimSafetyMarginMilliseconds", value.RawExportSourceClaimSafetyMarginMilliseconds);
            writer.WriteNumber("CaptureAgentConfigurationPollingIntervalSeconds", value.CaptureAgentConfigurationPollingIntervalSeconds);
            writer.WriteNumber("RawExportSourceMaximumChipDg2PortraitBytes", value.RawExportSourceMaximumChipDg2PortraitBytes);
            writer.WriteNumber("RawExportSourceMaximumLiveSelfieImageBytes", value.RawExportSourceMaximumLiveSelfieImageBytes);
            writer.WriteNumber("RawExportCaptureMaximumAggregatePlaintextBytesPerHost", value.RawExportCaptureMaximumAggregatePlaintextBytesPerHost);
            writer.WriteNumber("RawExportCustodyMaximumPlaintextWindowBytesPerStream", value.RawExportCustodyMaximumPlaintextWindowBytesPerStream);
            writer.WriteNumber("RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment", value.RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment);
            writer.WriteNumber("RawExportIngressMaximumPreAdmissionBufferedBytes", value.RawExportIngressMaximumPreAdmissionBufferedBytes);
            writer.WriteEndObject();
        }
        var bytes = stream.ToArray();
        var etag = "\"" + Convert.ToBase64String(SHA256.HashData(bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_') + "\"";
        return (bytes, etag);
    }

    public Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> AppendCaptureArtifactAsync(
        AuthenticatedCaptureRuntimeContext actor, Guid bindingId,
        CaptureRuntimeCaptureArtifactRequest request, Guid idempotencyKey,
        CancellationToken cancellationToken = default) =>
        bindingId == Guid.Empty || bindingId != request.BindingId || idempotencyKey == Guid.Empty
            ? Task.FromResult(Invalid<CaptureArtifactSubmissionResponseDto>())
            : append.AppendCaptureArtifactAsync(actor, bindingId, request, idempotencyKey, cancellationToken);

    public Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> AppendEvidenceResultAsync(
        AuthenticatedCaptureRuntimeContext actor, Guid verificationSessionId,
        CaptureRuntimeEvidenceResultRequest request, Guid idempotencyKey,
        CancellationToken cancellationToken = default) =>
        verificationSessionId == Guid.Empty || request.BindingId == Guid.Empty || idempotencyKey == Guid.Empty
            ? Task.FromResult(Invalid<EvidenceResultSubmissionResponseDto>())
            : append.AppendEvidenceResultAsync(actor, verificationSessionId, request, idempotencyKey, cancellationToken);

    internal static SessionOperationResult<T> MapFailure<T>(string result) => result switch
    {
        "INVALID_INPUT" => Invalid<T>(),
        "ACCESS_DENIED" => Denied<T>(),
        "RESOURCE_NOT_AVAILABLE" => SessionOperationResult<T>.Failure(CaptureRuntimeErrorCodes.ResourceNotAvailable, "Resource not available.", 404),
        "CONFLICT" or "TERMINALIZED_EXPIRED_AND_DENIED" => SessionOperationResult<T>.Failure(CaptureRuntimeErrorCodes.Conflict, "Conflict.", 409),
        "EXISTING_MATCH_SECRET_UNAVAILABLE" => SessionOperationResult<T>.Failure(CaptureRuntimeErrorCodes.ExistingMatchSecretUnavailable, "Secret is unavailable.", 409),
        _ => NotReady<T>()
    };
    private static SessionOperationResult<T> Invalid<T>() => SessionOperationResult<T>.Failure(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400);
    private static SessionOperationResult<T> Denied<T>() => SessionOperationResult<T>.Failure(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403);
    private static SessionOperationResult<T> NotReady<T>() => SessionOperationResult<T>.Failure(CaptureRuntimeErrorCodes.NotReady, "Capture Runtime is not ready.", 503);
}
