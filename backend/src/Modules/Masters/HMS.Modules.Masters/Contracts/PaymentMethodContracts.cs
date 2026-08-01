using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreatePaymentMethodRequest
{
    public string MethodCode { get; init; } = string.Empty;
    public string MethodName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdatePaymentMethodRequest
{
    public string MethodName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record PaymentMethodResponse
{
    public Guid Id { get; init; }
    public string MethodCode { get; init; } = string.Empty;
    public string MethodName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class PaymentMethodListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
