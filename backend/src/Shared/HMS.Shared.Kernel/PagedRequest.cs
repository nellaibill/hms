namespace HMS.Shared.Kernel;

/// <summary>
/// Standard pagination/sort/search query shape every module's list endpoints accept,
/// per docs/ApiStandards.md §6. Enforces the page-size ceiling server-side so a
/// caller can never request an unbounded page.
/// </summary>
public class PagedRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value,
        };
    }

    public string? Sort { get; set; }

    public string? Search { get; set; }
}
