using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateCurrencyRequest
{
    public string CurrencyCode { get; init; } = string.Empty;
    public string CurrencyName { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public int DecimalPlaces { get; init; } = 2;
    public bool IsActive { get; init; } = true;
}

public record UpdateCurrencyRequest
{
    public string CurrencyName { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public int DecimalPlaces { get; init; } = 2;
    public bool IsActive { get; init; } = true;
}

public record CurrencyResponse
{
    public Guid Id { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string CurrencyName { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public int DecimalPlaces { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>Query parameters for GET /api/v1/masters/currencies — pagination/sort/search come from <see cref="PagedRequest"/>.</summary>
public class CurrencyListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
