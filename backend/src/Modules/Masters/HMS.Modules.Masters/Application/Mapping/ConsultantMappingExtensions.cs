using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class ConsultantMappingExtensions
{
    public static ConsultantResponse ToResponse(this Consultant consultant) => new()
    {
        Id = consultant.Id,
        Code = consultant.Code,
        Name = consultant.Name,
        DepartmentId = consultant.DepartmentId,
        Specialization = consultant.Specialization,
        IsActive = consultant.IsActive,
        CreatedAt = consultant.CreatedAt,
        UpdatedAt = consultant.UpdatedAt,
    };
}
