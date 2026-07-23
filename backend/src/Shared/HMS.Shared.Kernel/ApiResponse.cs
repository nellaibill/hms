namespace HMS.Shared.Kernel;

/// <summary>
/// Standard success envelope for every module's controllers, per docs/ApiStandards.md §4.
/// Nothing here is mandatory except <see cref="Data"/> — an endpoint with nothing extra
/// to report simply omits Meta/Messages rather than sending empty placeholders.
/// </summary>
public class ApiResponse<T>
{
    public T? Data { get; init; }
    public object? Meta { get; init; }
    public IReadOnlyList<string>? Messages { get; init; }
}

/// <summary>
/// Pagination metadata nested under <see cref="ApiResponse{T}.Meta"/> for list endpoints.
/// </summary>
public class PaginationMeta
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

/// <summary>
/// Standard error envelope produced by the global exception handler and by every
/// controller's failure paths, per docs/ApiStandards.md §5.
/// </summary>
public class ApiErrorResponse
{
    public string ErrorCode { get; init; } = default!;
    public string Message { get; init; } = default!;
    public IReadOnlyList<ValidationErrorItem>? ValidationErrors { get; init; }
    public string CorrelationId { get; init; } = default!;
    public DateTime Timestamp { get; init; }
}

public class ValidationErrorItem
{
    public string Field { get; init; } = default!;
    public string Message { get; init; } = default!;
}
