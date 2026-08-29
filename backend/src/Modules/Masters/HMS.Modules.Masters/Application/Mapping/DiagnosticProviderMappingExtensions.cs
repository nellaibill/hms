using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class DiagnosticProviderMappingExtensions
{
    public static DiagnosticProviderResponse ToResponse(this DiagnosticProvider provider) => new()
    {
        Id = provider.Id,
        Code = provider.Code,
        Name = provider.Name,
        ContactDetails = provider.ContactDetails,
        IsActive = provider.IsActive,
        CreatedAt = provider.CreatedAt,
        UpdatedAt = provider.UpdatedAt,
    };
}
