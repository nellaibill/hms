using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping. A single entity doesn't justify a mapping library
/// (Mapster/AutoMapper) at this scale — see docs/DecisionLog.md.
/// </summary>
internal static class ShiftSwapRequestMappingExtensions
{
    public static SwapRequestResponse ToResponse(this ShiftSwapRequest shiftSwapRequest) => new()
    {
        Id = shiftSwapRequest.Id,
        RequestedByStaffId = shiftSwapRequest.RequestedByStaffId,
        RequestedToStaffId = shiftSwapRequest.RequestedToStaffId,
        CurrentShiftAssignmentId = shiftSwapRequest.CurrentShiftAssignmentId,
        RequestedShiftAssignmentId = shiftSwapRequest.RequestedShiftAssignmentId,
        Status = shiftSwapRequest.Status,
        RequestedDate = shiftSwapRequest.RequestedDate,
        ApprovedDate = shiftSwapRequest.ApprovedDate,
        ApprovedBy = shiftSwapRequest.ApprovedBy,
        Remarks = shiftSwapRequest.Remarks,
        CreatedAt = shiftSwapRequest.CreatedAt,
        UpdatedAt = shiftSwapRequest.UpdatedAt,
    };
}
