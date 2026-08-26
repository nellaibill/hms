using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateAppointmentTypeRequest
{
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

// Code is intentionally absent — a natural-key field, protected from change after creation.
public record UpdateAppointmentTypeRequest
{
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

public record AppointmentTypeResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class AppointmentTypeListQuery : PagedRequest
{
    public bool? IsActive { get; set; }
}
