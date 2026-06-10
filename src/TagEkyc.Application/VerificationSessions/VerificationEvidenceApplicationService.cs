using TagEkyc.Application.Ports;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Domain;

namespace TagEkyc.Application.VerificationSessions;

public sealed class VerificationEvidenceApplicationService(
    IVerificationSessionRepository sessions,
    ICaptureArtifactRepository captureArtifacts,
    IEvidenceResultRepository evidenceResults,
    IAuditEventRepository auditEvents,
    ILocalDevClientPolicyProvider policies)
    : ICaptureArtifactCommands, ITrustedEvidenceResultCommands
{
    private const string CaptureScope = "capture.artifact.append";
    private const string EvidenceScope = "trusted.evidence.append";

    public async Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> AppendCaptureArtifactAsync(
        AuthenticatedClientContext caller,
        string verificationSessionId,
        CaptureArtifactSubmissionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var callerError = ValidateCaller<CaptureArtifactSubmissionResponseDto>(
            caller,
            AuthenticatedCallerCategory.CaptureAgent,
            CaptureScope);
        if (callerError is not null)
        {
            return callerError;
        }

        var context = await LoadWritableSessionAsync<CaptureArtifactSubmissionResponseDto>(
            caller,
            verificationSessionId,
            cancellationToken);
        if (!context.IsSuccess)
        {
            return SessionOperationResult<CaptureArtifactSubmissionResponseDto>.Failure(
                context.Error!.Code,
                context.Error.Message,
                context.Error.StatusCode);
        }

        var session = context.Value!.Session;
        var policy = context.Value.Policy;
        var captureAgentId = string.IsNullOrWhiteSpace(request.CaptureAgentId)
            ? caller.KeyPrefix
            : request.CaptureAgentId;

        if (!string.Equals(captureAgentId, caller.KeyPrefix, StringComparison.Ordinal) &&
            caller.AllowedCaptureAgentIds?.Contains(captureAgentId) != true)
        {
            return Forbidden<CaptureArtifactSubmissionResponseDto>(
                "CAPTURE_AGENT_MISMATCH",
                "CaptureAgentId does not match the authenticated capture agent policy.");
        }

        var artifactType = ToDomain(request.ArtifactType);
        var artifactPolicyError = ValidateArtifactPolicy<CaptureArtifactSubmissionResponseDto>(
            session,
            policy,
            artifactType);
        if (artifactPolicyError is not null)
        {
            return artifactPolicyError;
        }

        var hashError = ValidateCaptureHashes<CaptureArtifactSubmissionResponseDto>(
            artifactType,
            request.ArtifactHash,
            request.MetadataHash,
            out var artifactHash,
            out var metadataHash);
        if (hashError is not null)
        {
            return hashError;
        }

        var now = DateTimeOffset.UtcNow;
        var artifact = new CaptureArtifact(
            Guid.NewGuid(),
            session.Id,
            artifactType,
            ToDomain(request.CaptureSource),
            captureAgentId,
            request.DeviceId,
            VaultRef: null,
            artifactHash,
            metadataHash,
            CaptureArtifactQualityState.Pending,
            RetryReasonCode: null,
            request.RequestId ?? session.RequestId,
            request.CorrelationId ?? session.CorrelationId,
            now,
            ExpiresAt: null);

        var finalState = session.State;
        await captureArtifacts.AppendAsync(artifact, cancellationToken);
        if (session.State == VerificationSessionState.Created)
        {
            finalState = VerificationSessionState.InProgress;
            await sessions.SetStateAsync(session.Id, finalState, cancellationToken);
            await auditEvents.AppendAsync(CreateAuditEvent(caller, session, "SESSION_STATE_CHANGED"), cancellationToken);
        }

        await auditEvents.AppendAsync(CreateAuditEvent(caller, session, "CAPTURE_ARTIFACT_RECORDED"), cancellationToken);

        return SessionOperationResult<CaptureArtifactSubmissionResponseDto>.Success(new CaptureArtifactSubmissionResponseDto(
            FormatId(artifact.Id),
            FormatId(session.Id),
            artifact.ArtifactHash?.ToString(),
            Accepted: true,
            finalState.ToString(),
            artifact.CorrelationId));
    }

    public async Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> AppendEvidenceResultAsync(
        AuthenticatedClientContext caller,
        string verificationSessionId,
        EvidenceResultSubmissionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var callerError = ValidateCaller<EvidenceResultSubmissionResponseDto>(
            caller,
            AuthenticatedCallerCategory.TrustedAdapter,
            EvidenceScope);
        if (callerError is not null)
        {
            return callerError;
        }

        var context = await LoadWritableSessionAsync<EvidenceResultSubmissionResponseDto>(
            caller,
            verificationSessionId,
            cancellationToken);
        if (!context.IsSuccess)
        {
            return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                context.Error!.Code,
                context.Error.Message,
                context.Error.StatusCode);
        }

        var session = context.Value!.Session;
        var policy = context.Value.Policy;
        var resultType = ToDomain(request.ResultType);
        if (resultType == EvidenceResultType.FraudRisk)
        {
            return Forbidden<EvidenceResultSubmissionResponseDto>(
                "FRAUD_RISK_DEFERRED",
                "FraudRisk evidence runtime recording is deferred in TIP-05.");
        }

        var evidenceCheck = MapEvidenceToCheck(resultType);
        if (!session.RequiredChecks.Contains(evidenceCheck) &&
            policy.AllowedOptionalEvidenceChecks?.Contains(evidenceCheck) != true)
        {
            return Forbidden<EvidenceResultSubmissionResponseDto>(
                "CHECK_NOT_ALLOWED",
                "Evidence result type does not map to a required or policy-allowed check.");
        }

        if (request.InputCaptureArtifactIds is null || request.InputCaptureArtifactIds.Count == 0)
        {
            return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                "INPUT_CAPTURE_ARTIFACTS_REQUIRED",
                "InputCaptureArtifactIds is required for this evidence result type.",
                400);
        }

        var sessionArtifacts = await captureArtifacts.ListBySessionAsync(session.Id, cancellationToken);
        var inputArtifacts = new List<CaptureArtifact>();
        foreach (var inputId in request.InputCaptureArtifactIds)
        {
            if (!Guid.TryParse(inputId, out var parsedId))
            {
                return NotFound<EvidenceResultSubmissionResponseDto>(
                    "CAPTURE_ARTIFACT_NOT_FOUND",
                    "Input capture artifact was not found for this session.");
            }

            var artifact = sessionArtifacts.SingleOrDefault(candidate => candidate.Id == parsedId);
            if (artifact is null)
            {
                return NotFound<EvidenceResultSubmissionResponseDto>(
                    "CAPTURE_ARTIFACT_NOT_FOUND",
                    "Input capture artifact was not found for this session.");
            }

            inputArtifacts.Add(artifact);
        }

        if (inputArtifacts.Any(artifact => !IsCompatibleInput(request.ResultType, artifact.ArtifactType, policy)))
        {
            return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                "INVALID_EVIDENCE_RESULT",
                "Input capture artifact type is not compatible with the evidence result type.",
                400);
        }

        if (request.Result is VerificationResultDto.NotAvailable or VerificationResultDto.NotSupported)
        {
            return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                "INVALID_RESULT_STATUS",
                "Evidence result status is not accepted in TIP-05.",
                400);
        }

        if (request.Confidence is < 0 or > 1)
        {
            return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                "INVALID_CONFIDENCE",
                "Confidence must be between 0.0 and 1.0.",
                400);
        }

        if (string.IsNullOrWhiteSpace(request.EngineName) || string.IsNullOrWhiteSpace(request.EngineVersion))
        {
            return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                "INVALID_EVIDENCE_RESULT",
                "EngineName and EngineVersion are required.",
                400);
        }

        if (!TryCreateHashRef(request.PayloadHash, out var payloadHash))
        {
            return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                "INVALID_HASH_REF",
                "PayloadHash is required and must use the sha256: prefix.",
                400);
        }

        if (!IsSanitizedSummaryRefAllowed(request.SanitizedSummaryRef))
        {
            return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                "INVALID_EVIDENCE_RESULT",
                "SanitizedSummaryRef must not contain vault refs, raw paths, sensitive URLs, raw refs, or plaintext identity payloads.",
                400);
        }

        var now = DateTimeOffset.UtcNow;
        var evidence = new EvidenceResult(
            Guid.NewGuid(),
            session.Id,
            VerificationCheckId: null,
            resultType,
            inputArtifacts.Select(artifact => artifact.Id).ToArray(),
            ToDomain(request.Result),
            request.Confidence,
            request.ReasonCodes ?? [],
            request.RetryReasonCode,
            request.SanitizedSummaryRef,
            payloadHash,
            ToDomain(request.PayloadSignatureStatus),
            request.EngineName,
            request.EngineVersion,
            request.RequestId ?? session.RequestId,
            request.CorrelationId ?? session.CorrelationId,
            now);

        var priorEvidence = await evidenceResults.ListBySessionAsync(session.Id, cancellationToken);
        var finalState = session.State;
        var stateEvents = new List<string>();
        await evidenceResults.AppendAsync(evidence, cancellationToken);

        if (session.State == VerificationSessionState.Created)
        {
            finalState = VerificationSessionState.InProgress;
            await sessions.SetStateAsync(session.Id, finalState, cancellationToken);
            stateEvents.Add("SESSION_STATE_CHANGED");
        }

        if (IsReadyToComplete(session, priorEvidence.Concat([evidence])) &&
            finalState == VerificationSessionState.InProgress)
        {
            finalState = VerificationSessionState.ReadyToComplete;
            await sessions.SetStateAsync(session.Id, finalState, cancellationToken);
            stateEvents.Add("SESSION_STATE_CHANGED");
        }

        foreach (var stateEvent in stateEvents)
        {
            await auditEvents.AppendAsync(CreateAuditEvent(caller, session, stateEvent), cancellationToken);
        }

        await auditEvents.AppendAsync(CreateAuditEvent(caller, session, "EVIDENCE_RESULT_RECORDED"), cancellationToken);

        return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Success(new EvidenceResultSubmissionResponseDto(
            FormatId(evidence.Id),
            Accepted: true,
            finalState.ToString(),
            NextAction: request.Result == VerificationResultDto.RetryRequired ? "RETRY_CAPTURE" : null));
    }

    private async Task<SessionOperationResult<WritableSessionContext<T>>> LoadWritableSessionAsync<T>(
        AuthenticatedClientContext caller,
        string verificationSessionId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(verificationSessionId, out var sessionId))
        {
            return SessionOperationResult<WritableSessionContext<T>>.Failure(
                "SESSION_NOT_FOUND",
                "Verification session was not found.",
                404);
        }

        var session = await sessions.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return SessionOperationResult<WritableSessionContext<T>>.Failure(
                "SESSION_NOT_FOUND",
                "Verification session was not found.",
                404);
        }

        if (!CanWriteSession(caller, session.ClientApplicationId))
        {
            await auditEvents.AppendAsync(CreateAuditEvent(caller, session, "SESSION_ACCESS_DENIED"), cancellationToken);
            return SessionOperationResult<WritableSessionContext<T>>.Failure(
                "SESSION_ACCESS_DENIED",
                "Caller is not authorized to write this session.",
                403);
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return SessionOperationResult<WritableSessionContext<T>>.Failure(
                "SESSION_EXPIRED",
                "Verification session is expired.",
                403);
        }

        if (session.State == VerificationSessionState.ReadyToComplete)
        {
            return SessionOperationResult<WritableSessionContext<T>>.Failure(
                "SESSION_READY_TO_COMPLETE",
                "Verification session is ready to complete and no longer accepts TIP-05 writes.",
                409);
        }

        if (session.State is VerificationSessionState.Completed or
            VerificationSessionState.Cancelled or
            VerificationSessionState.Expired or
            VerificationSessionState.TechnicalTerminal)
        {
            return SessionOperationResult<WritableSessionContext<T>>.Failure(
                "SESSION_TERMINAL",
                "Verification session is terminal.",
                409);
        }

        var policy = await policies.GetPolicyAsync(session.ClientApplicationId, cancellationToken);
        if (policy is null)
        {
            return SessionOperationResult<WritableSessionContext<T>>.Failure(
                "SESSION_ACCESS_DENIED",
                "Client policy was not found.",
                403);
        }

        return SessionOperationResult<WritableSessionContext<T>>.Success(new WritableSessionContext<T>(session, policy));
    }

    private static SessionOperationResult<T>? ValidateCaller<T>(
        AuthenticatedClientContext caller,
        AuthenticatedCallerCategory expectedCategory,
        string requiredScope)
    {
        if (caller.CallerCategory != expectedCategory)
        {
            return Forbidden<T>(
                "CALLER_CATEGORY_NOT_ALLOWED",
                "The API key caller category is not allowed for this endpoint.");
        }

        if (!caller.Scopes.Contains(requiredScope))
        {
            return Forbidden<T>(
                "MISSING_SCOPE",
                "The API key is not scoped for this endpoint.");
        }

        return null;
    }

    private static bool CanWriteSession(AuthenticatedClientContext caller, Guid sessionClientApplicationId) =>
        caller.ClientApplicationId == sessionClientApplicationId ||
        caller.AllowedClientApplicationIds?.Contains(sessionClientApplicationId) == true;

    private static SessionOperationResult<T>? ValidateArtifactPolicy<T>(
        VerificationSession session,
        LocalDevClientPolicy policy,
        CaptureArtifactType artifactType)
    {
        var optionalChecks = policy.AllowedOptionalEvidenceChecks ?? new HashSet<RequiredCheckType>();
        var supportingTypes = policy.AllowedSupportingArtifactTypes ?? new HashSet<CaptureArtifactType>();
        var allowed = artifactType switch
        {
            CaptureArtifactType.NfcReadArtifact =>
                session.RequiredChecks.Contains(RequiredCheckType.DocumentNfc) ||
                optionalChecks.Contains(RequiredCheckType.DocumentNfc),
            CaptureArtifactType.SelfieImage =>
                session.RequiredChecks.Contains(RequiredCheckType.FaceMatch) ||
                optionalChecks.Contains(RequiredCheckType.FaceMatch),
            CaptureArtifactType.LivenessMedia =>
                session.RequiredChecks.Contains(RequiredCheckType.Liveness) ||
                optionalChecks.Contains(RequiredCheckType.Liveness),
            CaptureArtifactType.FingerprintCapture =>
                session.RequiredChecks.Contains(RequiredCheckType.Fingerprint) ||
                optionalChecks.Contains(RequiredCheckType.Fingerprint),
            CaptureArtifactType.DocumentFrontImage or CaptureArtifactType.DocumentBackImage =>
                session.RequiredChecks.Contains(RequiredCheckType.DocumentOcr) ||
                optionalChecks.Contains(RequiredCheckType.DocumentOcr) ||
                supportingTypes.Contains(artifactType),
            CaptureArtifactType.DeviceCaptureMetadata =>
                supportingTypes.Contains(CaptureArtifactType.DeviceCaptureMetadata) &&
                (session.RequiredChecks.Count > 0 || optionalChecks.Count > 0),
            _ => false,
        };

        if (allowed)
        {
            return null;
        }

        return artifactType == CaptureArtifactType.FingerprintCapture
            ? Forbidden<T>("FINGERPRINT_NOT_ENABLED", "Fingerprint capture is not enabled for this session.")
            : Forbidden<T>("ARTIFACT_TYPE_NOT_ALLOWED", "Artifact type is not allowed for this session.");
    }

    private static SessionOperationResult<T>? ValidateCaptureHashes<T>(
        CaptureArtifactType artifactType,
        string? artifactHashValue,
        string? metadataHashValue,
        out HashRef? artifactHash,
        out HashRef? metadataHash)
    {
        artifactHash = null;
        metadataHash = null;

        var artifactHashRequired = artifactType != CaptureArtifactType.DeviceCaptureMetadata;
        if (artifactHashRequired && string.IsNullOrWhiteSpace(artifactHashValue))
        {
            return SessionOperationResult<T>.Failure(
                "INVALID_CAPTURE_ARTIFACT",
                "ArtifactHash is required for this artifact type.",
                400);
        }

        if (string.IsNullOrWhiteSpace(artifactHashValue) && string.IsNullOrWhiteSpace(metadataHashValue))
        {
            return SessionOperationResult<T>.Failure(
                "INVALID_CAPTURE_ARTIFACT",
                "At least one stable artifact hash or metadata hash is required.",
                400);
        }

        if (!string.IsNullOrWhiteSpace(artifactHashValue))
        {
            if (!TryCreateHashRef(artifactHashValue, out var parsedArtifactHash))
            {
                return SessionOperationResult<T>.Failure(
                    "INVALID_HASH_REF",
                    "ArtifactHash must use the sha256: prefix.",
                    400);
            }

            artifactHash = parsedArtifactHash;
        }

        if (!string.IsNullOrWhiteSpace(metadataHashValue))
        {
            if (!TryCreateHashRef(metadataHashValue, out var parsedMetadataHash))
            {
                return SessionOperationResult<T>.Failure(
                    "INVALID_HASH_REF",
                    "MetadataHash must use the sha256: prefix.",
                    400);
            }

            metadataHash = parsedMetadataHash;
        }

        return null;
    }

    private static bool IsCompatibleInput(
        EvidenceResultTypeDto resultType,
        CaptureArtifactType artifactType,
        LocalDevClientPolicy policy)
    {
        var supportingTypes = policy.AllowedSupportingArtifactTypes ?? new HashSet<CaptureArtifactType>();
        return resultType switch
        {
            EvidenceResultTypeDto.CaptureQuality => true,
            EvidenceResultTypeDto.DocumentOcr =>
                artifactType is CaptureArtifactType.DocumentFrontImage or CaptureArtifactType.DocumentBackImage,
            EvidenceResultTypeDto.NfcValidation => artifactType == CaptureArtifactType.NfcReadArtifact,
            EvidenceResultTypeDto.FaceMatch =>
                artifactType == CaptureArtifactType.SelfieImage ||
                (artifactType is CaptureArtifactType.DocumentFrontImage or CaptureArtifactType.DocumentBackImage &&
                 supportingTypes.Contains(artifactType)),
            EvidenceResultTypeDto.Liveness =>
                artifactType == CaptureArtifactType.LivenessMedia ||
                (artifactType == CaptureArtifactType.SelfieImage && supportingTypes.Contains(CaptureArtifactType.SelfieImage)),
            EvidenceResultTypeDto.FingerprintMatch => artifactType == CaptureArtifactType.FingerprintCapture,
            EvidenceResultTypeDto.FraudRisk => false,
            _ => false,
        };
    }

    private static bool IsReadyToComplete(
        VerificationSession session,
        IEnumerable<EvidenceResult> allEvidence)
    {
        var latestByCheck = allEvidence
            .Select(evidence => new
            {
                Check = MapEvidenceToCheck(evidence.ResultType),
                Evidence = evidence,
            })
            .GroupBy(item => item.Check)
            .ToDictionary(group => group.Key, group => group.Last().Evidence);

        return session.RequiredChecks.All(check =>
            latestByCheck.TryGetValue(check, out var latest) &&
            latest.Result == VerificationResult.Passed);
    }

    private static bool IsSanitizedSummaryRefAllowed(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var lowered = value.ToLowerInvariant();
        var forbiddenFragments = new[]
        {
            "vault:",
            "file:",
            "http://",
            "https://",
            "raw",
            "plaintext",
            "cccd",
            "fingerprint",
            "biometric",
        };

        return !forbiddenFragments.Any(lowered.Contains);
    }

    private static EvidenceResultType ToDomain(EvidenceResultTypeDto resultType) =>
        resultType switch
        {
            EvidenceResultTypeDto.CaptureQuality => EvidenceResultType.CaptureQuality,
            EvidenceResultTypeDto.DocumentOcr => EvidenceResultType.DocumentOcr,
            EvidenceResultTypeDto.NfcValidation => EvidenceResultType.NfcValidation,
            EvidenceResultTypeDto.FaceMatch => EvidenceResultType.FaceMatch,
            EvidenceResultTypeDto.Liveness => EvidenceResultType.Liveness,
            EvidenceResultTypeDto.FingerprintMatch => EvidenceResultType.FingerprintMatch,
            EvidenceResultTypeDto.FraudRisk => EvidenceResultType.FraudRisk,
            _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null),
        };

    private static RequiredCheckType MapEvidenceToCheck(EvidenceResultType resultType) =>
        resultType switch
        {
            EvidenceResultType.CaptureQuality => RequiredCheckType.CaptureQuality,
            EvidenceResultType.DocumentOcr => RequiredCheckType.DocumentOcr,
            EvidenceResultType.NfcValidation => RequiredCheckType.DocumentNfc,
            EvidenceResultType.FaceMatch => RequiredCheckType.FaceMatch,
            EvidenceResultType.Liveness => RequiredCheckType.Liveness,
            EvidenceResultType.FingerprintMatch => RequiredCheckType.Fingerprint,
            EvidenceResultType.FraudRisk => RequiredCheckType.RiskEvaluation,
            _ => throw new ArgumentOutOfRangeException(nameof(resultType), resultType, null),
        };

    private static CaptureArtifactType ToDomain(CaptureArtifactTypeDto artifactType) =>
        artifactType switch
        {
            CaptureArtifactTypeDto.DocumentFrontImage => CaptureArtifactType.DocumentFrontImage,
            CaptureArtifactTypeDto.DocumentBackImage => CaptureArtifactType.DocumentBackImage,
            CaptureArtifactTypeDto.SelfieImage => CaptureArtifactType.SelfieImage,
            CaptureArtifactTypeDto.LivenessMedia => CaptureArtifactType.LivenessMedia,
            CaptureArtifactTypeDto.NfcReadArtifact => CaptureArtifactType.NfcReadArtifact,
            CaptureArtifactTypeDto.FingerprintCapture => CaptureArtifactType.FingerprintCapture,
            CaptureArtifactTypeDto.DeviceCaptureMetadata => CaptureArtifactType.DeviceCaptureMetadata,
            _ => throw new ArgumentOutOfRangeException(nameof(artifactType), artifactType, null),
        };

    private static CaptureSource ToDomain(CaptureSourceDto captureSource) =>
        captureSource switch
        {
            CaptureSourceDto.MobileSdk => CaptureSource.MobileSdk,
            CaptureSourceDto.PcAgent => CaptureSource.PcAgent,
            CaptureSourceDto.KioskAgent => CaptureSource.KioskAgent,
            CaptureSourceDto.DeviceGateway => CaptureSource.DeviceGateway,
            CaptureSourceDto.InternalAdapter => CaptureSource.InternalAdapter,
            CaptureSourceDto.ExternalPreStaged => CaptureSource.ExternalPreStaged,
            _ => throw new ArgumentOutOfRangeException(nameof(captureSource), captureSource, null),
        };

    private static VerificationResult ToDomain(VerificationResultDto result) =>
        result switch
        {
            VerificationResultDto.Passed => VerificationResult.Passed,
            VerificationResultDto.RetryRequired => VerificationResult.RetryRequired,
            VerificationResultDto.FailedCaptureQuality => VerificationResult.FailedCaptureQuality,
            VerificationResultDto.FailedIdentity => VerificationResult.FailedIdentity,
            VerificationResultDto.ReviewRequired => VerificationResult.ReviewRequired,
            VerificationResultDto.TechnicalError => VerificationResult.TechnicalError,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
        };

    private static SignaturePlaceholderStatus ToDomain(SignaturePlaceholderStatusDto status) =>
        status switch
        {
            SignaturePlaceholderStatusDto.PlaceholderUnverified => SignaturePlaceholderStatus.PlaceholderUnverified,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    private static SessionOperationResult<T> Forbidden<T>(string code, string message) =>
        SessionOperationResult<T>.Failure(code, message, 403);

    private static SessionOperationResult<T> NotFound<T>(string code, string message) =>
        SessionOperationResult<T>.Failure(code, message, 404);

    private static bool TryCreateHashRef(string? value, out HashRef hashRef)
    {
        try
        {
            hashRef = new HashRef(value ?? string.Empty);
            return true;
        }
        catch (ArgumentException)
        {
            hashRef = default;
            return false;
        }
    }

    private static AuditEvent CreateAuditEvent(
        AuthenticatedClientContext caller,
        VerificationSession session,
        string eventType) =>
        new(
            Guid.NewGuid(),
            caller.ClientApplicationId,
            session.Id,
            caller.CallerCategory.ToString(),
            caller.KeyPrefix,
            eventType,
            new HashRef($"sha256:localdev-{eventType.ToLowerInvariant()}"),
            EventPayloadRef: null,
            session.RequestId,
            session.CorrelationId,
            DateTimeOffset.UtcNow);

    private static string FormatId(Guid id) => id.ToString("N");

    private sealed record WritableSessionContext<T>(VerificationSession Session, LocalDevClientPolicy Policy);
}
