using TagEkyc.Application.Ports;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.Common;
using TagEkyc.Domain;

namespace TagEkyc.Application.VerificationSessions;

public sealed class VerificationSessionApplicationService(
    IVerificationSessionRepository sessions,
    IAuditEventRepository auditEvents,
    ILocalDevClientPolicyProvider policies)
    : IVerificationSessionCommands, IVerificationSessionQueries
{
    private const string CreateScope = "business.session.create";
    private const string ReadScope = "business.session.read";

    public async Task<SessionOperationResult<CreateVerificationSessionResponseDto>> CreateAsync(
        AuthenticatedClientContext caller,
        CreateVerificationSessionRequestDto request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        if (!caller.Scopes.Contains(CreateScope))
        {
            return Forbidden<CreateVerificationSessionResponseDto>("MISSING_SCOPE", "The API key is not scoped for session creation.");
        }

        var policy = await policies.GetPolicyAsync(caller.ClientApplicationId, cancellationToken);
        var policyError = ValidatePolicy<CreateVerificationSessionResponseDto>(policy, request.Profile, request.Purpose, caller.Scopes);
        if (policyError is not null)
        {
            return policyError;
        }

        var shapeError = ValidateRequestShape(request);
        if (shapeError is not null)
        {
            return SessionOperationResult<CreateVerificationSessionResponseDto>.Failure(shapeError.Value.Code, shapeError.Value.Message, 400);
        }

        var requiredChecks = new HashSet<RequiredCheckType>();
        var seenChecks = new HashSet<RequiredCheckTypeDto>();
        foreach (var check in request.RequiredChecks)
        {
            if (!check.Required)
            {
                return InvalidRequiredChecks<CreateVerificationSessionResponseDto>("RequiredChecks entries must be required in TIP-04.");
            }

            if (!seenChecks.Add(check.CheckType))
            {
                return InvalidRequiredChecks<CreateVerificationSessionResponseDto>("Duplicate RequiredChecks check types are not allowed.");
            }

            requiredChecks.Add(ToDomain(check.CheckType));
        }

        if (!requiredChecks.All(policy!.AllowedRequiredChecks.Contains))
        {
            return InvalidRequiredChecks<CreateVerificationSessionResponseDto>(
                "Requested checks are not allowed for this client application.",
                403);
        }

        var profile = ToDomain(request.Profile);
        if (profile == VerificationProfile.TransactionBoundEkycProfile && !policy.AllowsTransactionBoundProfile)
        {
            return Forbidden<CreateVerificationSessionResponseDto>("TRANSACTION_BOUND_NOT_ALLOWED", "Transaction-bound sessions are not allowed for this client application.");
        }

        HashRef? bindingNonceHash = null;
        if (profile == VerificationProfile.TransactionBoundEkycProfile)
        {
            if (string.IsNullOrWhiteSpace(request.ExternalTransactionId) || string.IsNullOrWhiteSpace(request.BindingNonceHash))
            {
                return SessionOperationResult<CreateVerificationSessionResponseDto>.Failure(
                    "MISSING_TRANSACTION_BINDING",
                    "Transaction-bound sessions require externalTransactionId and bindingNonceHash.",
                    400);
            }

            if (!TryCreateHashRef(request.BindingNonceHash, out var parsedBindingNonceHash))
            {
                return SessionOperationResult<CreateVerificationSessionResponseDto>.Failure(
                    "INVALID_BINDING_NONCE_HASH",
                    "bindingNonceHash must use the sha256: prefix.",
                    400);
            }

            bindingNonceHash = parsedBindingNonceHash;
        }

        if (!string.IsNullOrWhiteSpace(request.ExternalSessionId))
        {
            var duplicate = await sessions.GetByExternalSessionIdAsync(
                caller.ClientApplicationId,
                request.ExternalSessionId,
                cancellationToken);

            if (duplicate is not null)
            {
                return SessionOperationResult<CreateVerificationSessionResponseDto>.Failure(
                    "DUPLICATE_EXTERNAL_SESSION",
                    "externalSessionId already exists for this client application.",
                    409);
            }
        }

        var now = DateTimeOffset.UtcNow;
        if (request.ExpiresAt <= now)
        {
            return SessionOperationResult<CreateVerificationSessionResponseDto>.Failure(
                "VALIDATION_ERROR",
                "expiresAt must be in the future.",
                400);
        }

        if (policy.MaxSessionTtl is not null && request.ExpiresAt > now.Add(policy.MaxSessionTtl.Value))
        {
            return SessionOperationResult<CreateVerificationSessionResponseDto>.Failure(
                "VALIDATION_ERROR",
                "expiresAt exceeds the local development policy maximum.",
                400);
        }

        var session = VerificationSession.Create(
            caller.ClientApplicationId,
            request.SubjectRef,
            profile,
            request.Purpose,
            requiredChecks,
            request.ExpiresAt,
            now,
            request.ExternalSessionId,
            request.ExternalTransactionId,
            bindingNonceHash,
            request.RequestId,
            request.CorrelationId);

        await sessions.AddAsync(session, cancellationToken);
        await auditEvents.AppendAsync(CreateAuditEvent(caller, session, "SESSION_CREATED"), cancellationToken);

        return SessionOperationResult<CreateVerificationSessionResponseDto>.Success(new CreateVerificationSessionResponseDto(
            FormatId(session.Id),
            ToDto(session.Profile),
            ToDto(session.State),
            ToDto(session.Result),
            session.RequestId,
            session.CorrelationId,
            session.ExpiresAt));
    }

    public async Task<SessionOperationResult<VerificationSessionSummaryDto>> GetSummaryAsync(
        AuthenticatedClientContext caller,
        string verificationSessionId,
        CancellationToken cancellationToken = default)
    {
        if (!caller.Scopes.Contains(ReadScope))
        {
            return Forbidden<VerificationSessionSummaryDto>("MISSING_SCOPE", "The API key is not scoped for session reads.");
        }

        if (!Guid.TryParse(verificationSessionId, out var sessionId))
        {
            return SessionOperationResult<VerificationSessionSummaryDto>.Failure(
                "SESSION_NOT_FOUND",
                "Verification session was not found.",
                404);
        }

        var session = await sessions.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return SessionOperationResult<VerificationSessionSummaryDto>.Failure(
                "SESSION_NOT_FOUND",
                "Verification session was not found.",
                404);
        }

        if (session.ClientApplicationId != caller.ClientApplicationId)
        {
            await auditEvents.AppendAsync(CreateAuditEvent(caller, session, "SESSION_ACCESS_DENIED"), cancellationToken);
            return Forbidden<VerificationSessionSummaryDto>("FORBIDDEN_CLIENT_APPLICATION", "The session belongs to another client application.");
        }

        return SessionOperationResult<VerificationSessionSummaryDto>.Success(new VerificationSessionSummaryDto(
            FormatId(session.Id),
            ToDto(session.Profile),
            session.ExternalSessionId,
            session.Purpose,
            ToDto(session.State),
            ToDto(session.Result),
            ToDto(session.AssuranceLevel),
            session.EvidencePackageId?.ToString("N"),
            session.EvidencePackageHash?.ToString(),
            session.ManifestHash?.ToString(),
            session.RequestId,
            session.CorrelationId,
            session.CompletedAt));
    }

    private static (string Code, string Message)? ValidateRequestShape(CreateVerificationSessionRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.SubjectRef))
        {
            return ("VALIDATION_ERROR", "subjectRef is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Purpose))
        {
            return ("VALIDATION_ERROR", "purpose is required.");
        }

        if (request.RequiredChecks is null || request.RequiredChecks.Count == 0)
        {
            return ("INVALID_REQUIRED_CHECKS", "At least one RequiredChecks entry is required.");
        }

        return null;
    }

    private static SessionOperationResult<T>? ValidatePolicy<T>(
        LocalDevClientPolicy? policy,
        VerificationProfileDto profile,
        string purpose,
        IReadOnlySet<string> callerScopes)
    {
        if (policy is null)
        {
            return Forbidden<T>("SESSION_ACCESS_DENIED", "Client policy was not found.");
        }

        if (policy.Status != ClientApplicationStatus.Active)
        {
            return SessionOperationResult<T>.Failure("CLIENT_APPLICATION_DISABLED", "Client application is disabled.", 401);
        }

        if (!policy.AllowedProfiles.Contains(ToDomain(profile)))
        {
            return Forbidden<T>("UNAUTHORIZED_PROFILE", "Profile is not allowed for this client application.");
        }

        if (!policy.AllowedPurposes.Contains(purpose))
        {
            return Forbidden<T>("UNAUTHORIZED_PURPOSE", "Purpose is not allowed for this client application.");
        }

        if (!callerScopes.All(policy.AllowedCallerScopes.Contains))
        {
            return Forbidden<T>("MISSING_SCOPE", "The API key scope is not allowed by client policy.");
        }

        return null;
    }

    private static SessionOperationResult<T> InvalidRequiredChecks<T>(string message, int statusCode = 400) =>
        SessionOperationResult<T>.Failure("INVALID_REQUIRED_CHECKS", message, statusCode);

    private static SessionOperationResult<T> Forbidden<T>(string code, string message) =>
        SessionOperationResult<T>.Failure(code, message, 403);

    private static bool TryCreateHashRef(string value, out HashRef hashRef)
    {
        try
        {
            hashRef = new HashRef(value);
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

    private static VerificationProfile ToDomain(VerificationProfileDto profile) =>
        profile switch
        {
            VerificationProfileDto.StandardEkycProfile => VerificationProfile.StandardEkycProfile,
            VerificationProfileDto.TransactionBoundEkycProfile => VerificationProfile.TransactionBoundEkycProfile,
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null),
        };

    private static VerificationProfileDto ToDto(VerificationProfile profile) =>
        profile switch
        {
            VerificationProfile.StandardEkycProfile => VerificationProfileDto.StandardEkycProfile,
            VerificationProfile.TransactionBoundEkycProfile => VerificationProfileDto.TransactionBoundEkycProfile,
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null),
        };

    private static RequiredCheckType ToDomain(RequiredCheckTypeDto checkType) =>
        checkType switch
        {
            RequiredCheckTypeDto.CaptureQuality => RequiredCheckType.CaptureQuality,
            RequiredCheckTypeDto.DocumentOcr => RequiredCheckType.DocumentOcr,
            RequiredCheckTypeDto.DocumentNfc => RequiredCheckType.DocumentNfc,
            RequiredCheckTypeDto.FaceMatch => RequiredCheckType.FaceMatch,
            RequiredCheckTypeDto.Liveness => RequiredCheckType.Liveness,
            RequiredCheckTypeDto.Fingerprint => RequiredCheckType.Fingerprint,
            RequiredCheckTypeDto.RiskEvaluation => RequiredCheckType.RiskEvaluation,
            _ => throw new ArgumentOutOfRangeException(nameof(checkType), checkType, null),
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

    private static string FormatId(Guid id) => id.ToString("N");
}
