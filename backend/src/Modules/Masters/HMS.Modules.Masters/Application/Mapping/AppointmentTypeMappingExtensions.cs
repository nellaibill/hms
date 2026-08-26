using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class AppointmentTypeMappingExtensions
{
    public static AppointmentTypeResponse ToResponse(this AppointmentType appointmentType) => new()
    {
        Id = appointmentType.Id,
        Name = appointmentType.Name,
        IsActive = appointmentType.IsActive,
        CreatedAt = appointmentType.CreatedAt,
        UpdatedAt = appointmentType.UpdatedAt,
    };
}
