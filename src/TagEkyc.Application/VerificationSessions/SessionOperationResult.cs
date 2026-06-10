namespace TagEkyc.Application.VerificationSessions;

public sealed record SessionOperationError(string Code, string Message, int StatusCode);

public sealed record SessionOperationResult<T>
{
    private SessionOperationResult(T? value, SessionOperationError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public SessionOperationError? Error { get; }
    public bool IsSuccess => Error is null;

    public static SessionOperationResult<T> Success(T value) => new(value, null);

    public static SessionOperationResult<T> Failure(string code, string message, int statusCode) =>
        new(default, new SessionOperationError(code, message, statusCode));
}

