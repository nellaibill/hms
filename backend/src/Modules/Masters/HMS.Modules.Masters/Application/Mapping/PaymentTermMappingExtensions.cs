using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Mapping;

internal static class PaymentTermMappingExtensions
{
    public static PaymentTermResponse ToResponse(this PaymentTerm term) => new()
    {
        Id = term.Id,
        TermName = term.TermName,
        Days = term.Days,
        Description = term.Description,
        IsActive = term.IsActive,
        CreatedAt = term.CreatedAt,
        UpdatedAt = term.UpdatedAt,
    };
}
