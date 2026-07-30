using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateCustomerRequest
{
    public string CustomerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Country { get; init; }
    public Guid? PaymentTermId { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateCustomerRequest
{
    public string CustomerName { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Country { get; init; }
    public Guid? PaymentTermId { get; init; }
    public bool IsActive { get; init; } = true;
}

public record CustomerResponse
{
    public Guid Id { get; init; }
    public string CustomerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Country { get; init; }
    public Guid? PaymentTermId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class CustomerListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
