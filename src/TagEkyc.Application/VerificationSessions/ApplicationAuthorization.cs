namespace TagEkyc.Application.VerificationSessions;

internal static class ApplicationAuthorization
{
    public const string BusinessSessionCreateScope = "business.session.create";
    public const string BusinessSessionReadScope = "business.session.read";
    public const string SessionCompleteScope = "session.complete";
    public const string SessionCancelScope = "session.cancel";
    public const string CaptureArtifactAppendScope = "capture.artifact.append";
    public const string TrustedEvidenceAppendScope = "trusted.evidence.append";

    public static SessionOperationResult<T>? RequireBusinessSessionCreate<T>(AuthenticatedClientContext caller) =>
        RequireScopeThenCategory<T>(
            caller,
            AuthenticatedCallerCategory.BusinessConsumer,
            BusinessSessionCreateScope,
            "The API key is not scoped for session creation.");

    public static SessionOperationResult<T>? RequireBusinessSessionRead<T>(AuthenticatedClientContext caller) =>
        RequireScopeThenCategory<T>(
            caller,
            AuthenticatedCallerCategory.BusinessConsumer,
            BusinessSessionReadScope,
            "The API key is not scoped for session reads.");

    public static SessionOperationResult<T>? RequireBusinessCompletion<T>(AuthenticatedClientContext caller) =>
        RequireCategoryThenScope<T>(
            caller,
            AuthenticatedCallerCategory.BusinessConsumer,
            SessionCompleteScope,
            "The API key is not scoped for this endpoint.");

    public static SessionOperationResult<T>? RequireBusinessCancellation<T>(AuthenticatedClientContext caller) =>
        RequireCategoryThenScope<T>(
            caller,
            AuthenticatedCallerCategory.BusinessConsumer,
            SessionCancelScope,
            "The API key is not scoped for this endpoint.");

    public static SessionOperationResult<T>? RequireBusinessReadEndpoint<T>(AuthenticatedClientContext caller) =>
        RequireCategoryThenScope<T>(
            caller,
            AuthenticatedCallerCategory.BusinessConsumer,
            BusinessSessionReadScope,
            "The API key is not scoped for this endpoint.");

    public static SessionOperationResult<T>? RequireCaptureArtifactAppend<T>(AuthenticatedClientContext caller) =>
        RequireCategoryThenScope<T>(
            caller,
            AuthenticatedCallerCategory.CaptureAgent,
            CaptureArtifactAppendScope,
            "The API key is not scoped for this endpoint.");

    public static SessionOperationResult<T>? RequireTrustedEvidenceAppend<T>(AuthenticatedClientContext caller) =>
        RequireCategoryThenScope<T>(
            caller,
            AuthenticatedCallerCategory.TrustedAdapter,
            TrustedEvidenceAppendScope,
            "The API key is not scoped for this endpoint.");

    public static bool CanAccessClientApplication(
        AuthenticatedClientContext caller,
        Guid clientApplicationId) =>
        caller.ClientApplicationId == clientApplicationId ||
        caller.AllowedClientApplicationIds?.Contains(clientApplicationId) == true;

    private static SessionOperationResult<T>? RequireScopeThenCategory<T>(
        AuthenticatedClientContext caller,
        AuthenticatedCallerCategory expectedCategory,
        string requiredScope,
        string missingScopeMessage)
    {
        if (!caller.Scopes.Contains(requiredScope))
        {
            return Forbidden<T>("MISSING_SCOPE", missingScopeMessage);
        }

        if (caller.CallerCategory != expectedCategory)
        {
            return CallerCategoryNotAllowed<T>();
        }

        return null;
    }

    private static SessionOperationResult<T>? RequireCategoryThenScope<T>(
        AuthenticatedClientContext caller,
        AuthenticatedCallerCategory expectedCategory,
        string requiredScope,
        string missingScopeMessage)
    {
        if (caller.CallerCategory != expectedCategory)
        {
            return CallerCategoryNotAllowed<T>();
        }

        if (!caller.Scopes.Contains(requiredScope))
        {
            return Forbidden<T>("MISSING_SCOPE", missingScopeMessage);
        }

        return null;
    }

    private static SessionOperationResult<T> CallerCategoryNotAllowed<T>() =>
        Forbidden<T>(
            "CALLER_CATEGORY_NOT_ALLOWED",
            "The API key caller category is not allowed for this endpoint.");

    private static SessionOperationResult<T> Forbidden<T>(string code, string message) =>
        SessionOperationResult<T>.Failure(code, message, 403);
}
