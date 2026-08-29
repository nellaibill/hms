using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class DiagnosticCategoryMappingExtensions
{
    public static DiagnosticCategoryResponse ToResponse(this DiagnosticCategory category) => new()
    {
        Id = category.Id,
        Code = category.Code,
        Name = category.Name,
        Description = category.Description,
        IsActive = category.IsActive,
        CreatedAt = category.CreatedAt,
        UpdatedAt = category.UpdatedAt,
    };
}
