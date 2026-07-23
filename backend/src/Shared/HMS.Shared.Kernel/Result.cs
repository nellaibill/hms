namespace HMS.Shared.Kernel;

/// <summary>
/// Represents the outcome of a use case that can fail for expected business reasons
/// (not found, conflict, business-rule violation). Exceptions remain reserved for
/// truly unexpected failures — see docs/Architecture.md's exception handling strategy.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? Error { get; }

    protected Result(bool isSuccess, string? errorCode, string? error)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        Error = error;
    }

    public static Result Success() => new(true, null, null);

    public static Result Failure(string errorCode, string error) => new(false, errorCode, error);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(T? value, bool isSuccess, string? errorCode, string? error)
        : base(isSuccess, errorCode, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, null, null);

    public static new Result<T> Failure(string errorCode, string error) => new(default, false, errorCode, error);
}
