using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Mapping;

internal static class AdmissionAdvanceMappingExtensions
{
    public static AdmissionAdvanceResponse ToResponse(this AdmissionAdvance advance) => new()
    {
        Id = advance.Id,
        AdmissionId = advance.AdmissionId,
        Amount = advance.Amount,
        Method = advance.Method,
        ReferenceNumber = advance.ReferenceNumber,
        Remarks = advance.Remarks,
        CreatedAt = advance.CreatedAt,
    };
}
