using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class DiagnosticServiceMappingExtensions
{
    public static DiagnosticServiceResponse ToResponse(this DiagnosticService diagnosticService) => new()
    {
        Id = diagnosticService.Id,
        Code = diagnosticService.Code,
        Name = diagnosticService.Name,
        CategoryId = diagnosticService.CategoryId,
        ServiceType = diagnosticService.ServiceType,
        IsOutsourced = diagnosticService.IsOutsourced,
        ProviderId = diagnosticService.ProviderId,
        Price = diagnosticService.Price,
        IsActive = diagnosticService.IsActive,
        CreatedAt = diagnosticService.CreatedAt,
        UpdatedAt = diagnosticService.UpdatedAt,
    };
}
