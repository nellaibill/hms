using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping. A single entity doesn't justify a mapping library
/// (Mapster/AutoMapper) at this scale — see docs/DecisionLog.md. Deliberately a flat,
/// direct mapping with no joined Shift/Staff/Department details (e.g. no ShiftName) —
/// none of those modules exist yet to join against, and denormalizing display fields
/// wasn't part of the Phase 2 spec.
/// </summary>
internal static class ShiftAssignmentMappingExtensions
{
    public static ShiftAssignmentResponse ToResponse(this ShiftAssignment shiftAssignment) => new()
    {
        Id = shiftAssignment.Id,
        StaffId = shiftAssignment.StaffId,
        DepartmentId = shiftAssignment.DepartmentId,
        ShiftId = shiftAssignment.ShiftId,
        RosterDate = shiftAssignment.RosterDate,
        Status = shiftAssignment.Status,
        Remarks = shiftAssignment.Remarks,
        CreatedAt = shiftAssignment.CreatedAt,
        UpdatedAt = shiftAssignment.UpdatedAt,
    };
}
