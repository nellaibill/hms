using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class DiagnosticTestMappingExtensions
{
    public static DiagnosticTestResponse ToResponse(this DiagnosticTest diagnosticTest) => new()
    {
        Id = diagnosticTest.Id,
        Name = diagnosticTest.Name,
        ServiceType = diagnosticTest.ServiceType,
        Category = diagnosticTest.Category,
        Price = diagnosticTest.Price,
        IsOutsourced = diagnosticTest.IsOutsourced,
        ReferenceLab = diagnosticTest.ReferenceLab,
        IsActive = diagnosticTest.IsActive,
        CreatedAt = diagnosticTest.CreatedAt,
        UpdatedAt = diagnosticTest.UpdatedAt,
    };
}
