namespace TagEkyc.Application.VerificationSessions;

public sealed record SessionOperationError(string Code, string Message, int StatusCode);

public sealed record SessionOperationResult<T>
{
    private SessionOperationResult(T? value, SessionOperationError? error, bool isReplay = false)
    {
        Value = value;
        Error = error;
        IsReplay = isReplay;
    }

    public T? Value { get; }
    public SessionOperationError? Error { get; }
    public bool IsSuccess => Error is null;
    // Internal execution evidence; not part of the public response DTO.
    public bool IsReplay { get; }

    public static SessionOperationResult<T> Success(T value, bool isReplay = false) => new(value, null, isReplay);

    public static SessionOperationResult<T> Failure(string code, string message, int statusCode) =>
        new(default, new SessionOperationError(code, message, statusCode));
}
