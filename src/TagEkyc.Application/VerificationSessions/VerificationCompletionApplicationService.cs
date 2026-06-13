using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.InternalAudit.Manifest;
using TagEkyc.Domain;

namespace TagEkyc.Application.VerificationSessions;

public sealed class VerificationCompletionApplicationService(
    IVerificationSessionRepository sessions,
    ICaptureArtifactRepository captureArtifacts,
    IEvidenceResultRepository evidenceResults,
    IVerificationDecisionRepository decisions,
    IEvidencePackageRepository evidencePackages,
    IInternalEvidenceManifestRepository manifests,
    IAuditEventRepository auditEvents,
    IVerificationFinalizationBoundary finalizationBoundary)
    : IVerificationSessionCompletionCommands, IEvidencePackageQueries, ICompletionNotificationQueries
{
    private const string PackageVersion = "tip-06-localdev-v1";
    private static readonly JsonSerializerOptions CanonicalJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SessionOperationResult<CompleteVerificationSessionResponseDto>> CompleteAsync(
        AuthenticatedClientContext caller,
        string verificationSessionId,
        CompleteVerificationSessionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var callerError = ApplicationAuthorization.RequireBusinessCompletion<CompleteVerificationSessionResponseDto>(caller);
        if (callerError is not null)
        {
            return callerError;
        }

        if (!Guid.TryParse(verificationSessionId, out var sessionId))
        {
            return NotFound<CompleteVerificationSessionResponseDto>("SESSION_NOT_FOUND", "Verification session was not found.");
        }

        var session = await sessions.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return NotFound<CompleteVerificationSessionResponseDto>("SESSION_NOT_FOUND", "Verification session was not found.");
        }

        if (session.ClientApplicationId != caller.ClientApplicationId)
        {
            await auditEvents.AppendAsync(CreateAuditEvent(caller, session, "SESSION_ACCESS_DENIED", session.RequestId, session.CorrelationId), cancellationToken);
            return Forbidden<CompleteVerificationSessionResponseDto>("FORBIDDEN_CLIENT_APPLICATION", "The session belongs to another client application.");
        }

        if (session.State == VerificationSessionState.Completed)
        {
            return await BuildCompletedResponseAsync(session, cancellationToken);
        }

        var operationNow = DateTimeOffset.UtcNow;
        if (session.State == VerificationSessionState.Expired || session.ExpiresAt <= operationNow)
        {
            return SessionOperationResult<CompleteVerificationSessionResponseDto>.Failure(
                "SESSION_EXPIRED",
                "Verification session is expired.",
                403);
        }

        if (session.State is VerificationSessionState.Cancelled or VerificationSessionState.TechnicalTerminal)
        {
            return SessionOperationResult<CompleteVerificationSessionResponseDto>.Failure(
                "SESSION_TERMINAL",
                "Verification session is terminal.",
                409);
        }

        var evidence = await evidenceResults.ListBySessionAsync(session.Id, cancellationToken);
        var latestEvidence = LatestEvidenceByRequiredCheck(evidence);
        var missingChecks = session.RequiredChecks
            .Where(check => !latestEvidence.ContainsKey(check))
            .OrderBy(check => check)
            .ToArray();
        if (missingChecks.Length > 0)
        {
            return SessionOperationResult<CompleteVerificationSessionResponseDto>.Failure(
                "REQUIRED_EVIDENCE_MISSING",
                "One or more required checks do not have accepted evidence.",
                409);
        }

        var selectedEvidence = session.RequiredChecks
            .OrderBy(check => check)
            .Select(check => latestEvidence[check])
            .ToArray();

        if (selectedEvidence.Any(candidate => candidate.PayloadHash is null))
        {
            return SessionOperationResult<CompleteVerificationSessionResponseDto>.Failure(
                "INVALID_EVIDENCE_RESULT",
                "Accepted evidence is missing a payload hash.",
                409);
        }

        if (selectedEvidence.Any(candidate => candidate.Result == VerificationResult.NotSupported))
        {
            return SessionOperationResult<CompleteVerificationSessionResponseDto>.Failure(
                "UNSUPPORTED_EVIDENCE_RESULT",
                "The latest evidence result is not supported for finalization.",
                409);
        }

        var effectiveRequestId = FirstNonEmpty(request.RequestId, session.RequestId, $"req-{session.Id:N}");
        var effectiveCorrelationId = FirstNonEmpty(request.CorrelationId, session.CorrelationId, $"corr-{session.Id:N}");
        var completedAt = operationNow;
        var finalResult = CalculateFinalResult(selectedEvidence, request.ForceReview);
        var assuranceLevel = CalculateAssuranceLevel(session, finalResult);
        var completedChecks = selectedEvidence
            .Where(candidate => candidate.Result == VerificationResult.Passed)
            .Select(candidate => MapEvidenceToCheck(candidate.ResultType))
            .OrderBy(check => check)
            .ToArray();
        var failedChecks = selectedEvidence
            .Where(candidate => candidate.Result != VerificationResult.Passed)
            .Select(candidate => MapEvidenceToCheck(candidate.ResultType))
            .OrderBy(check => check)
            .ToArray();
        var decisionReasonCodes = BuildDecisionReasonCodes(selectedEvidence, finalResult, request.ForceReview);
        var retryReasonCodes = selectedEvidence
            .Select(candidate => candidate.RetryReasonCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var decisionSeed = new
        {
            SessionId = session.Id.ToString("N"),
            EvidenceIds = selectedEvidence.Select(candidate => candidate.Id.ToString("N")).ToArray(),
            Result = finalResult.ToString(),
            AssuranceLevel = assuranceLevel.ToString(),
            ForceReview = request.ForceReview,
            RequestId = effectiveRequestId,
            CorrelationId = effectiveCorrelationId,
            CompletedAt = completedAt,
        };
        var decisionId = DeterministicGuid("tip-06-decision", decisionSeed);
        var packageId = DeterministicGuid("tip-06-evidence-package", new { SessionId = session.Id.ToString("N"), DecisionId = decisionId.ToString("N") });
        var auditEventId = DeterministicGuid("tip-06-completion-audit", new { SessionId = session.Id.ToString("N"), PackageId = packageId.ToString("N") });
        var artifacts = await captureArtifacts.ListBySessionAsync(session.Id, cancellationToken);
        var manifestEvidenceRefs = BuildManifestEvidenceRefs(selectedEvidence, artifacts);
        var existingAuditRefs = (await auditEvents.ListBySessionAsync(session.Id, cancellationToken))
            .Select(ToManifestAuditRef)
            .ToList();
        var completionAuditPayloadHash = new HashRef(HashCanonical("tip-06-completion-audit-payload", new
        {
            SessionId = session.Id.ToString("N"),
            DecisionId = decisionId.ToString("N"),
            EvidencePackageId = packageId.ToString("N"),
            Result = finalResult.ToString(),
            RequestId = effectiveRequestId,
            CorrelationId = effectiveCorrelationId,
            CompletedAt = completedAt,
        }));
        var completionAuditEvent = new AuditEvent(
            auditEventId,
            caller.ClientApplicationId,
            session.Id,
            caller.CallerCategory.ToString(),
            caller.KeyPrefix,
            "VERIFICATION_COMPLETED",
            completionAuditPayloadHash,
            EventPayloadRef: null,
            effectiveRequestId,
            effectiveCorrelationId,
            completedAt);
        var auditRefs = existingAuditRefs
            .Append(ToManifestAuditRef(completionAuditEvent))
            .OrderBy(candidate => candidate.EventId, StringComparer.Ordinal)
            .ToArray();
        var manifestBody = new
        {
            EvidencePackageId = packageId.ToString("N"),
            VerificationSessionId = session.Id.ToString("N"),
            PackageVersion,
            EvidenceRefs = manifestEvidenceRefs,
            AuditEventRefs = auditRefs,
            ResultRef = decisionId.ToString("N"),
            Result = finalResult.ToString(),
            AssuranceLevel = assuranceLevel.ToString(),
            RequestId = effectiveRequestId,
            CorrelationId = effectiveCorrelationId,
            CreatedAt = completedAt,
        };
        var manifestBodyHash = HashCanonical("tip-06-manifest-body", manifestBody);
        var packageHash = new HashRef(HashCanonical("tip-06-evidence-package", new
        {
            EvidencePackageId = packageId.ToString("N"),
            VerificationSessionId = session.Id.ToString("N"),
            PackageVersion,
            ManifestBodyHash = manifestBodyHash,
            ResultRef = decisionId.ToString("N"),
            EvidenceRefs = selectedEvidence.Select(candidate => candidate.Id.ToString("N")).ToArray(),
            Result = finalResult.ToString(),
            AssuranceLevel = assuranceLevel.ToString(),
            CreatedAt = completedAt,
        }));
        var manifestHash = new HashRef(HashCanonical("tip-06-evidence-manifest", new
        {
            BodyHash = manifestBodyHash,
            PackageHash = packageHash.ToString(),
        }));
        var decision = new VerificationDecision(
            decisionId,
            session.Id,
            finalResult,
            assuranceLevel,
            RiskScore: null,
            failedChecks,
            completedChecks,
            decisionReasonCodes,
            retryReasonCodes,
            completedAt);
        var evidencePackage = new EvidencePackage(
            packageId,
            session.Id,
            PackageVersion,
            manifestHash,
            selectedEvidence.Select(candidate => candidate.Id.ToString("N")).ToArray(),
            auditRefs.Select(candidate => candidate.EventId).ToArray(),
            decision.Id,
            packageHash,
            SignaturePlaceholderStatus.PlaceholderUnverified,
            completedAt);
        var manifest = new EvidenceManifestDto(
            packageId.ToString("N"),
            session.Id.ToString("N"),
            PackageVersion,
            manifestHash.ToString(),
            packageHash.ToString(),
            manifestEvidenceRefs,
            auditRefs,
            decision.Id.ToString("N"),
            SignaturePlaceholderStatusDto.PlaceholderUnverified,
            completedAt);
        var completedSession = session.WithCompletion(
            finalResult,
            assuranceLevel,
            decision.Id,
            evidencePackage.Id,
            evidencePackage.PackageHash,
            evidencePackage.ManifestHash,
            effectiveRequestId,
            effectiveCorrelationId,
            completedAt);
        var writeResult = await finalizationBoundary.TryFinalizeAsync(
            new VerificationFinalizationWrite(
                session,
                completedSession,
                decision,
                evidencePackage,
                manifest,
                completionAuditEvent),
            cancellationToken);

        return writeResult.Status switch
        {
            VerificationFinalizationWriteStatus.Applied =>
                SessionOperationResult<CompleteVerificationSessionResponseDto>.Success(ToCompleteResponse(completedSession)),
            VerificationFinalizationWriteStatus.AlreadyCompleted when writeResult.Session is not null =>
                SessionOperationResult<CompleteVerificationSessionResponseDto>.Success(ToCompleteResponse(writeResult.Session)),
            VerificationFinalizationWriteStatus.NotFound =>
                NotFound<CompleteVerificationSessionResponseDto>("SESSION_NOT_FOUND", "Verification session was not found."),
            _ => SessionOperationResult<CompleteVerificationSessionResponseDto>.Failure(
                "FINALIZATION_CONFLICT",
                "Verification session changed before finalization completed.",
                409),
        };
    }

    public async Task<SessionOperationResult<EvidencePackageSummaryDto>> GetEvidencePackageAsync(
        AuthenticatedClientContext caller,
        string evidencePackageId,
        CancellationToken cancellationToken = default)
    {
        var callerError = ApplicationAuthorization.RequireBusinessReadEndpoint<EvidencePackageSummaryDto>(caller);
        if (callerError is not null)
        {
            return callerError;
        }

        if (!Guid.TryParse(evidencePackageId, out var packageId))
        {
            return NotFound<EvidencePackageSummaryDto>("EVIDENCE_PACKAGE_NOT_FOUND", "Evidence package was not found.");
        }

        var package = await evidencePackages.GetAsync(packageId, cancellationToken);
        if (package is null)
        {
            return NotFound<EvidencePackageSummaryDto>("EVIDENCE_PACKAGE_NOT_FOUND", "Evidence package was not found.");
        }

        var session = await sessions.GetAsync(package.VerificationSessionId, cancellationToken);
        if (session is null)
        {
            return NotFound<EvidencePackageSummaryDto>("SESSION_NOT_FOUND", "Verification session was not found.");
        }

        if (session.ClientApplicationId != caller.ClientApplicationId)
        {
            await auditEvents.AppendAsync(CreateAuditEvent(caller, session, "EVIDENCE_PACKAGE_ACCESS_DENIED", session.RequestId, session.CorrelationId), cancellationToken);
            return Forbidden<EvidencePackageSummaryDto>("FORBIDDEN_CLIENT_APPLICATION", "The evidence package belongs to another client application.");
        }

        var decision = session.FinalDecisionId is null
            ? null
            : await decisions.GetAsync(session.FinalDecisionId.Value, cancellationToken);
        if (decision is null)
        {
            return SessionOperationResult<EvidencePackageSummaryDto>.Failure(
                "FINAL_DECISION_NOT_FOUND",
                "Evidence package final decision was not found.",
                409);
        }

        var manifest = await manifests.GetByPackageAsync(package.Id, cancellationToken);
        if (manifest is null)
        {
            return SessionOperationResult<EvidencePackageSummaryDto>.Failure(
                "EVIDENCE_MANIFEST_NOT_FOUND",
                "Evidence package manifest was not found.",
                409);
        }

        return SessionOperationResult<EvidencePackageSummaryDto>.Success(ToPackageSummary(package, session, decision, manifest));
    }

    public async Task<SessionOperationResult<VerificationCompletedEventDto>> GetCompletionNotificationAsync(
        AuthenticatedClientContext caller,
        string verificationSessionId,
        CancellationToken cancellationToken = default)
    {
        var callerError = ApplicationAuthorization.RequireBusinessReadEndpoint<VerificationCompletedEventDto>(caller);
        if (callerError is not null)
        {
            return callerError;
        }

        if (!Guid.TryParse(verificationSessionId, out var sessionId))
        {
            return NotFound<VerificationCompletedEventDto>("SESSION_NOT_FOUND", "Verification session was not found.");
        }

        var session = await sessions.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return NotFound<VerificationCompletedEventDto>("SESSION_NOT_FOUND", "Verification session was not found.");
        }

        if (session.ClientApplicationId != caller.ClientApplicationId)
        {
            return Forbidden<VerificationCompletedEventDto>("FORBIDDEN_CLIENT_APPLICATION", "The completion notification belongs to another client application.");
        }

        if (session.State != VerificationSessionState.Completed)
        {
            return SessionOperationResult<VerificationCompletedEventDto>.Failure(
                "SESSION_NOT_COMPLETED",
                "Verification session is not completed.",
                409);
        }

        if (session.FinalDecisionId is null ||
            session.EvidencePackageId is null ||
            session.EvidencePackageHash is null ||
            session.ManifestHash is null ||
            session.CompletedAt is null ||
            string.IsNullOrWhiteSpace(session.RequestId) ||
            string.IsNullOrWhiteSpace(session.CorrelationId))
        {
            return CompletionNotificationInvariantFailure();
        }

        var package = await evidencePackages.GetAsync(session.EvidencePackageId.Value, cancellationToken);
        if (package is null ||
            package.VerificationSessionId != session.Id ||
            package.Id != session.EvidencePackageId.Value ||
            package.PackageHash != session.EvidencePackageHash ||
            package.ManifestHash != session.ManifestHash)
        {
            return CompletionNotificationInvariantFailure();
        }

        return SessionOperationResult<VerificationCompletedEventDto>.Success(ToCompletionNotification(session, package));
    }

    private async Task<SessionOperationResult<CompleteVerificationSessionResponseDto>> BuildCompletedResponseAsync(
        VerificationSession session,
        CancellationToken cancellationToken)
    {
        if (session.FinalDecisionId is null ||
            session.EvidencePackageId is null ||
            session.EvidencePackageHash is null ||
            session.ManifestHash is null ||
            session.CompletedAt is null ||
            string.IsNullOrWhiteSpace(session.RequestId) ||
            string.IsNullOrWhiteSpace(session.CorrelationId))
        {
            return SessionOperationResult<CompleteVerificationSessionResponseDto>.Failure(
                "COMPLETION_SNAPSHOT_INCOMPLETE",
                "Completed session is missing finalization fields.",
                409);
        }

        var package = await evidencePackages.GetAsync(session.EvidencePackageId.Value, cancellationToken);
        if (package is null)
        {
            return SessionOperationResult<CompleteVerificationSessionResponseDto>.Failure(
                "EVIDENCE_PACKAGE_NOT_FOUND",
                "Evidence package was not found.",
                409);
        }

        return SessionOperationResult<CompleteVerificationSessionResponseDto>.Success(ToCompleteResponse(session, package));
    }

    private static IReadOnlyDictionary<RequiredCheckType, EvidenceResult> LatestEvidenceByRequiredCheck(IEnumerable<EvidenceResult> evidence) =>
        evidence
            .OrderBy(candidate => candidate.CreatedAt)
            .ThenBy(candidate => candidate.Id)
            .Select(candidate => new
            {
                Check = MapEvidenceToCheck(candidate.ResultType),
                Evidence = candidate,
            })
            .GroupBy(candidate => candidate.Check)
            .ToDictionary(group => group.Key, group => group.Last().Evidence);

    private static VerificationResult CalculateFinalResult(IReadOnlyList<EvidenceResult> evidence, bool forceReview)
    {
        if (forceReview || evidence.Any(candidate => candidate.Result == VerificationResult.ReviewRequired))
        {
            return VerificationResult.ReviewRequired;
        }

        if (evidence.Any(candidate => candidate.Result == VerificationResult.TechnicalError))
        {
            return VerificationResult.TechnicalError;
        }

        if (evidence.Any(candidate => candidate.Result == VerificationResult.RetryRequired))
        {
            return VerificationResult.RetryRequired;
        }

        if (evidence.Any(candidate => candidate.Result == VerificationResult.FailedCaptureQuality))
        {
            return VerificationResult.FailedCaptureQuality;
        }

        if (evidence.Any(candidate => candidate.Result == VerificationResult.FailedIdentity))
        {
            return VerificationResult.FailedIdentity;
        }

        return VerificationResult.Passed;
    }

    private static AssuranceLevel CalculateAssuranceLevel(VerificationSession session, VerificationResult result)
    {
        if (result == VerificationResult.TechnicalError)
        {
            return AssuranceLevel.Unknown;
        }

        if (result != VerificationResult.Passed)
        {
            return AssuranceLevel.Low;
        }

        return session.RequiredChecks.Contains(RequiredCheckType.DocumentNfc) &&
               session.RequiredChecks.Contains(RequiredCheckType.FaceMatch) &&
               session.RequiredChecks.Contains(RequiredCheckType.Liveness)
            ? AssuranceLevel.High
            : AssuranceLevel.Medium;
    }

    private static IReadOnlyList<string> BuildDecisionReasonCodes(
        IReadOnlyList<EvidenceResult> evidence,
        VerificationResult finalResult,
        bool forceReview)
    {
        var reasonCodes = evidence
            .SelectMany(candidate => candidate.ReasonCodes)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToList();

        if (forceReview)
        {
            reasonCodes.Add("FORCED_REVIEW");
        }

        if (reasonCodes.Count == 0)
        {
            reasonCodes.Add(finalResult == VerificationResult.Passed
                ? "ALL_REQUIRED_CHECKS_PASSED"
                : $"FINAL_{finalResult.ToString().ToUpperInvariant()}");
        }

        return reasonCodes
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<ManifestEvidenceRefDto> BuildManifestEvidenceRefs(
        IReadOnlyList<EvidenceResult> selectedEvidence,
        IReadOnlyList<CaptureArtifact> artifacts)
    {
        var artifactsById = artifacts.ToDictionary(candidate => candidate.Id);
        return selectedEvidence
            .OrderBy(candidate => candidate.ResultType)
            .ThenBy(candidate => candidate.Id)
            .Select(candidate => new ManifestEvidenceRefDto(
                candidate.ResultType.ToString(),
                candidate.Id.ToString("N"),
                VaultRef: null,
                ArtifactHash: AggregateArtifactHash(candidate, artifactsById),
                PayloadHash: candidate.PayloadHash?.ToString()))
            .ToArray();
    }

    private static string? AggregateArtifactHash(
        EvidenceResult evidence,
        IReadOnlyDictionary<Guid, CaptureArtifact> artifactsById)
    {
        var hashes = evidence.InputCaptureArtifactIds
            .Select(id => artifactsById.TryGetValue(id, out var artifact)
                ? artifact.ArtifactHash?.ToString() ?? artifact.MetadataHash?.ToString()
                : null)
            .Where(hash => !string.IsNullOrWhiteSpace(hash))
            .Select(hash => hash!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return hashes.Length switch
        {
            0 => null,
            1 => hashes[0],
            _ => HashCanonical("tip-06-artifact-hash-set", hashes),
        };
    }

    private static EvidencePackageSummaryDto ToPackageSummary(
        EvidencePackage package,
        VerificationSession session,
        VerificationDecision decision,
        EvidenceManifestDto manifest) =>
        new(
            package.Id.ToString("N"),
            session.Id.ToString("N"),
            package.PackageVersion,
            package.PackageHash.ToString(),
            package.ManifestHash.ToString(),
            ToDto(decision.Result),
            ToDto(decision.AssuranceLevel),
            manifest.EvidenceRefs.Select(ToPublicEvidenceRef).ToArray(),
            ToDto(package.EvidencePackageSignatureStatus),
            session.RequestId,
            session.CorrelationId,
            package.CreatedAt);

    private static EvidenceRefSummaryDto ToPublicEvidenceRef(ManifestEvidenceRefDto evidenceRef) =>
        new(
            evidenceRef.Type,
            evidenceRef.Id,
            evidenceRef.Type,
            evidenceRef.Id,
            evidenceRef.ArtifactHash);

    private static CompleteVerificationSessionResponseDto ToCompleteResponse(VerificationSession session) =>
        new(
            session.Id.ToString("N"),
            ToDto(session.State),
            ToDto(session.Result),
            ToDto(session.AssuranceLevel),
            session.FinalDecisionId!.Value.ToString("N"),
            session.EvidencePackageId!.Value.ToString("N"),
            session.EvidencePackageHash!.Value.ToString(),
            session.ManifestHash!.Value.ToString(),
            session.RequestId,
            session.CorrelationId,
            session.CompletedAt!.Value,
            SignaturePlaceholderStatusDto.PlaceholderUnverified);

    private static CompleteVerificationSessionResponseDto ToCompleteResponse(
        VerificationSession session,
        EvidencePackage package) =>
        ToCompleteResponse(session) with
        {
            EvidencePackageSignatureStatus = ToDto(package.EvidencePackageSignatureStatus),
        };

    private static VerificationCompletedEventDto ToCompletionNotification(
        VerificationSession session,
        EvidencePackage package) =>
        new(
            "VERIFICATION_COMPLETED",
            "localdev-not-dispatched",
            session.CompletedAt!.Value,
            session.Id.ToString("N"),
            session.ClientApplicationId.ToString("N"),
            ToDto(session.Profile),
            session.ExternalSessionId,
            ToDto(session.Result),
            ToDto(session.AssuranceLevel),
            package.Id.ToString("N"),
            package.PackageHash.ToString(),
            package.ManifestHash.ToString(),
            session.RequestId,
            session.CorrelationId,
            session.CompletedAt.Value,
            SignaturePlaceholderStatusDto.PlaceholderUnverified,
            ToDto(package.EvidencePackageSignatureStatus));

    private static ManifestAuditRefDto ToManifestAuditRef(AuditEvent auditEvent) =>
        new(
            auditEvent.Id.ToString("N"),
            auditEvent.EventType,
            auditEvent.EventPayloadHash.ToString());

    private static AuditEvent CreateAuditEvent(
        AuthenticatedClientContext caller,
        VerificationSession session,
        string eventType,
        string requestId,
        string correlationId) =>
        new(
            Guid.NewGuid(),
            caller.ClientApplicationId,
            session.Id,
            caller.CallerCategory.ToString(),
            caller.KeyPrefix,
            eventType,
            new HashRef($"sha256:localdev-{eventType.ToLowerInvariant()}"),
            EventPayloadRef: null,
            requestId,
            correlationId,
            DateTimeOffset.UtcNow);

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

    private static VerificationSessionStateDto ToDto(VerificationSessionState state) =>
        state switch
        {
            VerificationSessionState.Created => VerificationSessionStateDto.Created,
            VerificationSessionState.InProgress => VerificationSessionStateDto.InProgress,
            VerificationSessionState.ReadyToComplete => VerificationSessionStateDto.ReadyToComplete,
            VerificationSessionState.Completed => VerificationSessionStateDto.Completed,
            VerificationSessionState.Expired => VerificationSessionStateDto.Expired,
            VerificationSessionState.Cancelled => VerificationSessionStateDto.Cancelled,
            VerificationSessionState.TechnicalTerminal => VerificationSessionStateDto.TechnicalTerminal,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };

    private static VerificationProfileDto ToDto(VerificationProfile profile) =>
        profile switch
        {
            VerificationProfile.StandardEkycProfile => VerificationProfileDto.StandardEkycProfile,
            VerificationProfile.TransactionBoundEkycProfile => VerificationProfileDto.TransactionBoundEkycProfile,
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null),
        };

    private static VerificationResultDto ToDto(VerificationResult result) =>
        result switch
        {
            VerificationResult.NotAvailable => VerificationResultDto.NotAvailable,
            VerificationResult.Passed => VerificationResultDto.Passed,
            VerificationResult.RetryRequired => VerificationResultDto.RetryRequired,
            VerificationResult.FailedCaptureQuality => VerificationResultDto.FailedCaptureQuality,
            VerificationResult.FailedIdentity => VerificationResultDto.FailedIdentity,
            VerificationResult.ReviewRequired => VerificationResultDto.ReviewRequired,
            VerificationResult.TechnicalError => VerificationResultDto.TechnicalError,
            VerificationResult.NotSupported => VerificationResultDto.NotSupported,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
        };

    private static AssuranceLevelDto ToDto(AssuranceLevel assuranceLevel) =>
        assuranceLevel switch
        {
            AssuranceLevel.None => AssuranceLevelDto.None,
            AssuranceLevel.Low => AssuranceLevelDto.Low,
            AssuranceLevel.Medium => AssuranceLevelDto.Medium,
            AssuranceLevel.High => AssuranceLevelDto.High,
            AssuranceLevel.Unknown => AssuranceLevelDto.Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(assuranceLevel), assuranceLevel, null),
        };

    private static SignaturePlaceholderStatusDto ToDto(SignaturePlaceholderStatus status) =>
        status switch
        {
            SignaturePlaceholderStatus.PlaceholderUnverified => SignaturePlaceholderStatusDto.PlaceholderUnverified,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    private static SessionOperationResult<T> Forbidden<T>(string code, string message) =>
        SessionOperationResult<T>.Failure(code, message, 403);

    private static SessionOperationResult<T> NotFound<T>(string code, string message) =>
        SessionOperationResult<T>.Failure(code, message, 404);

    private static SessionOperationResult<VerificationCompletedEventDto> CompletionNotificationInvariantFailure() =>
        SessionOperationResult<VerificationCompletedEventDto>.Failure(
            "COMPLETION_NOTIFICATION_INVARIANT_FAILED",
            "Completed session is missing notification projection fields.",
            500);

    private static string FirstNonEmpty(params string?[] values) =>
        values.First(value => !string.IsNullOrWhiteSpace(value))!;

    private static string HashCanonical(string label, object value)
    {
        var json = JsonSerializer.Serialize(value, CanonicalJsonOptions);
        var input = Encoding.UTF8.GetBytes($"{label}\n{json}");
        return $"sha256:{Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant()}";
    }

    private static Guid DeterministicGuid(string label, object value)
    {
        var json = JsonSerializer.Serialize(value, CanonicalJsonOptions);
        var input = Encoding.UTF8.GetBytes($"{label}\n{json}");
        Span<byte> bytes = stackalloc byte[16];
        SHA256.HashData(input).AsSpan(0, 16).CopyTo(bytes);
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }
}
