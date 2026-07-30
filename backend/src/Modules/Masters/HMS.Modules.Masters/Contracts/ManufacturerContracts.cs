using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateManufacturerRequest
{
    public string ManufacturerCode { get; init; } = string.Empty;
    public string ManufacturerName { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Country { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateManufacturerRequest
{
    public string ManufacturerName { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Country { get; init; }
    public bool IsActive { get; init; } = true;
}

public record ManufacturerResponse
{
    public Guid Id { get; init; }
    public string ManufacturerCode { get; init; } = string.Empty;
    public string ManufacturerName { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Country { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class ManufacturerListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
