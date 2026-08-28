using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class DiagnosticPackageMappingExtensions
{
    public static DiagnosticPackageItemResponse ToResponse(this DiagnosticPackageItem item) => new()
    {
        Id = item.Id,
        ServiceId = item.ServiceId,
    };

    public static DiagnosticPackageResponse ToResponse(this DiagnosticPackage package) => new()
    {
        Id = package.Id,
        Code = package.Code,
        Name = package.Name,
        Description = package.Description,
        TotalPrice = package.TotalPrice,
        IsActive = package.IsActive,
        Items = package.Items.Select(i => i.ToResponse()).ToList(),
        CreatedAt = package.CreatedAt,
        UpdatedAt = package.UpdatedAt,
    };
}
