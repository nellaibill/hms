using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;

namespace HMS.Modules.HR.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping. A single entity doesn't justify a mapping library
/// (Mapster/AutoMapper) at this scale — see docs/DecisionLog.md.
/// </summary>
internal static class WeeklyRosterMappingExtensions
{
    public static WeeklyRosterResponse ToResponse(this WeeklyRoster weeklyRoster) => new()
    {
        Id = weeklyRoster.Id,
        WeekStartDate = weeklyRoster.WeekStartDate,
        DepartmentId = weeklyRoster.DepartmentId,
        Published = weeklyRoster.Published,
        PublishedDate = weeklyRoster.PublishedDate,
        CreatedAt = weeklyRoster.CreatedAt,
        UpdatedAt = weeklyRoster.UpdatedAt,
    };
}
