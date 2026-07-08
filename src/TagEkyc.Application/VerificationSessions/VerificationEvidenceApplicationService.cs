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
    ILocalDevClientPolicyProvider policies,
    IAppendIdempotencyRepository idempotencyRecords,
    IAppendIdempotencyBoundary appendBoundary,
    IMetadataReferenceRegistry? metadataReferences = null)
    : ICaptureArtifactCommands, ITrustedEvidenceResultCommands
{
    private const decimal FaceMatchThreshold = 0.80m;
    private const decimal LivenessThreshold = 0.80m;

    private static readonly IReadOnlySet<string> RequiredNfcChipFlags = new HashSet<string>(StringComparer.Ordinal)
    {
        "NFC_READ_OK",
        "PACE_SUCCESS",
        "SOD_INTERNAL_VALID",
        "DG_HASHES_MATCH_SOD",
    };

    private static readonly IReadOnlySet<string> CscaStateFlags = new HashSet<string>(StringComparer.Ordinal)
    {
        "CSCA_VERIFIED",
        "CSCA_NOT_VERIFIED",
    };

    private static readonly IReadOnlySet<string> ChipAuthStateFlags = new HashSet<string>(StringComparer.Ordinal)
    {
        "CHIP_AUTH_RESPONSE_VALID",
        "CHIP_AUTH_WEAK_CONTEXT",
        "CHIP_AUTH_NOT_PERFORMED",
    };

    private static readonly IReadOnlySet<string> NegativeCaptureBindingStates = new HashSet<string>(StringComparer.Ordinal)
    {
        "DIRECT_CLIENT_UPLOAD_UNTRUSTED",
        "CAPTURE_BINDING_MISSING",
        "CAPTURE_BINDING_UNVERIFIED",
    };

    public async Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> AppendCaptureArtifactAsync(
        AuthenticatedClientContext caller,
        string verificationSessionId,
        CaptureArtifactSubmissionRequestDto request,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null)
    {
        var callerError = ApplicationAuthorization.RequireCaptureArtifactAppend<CaptureArtifactSubmissionResponseDto>(caller);
        if (callerError is not null)
        {
            return callerError;
        }

        var key = ValidateIdempotencyKey<CaptureArtifactSubmissionResponseDto>(idempotencyKey);
        if (!key.IsSuccess)
        {
            return SessionOperationResult<CaptureArtifactSubmissionResponseDto>.Failure(
                key.Error!.Code,
                key.Error.Message,
                key.Error.StatusCode);
        }

        var context = await LoadAppendSessionAsync<CaptureArtifactSubmissionResponseDto>(
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
        var existing = await idempotencyRecords.GetAsync(session.Id, key.Value!, cancellationToken);
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
        var submissionSlot = artifact.ArtifactType.ToString();
        var fingerprint = FingerprintCaptureArtifact(artifact);
        if (existing is not null)
        {
            return await ResolveCaptureArtifactReplayAsync(existing, submissionSlot, fingerprint, artifact, session, caller, cancellationToken);
        }

        var writableError = ValidateWritableAppendSession<CaptureArtifactSubmissionResponseDto>(session);
        if (writableError is not null)
        {
            return writableError;
        }

        var finalState = session.State;
        var auditWrites = new List<AuditEvent>();
        if (session.State == VerificationSessionState.Created)
        {
            finalState = VerificationSessionState.InProgress;
            auditWrites.Add(CreateAuditEvent(caller, session, "SESSION_STATE_CHANGED"));
        }

        auditWrites.Add(CreateAuditEvent(caller, session, "CAPTURE_ARTIFACT_RECORDED"));
        var apply = await appendBoundary.TryApplyCaptureArtifactAsync(
            new AppendCaptureArtifactWrite(
                new AppendIdempotencyRecord(
                    session.Id,
                    key.Value!,
                    "captureArtifact",
                    submissionSlot,
                    artifact.Id,
                    fingerprint,
                    now),
                artifact,
                session,
                finalState,
                auditWrites),
            cancellationToken);
        if (apply.Status == AppendIdempotencyApplyStatus.SessionTerminal)
        {
            return SessionTerminal<CaptureArtifactSubmissionResponseDto>();
        }

        if (apply.Status != AppendIdempotencyApplyStatus.Applied)
        {
            return await ResolveCaptureArtifactReplayAsync(apply.Record, submissionSlot, fingerprint, artifact, session, caller, cancellationToken);
        }

        await RegisterCaptureArtifactMetadataReferenceAsync(artifact, now, cancellationToken);

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
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null)
    {
        var callerError = ApplicationAuthorization.RequireTrustedEvidenceAppend<EvidenceResultSubmissionResponseDto>(caller);
        if (callerError is not null)
        {
            return callerError;
        }

        var key = ValidateIdempotencyKey<EvidenceResultSubmissionResponseDto>(idempotencyKey);
        if (!key.IsSuccess)
        {
            return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                key.Error!.Code,
                key.Error.Message,
                key.Error.StatusCode);
        }

        var context = await LoadAppendSessionAsync<EvidenceResultSubmissionResponseDto>(
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
        var existing = await idempotencyRecords.GetAsync(session.Id, key.Value!, cancellationToken);
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

        if (!IsSanitizedSummaryRefAllowed(request.SanitizedSummaryRef))
        {
            return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                "INVALID_EVIDENCE_RESULT",
                "SanitizedSummaryRef must not contain vault refs, raw paths, sensitive URLs, raw refs, or plaintext identity payloads.",
                400);
        }

        var priorEvidence = await evidenceResults.ListBySessionAsync(session.Id, cancellationToken);
        var effectiveResult = request.Result;
        var effectiveReasonCodes = request.ReasonCodes ?? [];
        HashRef payloadHash;
        if (resultType == EvidenceResultType.NfcValidation)
        {
            var nfcPreparation = PrepareNfcEvidenceResult(
                session,
                request,
                inputArtifacts);
            if (!nfcPreparation.IsSuccess)
            {
                return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                    nfcPreparation.Error!.Code,
                    nfcPreparation.Error.Message,
                    nfcPreparation.Error.StatusCode);
            }

            effectiveResult = nfcPreparation.Value!.Result;
            effectiveReasonCodes = nfcPreparation.Value.ReasonCodes;
            payloadHash = nfcPreparation.Value.PayloadHash;
        }
        else if (resultType == EvidenceResultType.FaceMatch)
        {
            var faceMatchPreparation = PrepareFaceMatchEvidenceResult(
                session,
                request,
                inputArtifacts,
                sessionArtifacts,
                priorEvidence);
            if (!faceMatchPreparation.IsSuccess)
            {
                return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                    faceMatchPreparation.Error!.Code,
                    faceMatchPreparation.Error.Message,
                    faceMatchPreparation.Error.StatusCode);
            }

            effectiveResult = faceMatchPreparation.Value!.Result;
            effectiveReasonCodes = faceMatchPreparation.Value.ReasonCodes;
            payloadHash = faceMatchPreparation.Value.PayloadHash;
        }
        else if (resultType == EvidenceResultType.Liveness)
        {
            var livenessPreparation = PrepareLivenessEvidenceResult(
                session,
                request,
                inputArtifacts);
            if (!livenessPreparation.IsSuccess)
            {
                return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                    livenessPreparation.Error!.Code,
                    livenessPreparation.Error.Message,
                    livenessPreparation.Error.StatusCode);
            }

            effectiveResult = livenessPreparation.Value!.Result;
            effectiveReasonCodes = livenessPreparation.Value.ReasonCodes;
            payloadHash = livenessPreparation.Value.PayloadHash;
        }
        else if (!TryCreateHashRef(request.PayloadHash, out payloadHash))
        {
            return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Failure(
                "INVALID_HASH_REF",
                "PayloadHash is required and must use the sha256: prefix.",
                400);
        }

        var now = DateTimeOffset.UtcNow;
        var evidence = new EvidenceResult(
            Guid.NewGuid(),
            session.Id,
            VerificationCheckId: null,
            resultType,
            inputArtifacts.Select(artifact => artifact.Id).ToArray(),
            ToDomain(effectiveResult),
            request.Confidence,
            effectiveReasonCodes,
            request.RetryReasonCode,
            request.SanitizedSummaryRef,
            payloadHash,
            ToDomain(request.PayloadSignatureStatus),
            request.EngineName,
            request.EngineVersion,
            request.RequestId ?? session.RequestId,
            request.CorrelationId ?? session.CorrelationId,
            now);
        var submissionSlot = evidence.ResultType.ToString();
        var fingerprint = FingerprintEvidenceResult(evidence);
        if (existing is not null)
        {
            return await ResolveEvidenceResultReplayAsync(existing, submissionSlot, fingerprint, evidence, session, caller, cancellationToken);
        }

        var writableError = ValidateWritableAppendSession<EvidenceResultSubmissionResponseDto>(session);
        if (writableError is not null)
        {
            return writableError;
        }

        var finalState = session.State;
        var stateEvents = new List<string>();

        if (session.State == VerificationSessionState.Created)
        {
            finalState = VerificationSessionState.InProgress;
            stateEvents.Add("SESSION_STATE_CHANGED");
        }

        if (EvidenceSelection.AllRequiredChecksPassed(session, priorEvidence.Concat([evidence])) &&
            finalState == VerificationSessionState.InProgress)
        {
            finalState = VerificationSessionState.ReadyToComplete;
            stateEvents.Add("SESSION_STATE_CHANGED");
        }

        var auditWrites = stateEvents
            .Select(stateEvent => CreateAuditEvent(caller, session, stateEvent))
            .Append(CreateAuditEvent(caller, session, "EVIDENCE_RESULT_RECORDED"))
            .ToArray();
        var apply = await appendBoundary.TryApplyEvidenceResultAsync(
            new AppendEvidenceResultWrite(
                new AppendIdempotencyRecord(
                    session.Id,
                    key.Value!,
                    "evidenceResult",
                    submissionSlot,
                    evidence.Id,
                    fingerprint,
                    now),
                evidence,
                session,
                finalState,
                auditWrites),
            cancellationToken);
        if (apply.Status == AppendIdempotencyApplyStatus.SessionTerminal)
        {
            return SessionTerminal<EvidenceResultSubmissionResponseDto>();
        }

        if (apply.Status != AppendIdempotencyApplyStatus.Applied)
        {
            return await ResolveEvidenceResultReplayAsync(apply.Record, submissionSlot, fingerprint, evidence, session, caller, cancellationToken);
        }

        return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Success(new EvidenceResultSubmissionResponseDto(
            FormatId(evidence.Id),
            Accepted: true,
            finalState.ToString(),
            NextAction: effectiveResult == VerificationResultDto.RetryRequired ? "RETRY_CAPTURE" : null));
    }

    private static SessionOperationResult<string> ValidateIdempotencyKey<T>(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return SessionOperationResult<string>.Failure(
                "IDEMPOTENCY_KEY_REQUIRED",
                "Idempotency-Key is required for append operations.",
                400);
        }

        if (idempotencyKey.Length > 256 || idempotencyKey.Any(char.IsControl))
        {
            return SessionOperationResult<string>.Failure(
                "IDEMPOTENCY_KEY_INVALID_FORMAT",
                "Idempotency-Key must be 256 characters or fewer and must not contain control characters.",
                400);
        }

        return SessionOperationResult<string>.Success(idempotencyKey);
    }

    private async Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> ResolveCaptureArtifactReplayAsync(
        AppendIdempotencyRecord record,
        string submissionSlot,
        string fingerprint,
        CaptureArtifact artifact,
        VerificationSession session,
        AuthenticatedClientContext caller,
        CancellationToken cancellationToken)
    {
        var mismatch = ResolveReplayMismatch<CaptureArtifactSubmissionResponseDto>(record, "captureArtifact", submissionSlot, fingerprint);
        if (mismatch is not null)
        {
            return mismatch;
        }

        await auditEvents.AppendAsync(CreateDeduplicatedAuditEvent(caller, session, "CAPTURE_ARTIFACT_DEDUPLICATED", record), cancellationToken);
        return SessionOperationResult<CaptureArtifactSubmissionResponseDto>.Success(new CaptureArtifactSubmissionResponseDto(
            FormatId(record.MintedId),
            FormatId(session.Id),
            artifact.ArtifactHash?.ToString(),
            Accepted: true,
            session.State.ToString(),
            artifact.CorrelationId,
            Deduplicated: true));
    }

    private async Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> ResolveEvidenceResultReplayAsync(
        AppendIdempotencyRecord record,
        string submissionSlot,
        string fingerprint,
        EvidenceResult evidence,
        VerificationSession session,
        AuthenticatedClientContext caller,
        CancellationToken cancellationToken)
    {
        var mismatch = ResolveReplayMismatch<EvidenceResultSubmissionResponseDto>(record, "evidenceResult", submissionSlot, fingerprint);
        if (mismatch is not null)
        {
            return mismatch;
        }

        await auditEvents.AppendAsync(CreateDeduplicatedAuditEvent(caller, session, "EVIDENCE_RESULT_DEDUPLICATED", record), cancellationToken);
        return SessionOperationResult<EvidenceResultSubmissionResponseDto>.Success(new EvidenceResultSubmissionResponseDto(
            FormatId(record.MintedId),
            Accepted: true,
            session.State.ToString(),
            NextAction: evidence.Result == VerificationResult.RetryRequired ? "RETRY_CAPTURE" : null,
            Deduplicated: true));
    }

    private static SessionOperationResult<T>? ResolveReplayMismatch<T>(
        AppendIdempotencyRecord record,
        string endpointKind,
        string submissionSlot,
        string fingerprint)
    {
        if (!string.Equals(record.EndpointKind, endpointKind, StringComparison.Ordinal) ||
            !string.Equals(record.SubmissionSlot, submissionSlot, StringComparison.Ordinal))
        {
            return SessionOperationResult<T>.Failure(
                "IDEMPOTENCY_KEY_SLOT_MISMATCH",
                "Idempotency-Key was already used for a different append slot.",
                409);
        }

        if (!string.Equals(record.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            return SessionOperationResult<T>.Failure(
                "IDEMPOTENCY_KEY_PAYLOAD_MISMATCH",
                "Idempotency-Key was already used for a materially different payload.",
                409);
        }

        return null;
    }

    private static SessionOperationResult<T> SessionTerminal<T>() =>
        SessionOperationResult<T>.Failure(
            "SESSION_TERMINAL",
            "Verification session is terminal.",
            409);

    private static string FingerprintCaptureArtifact(CaptureArtifact artifact) =>
        EvidenceCanonicalization.HashCanonical("tip-80s-i-append-idempotency-capture-artifact", new
        {
            endpointKind = "captureArtifact",
            sessionId = artifact.VerificationSessionId.ToString("N"),
            artifactType = artifact.ArtifactType.ToString(),
            artifactHash = artifact.ArtifactHash?.ToString(),
            metadataHash = artifact.MetadataHash?.ToString(),
            captureSource = artifact.CaptureSource.ToString(),
            deviceId = artifact.DeviceId,
            captureAgentId = artifact.CaptureAgentId,
            requestId = artifact.RequestId,
            correlationId = artifact.CorrelationId,
        });

    private static string FingerprintEvidenceResult(EvidenceResult evidence) =>
        EvidenceCanonicalization.HashCanonical("tip-80s-i-append-idempotency-evidence-result", new
        {
            endpointKind = "evidenceResult",
            sessionId = evidence.VerificationSessionId.ToString("N"),
            resultType = evidence.ResultType.ToString(),
            effectiveResult = evidence.Result.ToString(),
            effectiveReasonCodes = evidence.ReasonCodes.ToArray(),
            evidence.Confidence,
            evidence.RetryReasonCode,
            evidence.SanitizedSummaryRef,
            effectivePayloadHash = evidence.PayloadHash?.ToString(),
            payloadSignatureStatus = evidence.PayloadSignatureStatus.ToString(),
            evidence.EngineName,
            evidence.EngineVersion,
            inputCaptureArtifactIds = evidence.InputCaptureArtifactIds
                .Select(id => id.ToString("N"))
                .Order(StringComparer.Ordinal)
                .ToArray(),
            requestId = evidence.RequestId,
            correlationId = evidence.CorrelationId,
        });

    private static SessionOperationResult<NfcEvidencePreparation> PrepareNfcEvidenceResult(
        VerificationSession session,
        EvidenceResultSubmissionRequestDto request,
        IReadOnlyList<CaptureArtifact> inputArtifacts)
    {
        var basis = request.NfcEvidenceDecisionBasis;
        if (basis is null)
        {
            return request.Result == VerificationResultDto.Passed
                ? SessionOperationResult<NfcEvidencePreparation>.Failure(
                    "NFC_EVIDENCE_DECOMPOSITION_REQUIRED",
                    "NfcValidation=Passed requires decomposed chip-evidence decision-basis flags.",
                    400)
                : SessionOperationResult<NfcEvidencePreparation>.Failure(
                    "NFC_EVIDENCE_DECISION_BASIS_REQUIRED",
                    "NfcValidation evidence requires NfcEvidenceDecisionBasis.",
                    400);
        }

        if (!IsSanitizedSummaryRefAllowed(basis.SanitizedSummaryLabel))
        {
            return SessionOperationResult<NfcEvidencePreparation>.Failure(
                "INVALID_EVIDENCE_RESULT",
                "NfcEvidenceDecisionBasis.SanitizedSummaryLabel must be sanitized.",
                400);
        }

        var submittedFlags = NormalizeFlags(basis.Flags);
        var missingChipDecomposition = MissingRequiredNfcChipFlags(submittedFlags);
        if (request.Result == VerificationResultDto.Passed && missingChipDecomposition.Count > 0)
        {
            return SessionOperationResult<NfcEvidencePreparation>.Failure(
                "NFC_EVIDENCE_DECOMPOSITION_REQUIRED",
                "NfcValidation=Passed requires NFC_READ_OK, PACE_SUCCESS, SOD_INTERNAL_VALID, DG_HASHES_MATCH_SOD, one CSCA state, and one chip-auth state.",
                400);
        }

        var captureBindingTrusted = IsCaptureBindingTrusted(session, inputArtifacts, basis.CaptureBinding);
        var hasNegativeBindingState = submittedFlags.Overlaps(NegativeCaptureBindingStates);
        var effectiveResult = request.Result;
        var finalFlags = new SortedSet<string>(submittedFlags, StringComparer.Ordinal);
        var reasonCodes = new SortedSet<string>(request.ReasonCodes ?? [], StringComparer.Ordinal);

        if (!captureBindingTrusted || hasNegativeBindingState)
        {
            finalFlags.Remove("CAPTURE_BOUND_TO_SESSION");
            finalFlags.Add("DIRECT_CLIENT_UPLOAD_UNTRUSTED");
            reasonCodes.Add("DIRECT_CLIENT_UPLOAD_UNTRUSTED");

            if (request.Result == VerificationResultDto.Passed)
            {
                effectiveResult = VerificationResultDto.ReviewRequired;
            }
        }
        else
        {
            finalFlags.Add("CAPTURE_BOUND_TO_SESSION");
        }

        var normalizedBasis = BuildNormalizedNfcDecisionBasis(
            session,
            request,
            inputArtifacts,
            basis,
            finalFlags,
            effectiveResult);
        var computedPayloadHashValue = EvidenceCanonicalization.HashCanonical(
            "tip-69-nfc-evidence-decision-basis",
            normalizedBasis);

        if (!string.IsNullOrWhiteSpace(request.PayloadHash) &&
            !string.Equals(request.PayloadHash, computedPayloadHashValue, StringComparison.Ordinal))
        {
            return SessionOperationResult<NfcEvidencePreparation>.Failure(
                "NFC_PAYLOAD_HASH_MISMATCH",
                "Adapter-supplied PayloadHash does not match the server-computed NFC decision-basis hash.",
                400);
        }

        return SessionOperationResult<NfcEvidencePreparation>.Success(new NfcEvidencePreparation(
            effectiveResult,
            reasonCodes.ToArray(),
            new HashRef(computedPayloadHashValue)));
    }

    private static SortedSet<string> NormalizeFlags(IReadOnlyList<string>? flags) =>
        new((flags ?? [])
            .Where(flag => !string.IsNullOrWhiteSpace(flag))
            .Select(flag => flag.Trim())
            .Distinct(StringComparer.Ordinal),
            StringComparer.Ordinal);

    private static IReadOnlyList<string> MissingRequiredNfcChipFlags(IReadOnlySet<string> flags)
    {
        var missing = RequiredNfcChipFlags
            .Where(required => !flags.Contains(required))
            .ToList();
        if (!CscaStateFlags.Any(flags.Contains))
        {
            missing.Add("CSCA_STATE");
        }

        if (!ChipAuthStateFlags.Any(flags.Contains))
        {
            missing.Add("CHIP_AUTH_STATE");
        }

        return missing;
    }

    private static bool IsCaptureBindingTrusted(
        VerificationSession session,
        IReadOnlyList<CaptureArtifact> inputArtifacts,
        NfcCaptureBindingDto? captureBinding)
    {
        if (captureBinding is null ||
            inputArtifacts.Count == 0 ||
            inputArtifacts.Any(artifact => artifact.CaptureSource == CaptureSource.ExternalPreStaged))
        {
            return false;
        }

        var expectedChallengeHash = BuildSessionChallengeHash(session);
        if (expectedChallengeHash is null ||
            !string.Equals(captureBinding.ChallengeHash, expectedChallengeHash, StringComparison.Ordinal) ||
            !string.Equals(captureBinding.SessionId, session.Id.ToString("N"), StringComparison.Ordinal))
        {
            return false;
        }

        return inputArtifacts.Any(artifact =>
            string.Equals(captureBinding.ArtifactHash, artifact.ArtifactHash?.ToString(), StringComparison.Ordinal) &&
            string.Equals(captureBinding.CaptureAgentId, artifact.CaptureAgentId, StringComparison.Ordinal) &&
            string.Equals(captureBinding.DeviceId, artifact.DeviceId, StringComparison.Ordinal));
    }

    private static string? BuildSessionChallengeHash(VerificationSession session) =>
        string.IsNullOrEmpty(session.Challenge)
            ? null
            : EvidenceCanonicalization.HashCanonical("tip-69-capture-session-challenge", new
            {
                sessionId = session.Id.ToString("N"),
                challenge = session.Challenge,
            });

    private static object BuildNormalizedNfcDecisionBasis(
        VerificationSession session,
        EvidenceResultSubmissionRequestDto request,
        IReadOnlyList<CaptureArtifact> inputArtifacts,
        NfcEvidenceDecisionBasisDto submittedBasis,
        IReadOnlySet<string> finalFlags,
        VerificationResultDto effectiveResult)
    {
        var primaryArtifact = inputArtifacts.OrderBy(artifact => artifact.Id).First();
        var submittedBinding = submittedBasis.CaptureBinding;

        return new
        {
            extension = new
            {
                id = "nfc-validation",
                requiredCheckType = "DocumentNfc",
                category = "IdentityEvidence",
                emitsEvidenceType = "NfcValidation",
            },
            flags = finalFlags.Order(StringComparer.Ordinal).ToArray(),
            captureBinding = new
            {
                challengeHash = submittedBinding?.ChallengeHash,
                sessionId = session.Id.ToString("N"),
                captureAgentId = primaryArtifact.CaptureAgentId,
                deviceId = primaryArtifact.DeviceId,
                capturedAt = submittedBinding?.CapturedAt ?? primaryArtifact.CreatedAt,
                artifactHash = primaryArtifact.ArtifactHash?.ToString(),
            },
            serverDecisionResult = effectiveResult.ToString(),
            adapterRequestedResult = request.Result.ToString(),
            engineName = request.EngineName,
            engineVersion = request.EngineVersion,
            inputArtifacts = inputArtifacts
                .OrderBy(artifact => artifact.Id)
                .Select(artifact => new
                {
                    artifactId = artifact.Id.ToString("N"),
                    artifactHash = artifact.ArtifactHash?.ToString(),
                })
                .ToArray(),
            sanitizedSummaryLabel = submittedBasis.SanitizedSummaryLabel ?? request.SanitizedSummaryRef,
        };
    }

    private static SessionOperationResult<FaceMatchEvidencePreparation> PrepareFaceMatchEvidenceResult(
        VerificationSession session,
        EvidenceResultSubmissionRequestDto request,
        IReadOnlyList<CaptureArtifact> inputArtifacts,
        IReadOnlyList<CaptureArtifact> sessionArtifacts,
        IReadOnlyList<EvidenceResult> priorEvidence)
    {
        var basis = request.FaceMatchEvidenceDecisionBasis;
        if (basis is null || basis.MatchScore is null)
        {
            return SessionOperationResult<FaceMatchEvidencePreparation>.Failure(
                "FACE_MATCH_DECISION_BASIS_REQUIRED",
                "FaceMatch evidence requires a decision-basis with a match score.",
                400);
        }

        if (!IsSanitizedSummaryRefAllowed(basis.SanitizedSummaryLabel))
        {
            return SessionOperationResult<FaceMatchEvidencePreparation>.Failure(
                "INVALID_EVIDENCE_RESULT",
                "FaceMatchEvidenceDecisionBasis.SanitizedSummaryLabel must be sanitized.",
                400);
        }

        if (basis.MatchScore is < 0 or > 1)
        {
            return SessionOperationResult<FaceMatchEvidencePreparation>.Failure(
                "FACE_MATCH_DECISION_BASIS_MISMATCH",
                "FaceMatch matchScore must be between 0.0 and 1.0.",
                400);
        }

        if (!Guid.TryParse(basis.LiveFaceArtifactId, out var liveArtifactId))
        {
            return SessionOperationResult<FaceMatchEvidencePreparation>.Failure(
                "FACE_MATCH_DECISION_BASIS_REQUIRED",
                "FaceMatch decision-basis must identify the live face artifact.",
                400);
        }

        var liveArtifact = inputArtifacts.SingleOrDefault(artifact => artifact.Id == liveArtifactId);
        if (liveArtifact is null ||
            liveArtifact.ArtifactType != CaptureArtifactType.SelfieImage ||
            !string.Equals(basis.LiveFaceArtifactHash, liveArtifact.ArtifactHash?.ToString(), StringComparison.Ordinal))
        {
            return SessionOperationResult<FaceMatchEvidencePreparation>.Failure(
                "FACE_MATCH_DECISION_BASIS_MISMATCH",
                "FaceMatch live face artifact id/hash must match a submitted SelfieImage input artifact.",
                400);
        }

        var serverIsMatch = basis.MatchScore.Value >= FaceMatchThreshold;
        if (basis.IsMatch.HasValue && basis.IsMatch.Value != serverIsMatch)
        {
            return SessionOperationResult<FaceMatchEvidencePreparation>.Failure(
                "FACE_MATCH_DECISION_BASIS_MISMATCH",
                "Adapter-supplied FaceMatch isMatch conflicts with the server threshold calculation.",
                400);
        }

        var effectiveResult = request.Result;
        var reasonCodes = new SortedSet<string>(request.ReasonCodes ?? [], StringComparer.Ordinal);
        var finalFlags = new SortedSet<string>(StringComparer.Ordinal);

        var captureBindingTrusted = IsFaceCaptureBindingTrusted(session, liveArtifact, basis.LiveCaptureBinding);
        if (captureBindingTrusted)
        {
            finalFlags.Add("CAPTURE_BOUND_TO_SESSION");
        }
        else
        {
            finalFlags.Add("DIRECT_CLIENT_UPLOAD_UNTRUSTED");
            reasonCodes.Add("DIRECT_CLIENT_UPLOAD_UNTRUSTED");
            if (request.Result == VerificationResultDto.Passed)
            {
                effectiveResult = VerificationResultDto.ReviewRequired;
            }
        }

        var referenceValidation = ValidateFaceMatchReference(session, basis, sessionArtifacts, priorEvidence);
        if (!referenceValidation.IsProductionGrade)
        {
            reasonCodes.Add(referenceValidation.ReasonCode);
            if (request.Result == VerificationResultDto.Passed)
            {
                effectiveResult = VerificationResultDto.ReviewRequired;
            }
        }

        if (!serverIsMatch && request.Result == VerificationResultDto.Passed)
        {
            reasonCodes.Add("FACE_MATCH_BELOW_THRESHOLD");
            effectiveResult = VerificationResultDto.ReviewRequired;
        }

        var normalizedBasis = BuildNormalizedFaceMatchDecisionBasis(
            session,
            request,
            liveArtifact,
            basis,
            finalFlags,
            referenceValidation,
            serverIsMatch,
            effectiveResult);
        var computedPayloadHashValue = EvidenceCanonicalization.HashCanonical(
            "tip-70-face-match-decision-basis",
            normalizedBasis);

        if (!string.IsNullOrWhiteSpace(request.PayloadHash) &&
            !string.Equals(request.PayloadHash, computedPayloadHashValue, StringComparison.Ordinal))
        {
            return SessionOperationResult<FaceMatchEvidencePreparation>.Failure(
                "FACE_MATCH_PAYLOAD_HASH_MISMATCH",
                "Adapter-supplied PayloadHash does not match the server-computed FaceMatch decision-basis hash.",
                400);
        }

        return SessionOperationResult<FaceMatchEvidencePreparation>.Success(new FaceMatchEvidencePreparation(
            effectiveResult,
            reasonCodes.ToArray(),
            new HashRef(computedPayloadHashValue)));
    }

    private static bool IsFaceCaptureBindingTrusted(
        VerificationSession session,
        CaptureArtifact liveArtifact,
        FaceMatchCaptureBindingDto? captureBinding)
    {
        if (captureBinding is null ||
            liveArtifact.CaptureSource == CaptureSource.ExternalPreStaged)
        {
            return false;
        }

        var expectedChallengeHash = BuildSessionChallengeHash(session);
        if (expectedChallengeHash is null ||
            !string.Equals(captureBinding.ChallengeHash, expectedChallengeHash, StringComparison.Ordinal) ||
            !string.Equals(captureBinding.SessionId, session.Id.ToString("N"), StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals(captureBinding.ArtifactHash, liveArtifact.ArtifactHash?.ToString(), StringComparison.Ordinal) &&
               string.Equals(captureBinding.CaptureAgentId, liveArtifact.CaptureAgentId, StringComparison.Ordinal) &&
               string.Equals(captureBinding.DeviceId, liveArtifact.DeviceId, StringComparison.Ordinal);
    }

    private static FaceMatchReferenceValidation ValidateFaceMatchReference(
        VerificationSession session,
        FaceMatchEvidenceDecisionBasisDto basis,
        IReadOnlyList<CaptureArtifact> sessionArtifacts,
        IReadOnlyList<EvidenceResult> priorEvidence)
    {
        if (basis.ReferenceFaceSource != FaceMatchReferenceFaceSourceDto.ChipDg2FromTrustedNfc)
        {
            return FaceMatchReferenceValidation.NotProductionGrade("FACE_MATCH_REFERENCE_NOT_PRODUCTION_GRADE");
        }

        if (!string.Equals(basis.ReferenceEvidenceType, nameof(EvidenceResultTypeDto.NfcValidation), StringComparison.Ordinal) ||
            !Guid.TryParse(basis.ReferenceEvidenceResultId, out var referenceEvidenceId))
        {
            return FaceMatchReferenceValidation.NotProductionGrade("FACE_MATCH_REFERENCE_NFC_NOT_TRUSTED");
        }

        var referenceEvidence = priorEvidence.SingleOrDefault(evidence => evidence.Id == referenceEvidenceId);
        if (referenceEvidence is null ||
            referenceEvidence.VerificationSessionId != session.Id ||
            referenceEvidence.ResultType != EvidenceResultType.NfcValidation ||
            referenceEvidence.Result != VerificationResult.Passed ||
            referenceEvidence.PayloadHash is null)
        {
            return FaceMatchReferenceValidation.NotProductionGrade("FACE_MATCH_REFERENCE_NFC_NOT_TRUSTED");
        }

        if (!string.IsNullOrWhiteSpace(basis.ReferencePayloadHash) &&
            !string.Equals(basis.ReferencePayloadHash, referenceEvidence.PayloadHash.ToString(), StringComparison.Ordinal))
        {
            return FaceMatchReferenceValidation.NotProductionGrade("FACE_MATCH_REFERENCE_NFC_NOT_TRUSTED");
        }

        if (!Guid.TryParse(basis.ReferenceArtifactId, out var referenceArtifactId) ||
            !referenceEvidence.InputCaptureArtifactIds.Contains(referenceArtifactId))
        {
            return FaceMatchReferenceValidation.NotProductionGrade("FACE_MATCH_REFERENCE_NFC_NOT_TRUSTED");
        }

        var referenceArtifact = sessionArtifacts.SingleOrDefault(artifact => artifact.Id == referenceArtifactId);
        if (referenceArtifact is null ||
            referenceArtifact.ArtifactType != CaptureArtifactType.NfcReadArtifact ||
            !string.Equals(basis.ReferenceArtifactHash, referenceArtifact.ArtifactHash?.ToString(), StringComparison.Ordinal))
        {
            return FaceMatchReferenceValidation.NotProductionGrade("FACE_MATCH_REFERENCE_NFC_NOT_TRUSTED");
        }

        return new FaceMatchReferenceValidation(
            true,
            ReasonCode: string.Empty,
            referenceEvidence.Id.ToString("N"),
            referenceEvidence.ResultType.ToString(),
            referenceEvidence.PayloadHash.ToString(),
            referenceArtifact.Id.ToString("N"),
            referenceArtifact.ArtifactHash?.ToString());
    }

    private static object BuildNormalizedFaceMatchDecisionBasis(
        VerificationSession session,
        EvidenceResultSubmissionRequestDto request,
        CaptureArtifact liveArtifact,
        FaceMatchEvidenceDecisionBasisDto submittedBasis,
        IReadOnlySet<string> finalFlags,
        FaceMatchReferenceValidation referenceValidation,
        bool serverIsMatch,
        VerificationResultDto effectiveResult)
    {
        var submittedBinding = submittedBasis.LiveCaptureBinding;

        return new
        {
            extension = new
            {
                id = "face-match",
                requiredCheckType = "FaceMatch",
                category = "IdentityEvidence",
                emitsEvidenceType = "FaceMatch",
            },
            flags = finalFlags.Order(StringComparer.Ordinal).ToArray(),
            liveFaceArtifact = new
            {
                artifactId = liveArtifact.Id.ToString("N"),
                artifactHash = liveArtifact.ArtifactHash?.ToString(),
            },
            matchScore = submittedBasis.MatchScore,
            thresholdApplied = FaceMatchThreshold,
            isMatch = serverIsMatch,
            reference = new
            {
                referenceFaceSource = submittedBasis.ReferenceFaceSource?.ToString(),
                referenceEvidenceResultId = referenceValidation.ReferenceEvidenceResultId ?? submittedBasis.ReferenceEvidenceResultId,
                referenceEvidenceType = referenceValidation.ReferenceEvidenceType ?? submittedBasis.ReferenceEvidenceType,
                referenceArtifactId = referenceValidation.ReferenceArtifactId ?? submittedBasis.ReferenceArtifactId,
                referenceArtifactHash = referenceValidation.ReferenceArtifactHash ?? submittedBasis.ReferenceArtifactHash,
                referencePayloadHash = referenceValidation.ReferencePayloadHash ?? submittedBasis.ReferencePayloadHash,
            },
            liveCaptureBinding = new
            {
                challengeHash = submittedBinding?.ChallengeHash,
                sessionId = session.Id.ToString("N"),
                captureAgentId = liveArtifact.CaptureAgentId,
                deviceId = liveArtifact.DeviceId,
                capturedAt = submittedBinding?.CapturedAt ?? liveArtifact.CreatedAt,
                artifactHash = liveArtifact.ArtifactHash?.ToString(),
            },
            serverDecisionResult = effectiveResult.ToString(),
            adapterRequestedResult = request.Result.ToString(),
            engineName = request.EngineName,
            engineVersion = request.EngineVersion,
            sanitizedSummaryLabel = submittedBasis.SanitizedSummaryLabel ?? request.SanitizedSummaryRef,
        };
    }

    private static SessionOperationResult<LivenessEvidencePreparation> PrepareLivenessEvidenceResult(
        VerificationSession session,
        EvidenceResultSubmissionRequestDto request,
        IReadOnlyList<CaptureArtifact> inputArtifacts)
    {
        var basis = request.LivenessEvidenceDecisionBasis;
        if (basis is null || basis.LivenessScore is null)
        {
            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "LIVENESS_DECISION_BASIS_REQUIRED",
                "Liveness evidence requires a decision-basis with a liveness score.",
                400);
        }

        if (!IsSanitizedSummaryRefAllowed(basis.SanitizedSummaryLabel))
        {
            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "INVALID_EVIDENCE_RESULT",
                "LivenessEvidenceDecisionBasis.SanitizedSummaryLabel must be sanitized.",
                400);
        }

        if (basis.LivenessScore is < 0 or > 1)
        {
            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "LIVENESS_DECISION_BASIS_MISMATCH",
                "Liveness livenessScore must be between 0.0 and 1.0.",
                400);
        }

        if (basis.ThresholdApplied.HasValue && basis.ThresholdApplied.Value != LivenessThreshold)
        {
            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "LIVENESS_DECISION_BASIS_MISMATCH",
                "Liveness thresholdApplied conflicts with the server configured threshold.",
                400);
        }

        if (basis.AdapterRequestedResult.HasValue && basis.AdapterRequestedResult.Value != request.Result)
        {
            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "LIVENESS_DECISION_BASIS_MISMATCH",
                "Liveness adapterRequestedResult must match the submitted evidence result.",
                400);
        }

        if (string.IsNullOrWhiteSpace(basis.Method) ||
            string.IsNullOrWhiteSpace(basis.LivenessGrade))
        {
            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "LIVENESS_DECISION_BASIS_REQUIRED",
                "Liveness decision-basis requires method and livenessGrade.",
                400);
        }

        if (!string.Equals(basis.LivenessGrade, "passive-2d-only", StringComparison.Ordinal))
        {
            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "LIVENESS_DECISION_BASIS_MISMATCH",
                "Liveness method and livenessGrade must describe the server-supported fixture path honestly.",
                400);
        }

        var methodConsistency = ValidateLivenessMethodEngineConsistency(
            basis.Method,
            request.EngineName,
            request.EngineVersion);
        if (methodConsistency is not null)
        {
            return methodConsistency;
        }

        if (!Guid.TryParse(basis.LiveMediaArtifactId, out var liveMediaArtifactId))
        {
            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "LIVENESS_DECISION_BASIS_REQUIRED",
                "Liveness decision-basis must identify the live media artifact.",
                400);
        }

        var liveArtifact = inputArtifacts.SingleOrDefault(artifact => artifact.Id == liveMediaArtifactId);
        if (liveArtifact is null ||
            liveArtifact.ArtifactType is not (CaptureArtifactType.LivenessMedia or CaptureArtifactType.SelfieImage) ||
            !string.Equals(basis.LiveMediaArtifactHash, liveArtifact.ArtifactHash?.ToString(), StringComparison.Ordinal))
        {
            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "LIVENESS_DECISION_BASIS_MISMATCH",
                "Liveness live media artifact id/hash must match a submitted LivenessMedia or eligible SelfieImage input artifact.",
                400);
        }

        var serverDerivedIsLive = basis.LivenessScore.Value >= LivenessThreshold;
        if (basis.ServerDerivedIsLive.HasValue && basis.ServerDerivedIsLive.Value != serverDerivedIsLive)
        {
            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "LIVENESS_DECISION_BASIS_MISMATCH",
                "Adapter-supplied serverDerivedIsLive conflicts with the server threshold calculation.",
                400);
        }

        var adapterVerdict = NormalizeAdapterRequestedVerdict(basis.AdapterRequestedVerdict);
        if (basis.AdapterRequestedVerdict is not null && adapterVerdict is null)
        {
            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "LIVENESS_DECISION_BASIS_MISMATCH",
                "Liveness adapterRequestedVerdict must be live, spoof, or uncertain.",
                400);
        }

        var effectiveResult = request.Result;
        var reasonCodes = new SortedSet<string>(request.ReasonCodes ?? [], StringComparer.Ordinal);
        var finalFlags = new SortedSet<string>(StringComparer.Ordinal);
        var captureBindingTrusted = IsLivenessCaptureBindingTrusted(session, liveArtifact, basis.LiveCaptureBinding);

        if (captureBindingTrusted)
        {
            finalFlags.Add("CAPTURE_BOUND_TO_SESSION");
        }
        else
        {
            finalFlags.Add("DIRECT_CLIENT_UPLOAD_UNTRUSTED");
            reasonCodes.Add("DIRECT_CLIENT_UPLOAD_UNTRUSTED");
            if (request.Result == VerificationResultDto.Passed)
            {
                effectiveResult = VerificationResultDto.ReviewRequired;
            }
        }

        if (!serverDerivedIsLive && request.Result == VerificationResultDto.Passed)
        {
            reasonCodes.Add("LIVENESS_BELOW_THRESHOLD");
            effectiveResult = VerificationResultDto.ReviewRequired;
        }

        if (AdapterVerdictConflictsWithServerDerived(adapterVerdict, serverDerivedIsLive))
        {
            reasonCodes.Add("LIVENESS_VERDICT_MISMATCH");
            effectiveResult = VerificationResultDto.ReviewRequired;
        }

        var normalizedBasis = BuildNormalizedLivenessDecisionBasis(
            session,
            request,
            liveArtifact,
            basis,
            finalFlags,
            adapterVerdict,
            serverDerivedIsLive,
            effectiveResult);
        var computedPayloadHashValue = EvidenceCanonicalization.HashCanonical(
            "tip-72-liveness-decision-basis",
            normalizedBasis);

        if (!string.IsNullOrWhiteSpace(request.PayloadHash) &&
            !string.Equals(request.PayloadHash, computedPayloadHashValue, StringComparison.Ordinal))
        {
            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "LIVENESS_PAYLOAD_HASH_MISMATCH",
                "Adapter-supplied PayloadHash does not match the server-computed Liveness decision-basis hash.",
                400);
        }

        return SessionOperationResult<LivenessEvidencePreparation>.Success(new LivenessEvidencePreparation(
            effectiveResult,
            reasonCodes.ToArray(),
            new HashRef(computedPayloadHashValue)));
    }

    private static bool IsLivenessCaptureBindingTrusted(
        VerificationSession session,
        CaptureArtifact liveArtifact,
        LivenessCaptureBindingDto? captureBinding)
    {
        if (captureBinding is null ||
            liveArtifact.CaptureSource == CaptureSource.ExternalPreStaged)
        {
            return false;
        }

        var expectedChallengeHash = BuildSessionChallengeHash(session);
        if (expectedChallengeHash is null ||
            !string.Equals(captureBinding.ChallengeHash, expectedChallengeHash, StringComparison.Ordinal) ||
            !string.Equals(captureBinding.SessionId, session.Id.ToString("N"), StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals(captureBinding.ArtifactHash, liveArtifact.ArtifactHash?.ToString(), StringComparison.Ordinal) &&
               string.Equals(captureBinding.CaptureAgentId, liveArtifact.CaptureAgentId, StringComparison.Ordinal) &&
               string.Equals(captureBinding.DeviceId, liveArtifact.DeviceId, StringComparison.Ordinal);
    }

    private static object BuildNormalizedLivenessDecisionBasis(
        VerificationSession session,
        EvidenceResultSubmissionRequestDto request,
        CaptureArtifact liveArtifact,
        LivenessEvidenceDecisionBasisDto submittedBasis,
        IReadOnlySet<string> finalFlags,
        string? adapterRequestedVerdict,
        bool serverDerivedIsLive,
        VerificationResultDto effectiveResult)
    {
        var submittedBinding = submittedBasis.LiveCaptureBinding;

        return new
        {
            extension = new
            {
                id = "liveness",
                requiredCheckType = "Liveness",
                category = "IdentityEvidence",
                emitsEvidenceType = "Liveness",
            },
            flags = finalFlags.Order(StringComparer.Ordinal).ToArray(),
            liveMediaArtifact = new
            {
                artifactId = liveArtifact.Id.ToString("N"),
                artifactHash = liveArtifact.ArtifactHash?.ToString(),
            },
            livenessScore = submittedBasis.LivenessScore,
            adapterRequestedVerdict,
            method = submittedBasis.Method,
            livenessGrade = submittedBasis.LivenessGrade,
            thresholdApplied = LivenessThreshold,
            liveCaptureBinding = new
            {
                challengeHash = submittedBinding?.ChallengeHash,
                sessionId = session.Id.ToString("N"),
                captureAgentId = liveArtifact.CaptureAgentId,
                deviceId = liveArtifact.DeviceId,
                capturedAt = submittedBinding?.CapturedAt ?? liveArtifact.CreatedAt,
                artifactHash = liveArtifact.ArtifactHash?.ToString(),
            },
            serverDerivedIsLive,
            serverDecisionResult = effectiveResult.ToString(),
            adapterRequestedResult = request.Result.ToString(),
            sanitizedSummaryLabel = submittedBasis.SanitizedSummaryLabel ?? request.SanitizedSummaryRef,
        };
    }

    private static string? NormalizeAdapterRequestedVerdict(string? verdict)
    {
        if (string.IsNullOrWhiteSpace(verdict))
        {
            return null;
        }

        var normalized = verdict.Trim().ToLowerInvariant();
        return normalized is "live" or "spoof" or "uncertain" ? normalized : null;
    }

    private static bool AdapterVerdictConflictsWithServerDerived(string? adapterVerdict, bool serverDerivedIsLive) =>
        adapterVerdict switch
        {
            "live" => !serverDerivedIsLive,
            "spoof" or "uncertain" => serverDerivedIsLive,
            _ => false,
        };

    private const string FixtureLivenessMethod = "fixture-liveness";
    private const string FixtureLivenessEngineName = "fixture-liveness";
    private const string SilentFaceMethod = "silent-face";
    private const string SilentFaceEngineName = "silent-face-minifasnet";
    private const string SilentFaceEngineVersion = "minifasnet-v2+onnxruntime-1.20.1";

    private static SessionOperationResult<LivenessEvidencePreparation>? ValidateLivenessMethodEngineConsistency(
        string method,
        string engineName,
        string engineVersion)
    {
        if (string.Equals(method, SilentFaceMethod, StringComparison.Ordinal))
        {
            if (string.Equals(engineName, SilentFaceEngineName, StringComparison.Ordinal) &&
                string.Equals(engineVersion, SilentFaceEngineVersion, StringComparison.Ordinal))
            {
                return null;
            }

            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "LIVENESS_METHOD_UNEARNED",
                "Liveness silent-face method requires an explicit allowlisted engine name and version.",
                400);
        }

        if (string.Equals(method, FixtureLivenessMethod, StringComparison.Ordinal))
        {
            if (string.Equals(engineName, FixtureLivenessEngineName, StringComparison.Ordinal))
            {
                return null;
            }

            return SessionOperationResult<LivenessEvidencePreparation>.Failure(
                "LIVENESS_DECISION_BASIS_MISMATCH",
                "Liveness fixture method requires the fixture liveness engine.",
                400);
        }

        return SessionOperationResult<LivenessEvidencePreparation>.Failure(
            "LIVENESS_DECISION_BASIS_MISMATCH",
            "Liveness method and livenessGrade must describe a server-supported liveness path honestly.",
            400);
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

        if (!ApplicationAuthorization.CanAccessClientApplication(caller, session.ClientApplicationId))
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

    private async Task<SessionOperationResult<WritableSessionContext<T>>> LoadAppendSessionAsync<T>(
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

        if (!ApplicationAuthorization.CanAccessClientApplication(caller, session.ClientApplicationId))
        {
            await auditEvents.AppendAsync(CreateAuditEvent(caller, session, "SESSION_ACCESS_DENIED"), cancellationToken);
            return SessionOperationResult<WritableSessionContext<T>>.Failure(
                "SESSION_ACCESS_DENIED",
                "Caller is not authorized to write this session.",
                403);
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

    private static SessionOperationResult<T>? ValidateWritableAppendSession<T>(VerificationSession session)
    {
        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return SessionOperationResult<T>.Failure(
                "SESSION_EXPIRED",
                "Verification session is expired.",
                403);
        }

        if (session.State == VerificationSessionState.ReadyToComplete)
        {
            return SessionOperationResult<T>.Failure(
                "SESSION_READY_TO_COMPLETE",
                "Verification session is ready to complete and no longer accepts TIP-05 writes.",
                409);
        }

        if (session.State is VerificationSessionState.Completed or
            VerificationSessionState.Cancelled or
            VerificationSessionState.Expired or
            VerificationSessionState.TechnicalTerminal)
        {
            return SessionOperationResult<T>.Failure(
                "SESSION_TERMINAL",
                "Verification session is terminal.",
                409);
        }

        return null;
    }

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

    private async Task RegisterCaptureArtifactMetadataReferenceAsync(
        CaptureArtifact artifact,
        DateTimeOffset registeredAt,
        CancellationToken cancellationToken)
    {
        if (metadataReferences is null)
        {
            return;
        }

        var metadataHash = artifact.MetadataHash ?? artifact.ArtifactHash;
        if (metadataHash is null)
        {
            return;
        }

        await metadataReferences.RegisterAsync(
            new MetadataReferenceRegistration(
                new MetadataReferenceId($"capture-artifact-metadata:{artifact.Id:N}"),
                "capture-artifact-metadata",
                MetadataReferenceState.RegisteredMetadata,
                metadataHash,
                registeredAt),
            cancellationToken);
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

    private static AuditEvent CreateDeduplicatedAuditEvent(
        AuthenticatedClientContext caller,
        VerificationSession session,
        string eventType,
        AppendIdempotencyRecord record)
    {
        var idempotencyKeyHash = EvidenceCanonicalization.HashCanonical("tip-80s-i-idempotency-key-audit", new
        {
            sessionId = session.Id.ToString("N"),
            idempotencyKey = record.IdempotencyKey,
        });

        var auditHash = EvidenceCanonicalization.HashCanonical("tip-80s-i-append-deduplicated-audit", new
        {
            eventType,
            sessionId = session.Id.ToString("N"),
            endpointKind = record.EndpointKind,
            submissionSlot = record.SubmissionSlot,
            mintedId = record.MintedId.ToString("N"),
            idempotencyKeyHash,
            deduplicated = true,
        });

        return new AuditEvent(
            Guid.NewGuid(),
            caller.ClientApplicationId,
            session.Id,
            caller.CallerCategory.ToString(),
            caller.KeyPrefix,
            eventType,
            new HashRef(auditHash),
            EventPayloadRef: null,
            session.RequestId,
            session.CorrelationId,
            DateTimeOffset.UtcNow);
    }

    private static string FormatId(Guid id) => id.ToString("N");

    private sealed record NfcEvidencePreparation(
        VerificationResultDto Result,
        IReadOnlyList<string> ReasonCodes,
        HashRef PayloadHash);

    private sealed record FaceMatchEvidencePreparation(
        VerificationResultDto Result,
        IReadOnlyList<string> ReasonCodes,
        HashRef PayloadHash);

    private sealed record LivenessEvidencePreparation(
        VerificationResultDto Result,
        IReadOnlyList<string> ReasonCodes,
        HashRef PayloadHash);

    private sealed record FaceMatchReferenceValidation(
        bool IsProductionGrade,
        string ReasonCode,
        string? ReferenceEvidenceResultId,
        string? ReferenceEvidenceType,
        string? ReferencePayloadHash,
        string? ReferenceArtifactId,
        string? ReferenceArtifactHash)
    {
        public static FaceMatchReferenceValidation NotProductionGrade(string reasonCode) =>
            new(false, reasonCode, null, null, null, null, null);
    }

    private sealed record WritableSessionContext<T>(VerificationSession Session, LocalDevClientPolicy Policy);
}
