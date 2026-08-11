using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Mapping;

internal static class AdmissionChargeMappingExtensions
{
    public static AdmissionChargeResponse ToResponse(this AdmissionCharge charge) => new()
    {
        Id = charge.Id,
        AdmissionId = charge.AdmissionId,
        ChargeType = charge.ChargeType,
        Amount = charge.Amount,
        Remarks = charge.Remarks,
        CreatedAt = charge.CreatedAt,
    };
}
