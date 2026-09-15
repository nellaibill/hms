using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class DiagnosisMappingExtensions
{
    public static DiagnosisResponse ToResponse(this Diagnosis diagnosis) => new()
    {
        Id = diagnosis.Id,
        Name = diagnosis.Name,
        IcdCode = diagnosis.IcdCode,
        IsActive = diagnosis.IsActive,
        CreatedAt = diagnosis.CreatedAt,
        UpdatedAt = diagnosis.UpdatedAt,
    };
}
