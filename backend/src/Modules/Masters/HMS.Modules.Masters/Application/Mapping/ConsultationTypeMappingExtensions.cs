using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class ConsultationTypeMappingExtensions
{
    public static ConsultationTypeResponse ToResponse(this ConsultationType consultationType) => new()
    {
        Id = consultationType.Id,
        Name = consultationType.Name,
        Amount = consultationType.Amount,
        IsActive = consultationType.IsActive,
        CreatedAt = consultationType.CreatedAt,
        UpdatedAt = consultationType.UpdatedAt,
    };
}
