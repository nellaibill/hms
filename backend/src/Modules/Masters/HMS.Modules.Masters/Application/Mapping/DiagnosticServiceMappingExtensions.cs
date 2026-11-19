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
        CostPrice = diagnosticService.CostPrice,
        IsActive = diagnosticService.IsActive,
        DoctorCharge = diagnosticService.DoctorCharge,
        BedsidePrice = diagnosticService.BedsidePrice,
        BedsideDoctorCharge = diagnosticService.BedsideDoctorCharge,
        RateOptions = diagnosticService.RateOptions
            .OrderBy(option => option.SortOrder)
            .Select(option => new DiagnosticServiceRateOptionDto
            {
                Id = option.Id,
                Name = option.Name,
                Price = option.Price,
                DoctorCharge = option.DoctorCharge,
                CostPrice = option.CostPrice,
            })
            .ToList(),
        CreatedAt = diagnosticService.CreatedAt,
        UpdatedAt = diagnosticService.UpdatedAt,
    };
}
