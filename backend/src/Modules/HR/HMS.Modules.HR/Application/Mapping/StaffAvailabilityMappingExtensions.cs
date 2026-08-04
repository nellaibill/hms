using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping. A single entity doesn't justify a mapping library
/// (Mapster/AutoMapper) at this scale — see docs/DecisionLog.md.
/// </summary>
internal static class StaffAvailabilityMappingExtensions
{
    public static StaffAvailabilityResponse ToResponse(this StaffAvailability staffAvailability) => new()
    {
        Id = staffAvailability.Id,
        StaffId = staffAvailability.StaffId,
        StartDate = staffAvailability.StartDate,
        EndDate = staffAvailability.EndDate,
        AvailabilityStatus = staffAvailability.AvailabilityStatus,
        Reason = staffAvailability.Reason,
        CreatedAt = staffAvailability.CreatedAt,
        UpdatedAt = staffAvailability.UpdatedAt,
    };
}
