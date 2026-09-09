using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Mapping;

internal static class MedicationAdministrationMappingExtensions
{
    public static MedicationAdministrationResponse ToResponse(this MedicationAdministration administration) => new()
    {
        Id = administration.Id,
        MedicationOrderId = administration.MedicationOrderId,
        ScheduledTime = administration.ScheduledTime,
        WasGiven = administration.WasGiven,
        AdministeredAt = administration.AdministeredAt,
        Reason = administration.Reason,
        Remarks = administration.Remarks,
        RecordedByUserId = administration.RecordedByUserId,
        CreatedAt = administration.CreatedAt,
    };
}
