using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreatePaymentTermRequest
{
    public string TermName { get; init; } = string.Empty;
    public int Days { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdatePaymentTermRequest
{
    public string TermName { get; init; } = string.Empty;
    public int Days { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record PaymentTermResponse
{
    public Guid Id { get; init; }
    public string TermName { get; init; } = string.Empty;
    public int Days { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class PaymentTermListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
