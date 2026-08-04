using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping. A single entity doesn't justify a mapping library
/// (Mapster/AutoMapper) at this scale — see docs/DecisionLog.md.
/// </summary>
internal static class ShiftMappingExtensions
{
    public static ShiftResponse ToResponse(this Shift shift) => new()
    {
        Id = shift.Id,
        Code = shift.Code,
        Name = shift.Name,
        StartTime = shift.StartTime,
        EndTime = shift.EndTime,
        BreakMinutes = shift.BreakMinutes,
        GraceMinutes = shift.GraceMinutes,
        IsNightShift = shift.IsNightShift,
        IsActive = shift.IsActive,
        CreatedAt = shift.CreatedAt,
        UpdatedAt = shift.UpdatedAt,
    };
}
