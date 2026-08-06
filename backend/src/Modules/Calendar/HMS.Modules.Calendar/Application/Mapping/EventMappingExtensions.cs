using HMS.Modules.Calendar.Contracts;
using HMS.Modules.Calendar.Domain;

namespace HMS.Modules.Calendar.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping. A single entity doesn't justify a mapping library
/// (Mapster/AutoMapper) at this scale — see docs/DecisionLog.md.
/// </summary>
internal static class EventMappingExtensions
{
    public static EventResponse ToResponse(this Event calendarEvent) => new()
    {
        Id = calendarEvent.Id,
        Title = calendarEvent.Title,
        Description = calendarEvent.Description,
        EventType = calendarEvent.EventType,
        StartDate = calendarEvent.StartDate,
        EndDate = calendarEvent.EndDate,
        IsAllDay = calendarEvent.IsAllDay,
        DepartmentId = calendarEvent.DepartmentId,
        CreatedBy = calendarEvent.CreatedBy,
        CreatedAt = calendarEvent.CreatedAt,
        UpdatedAt = calendarEvent.UpdatedAt,
    };
}
