using HMS.Modules.Calendar.Application.Abstractions;
using HMS.Modules.Calendar.Application.Mapping;
using HMS.Modules.Calendar.Contracts;
using HMS.Modules.Calendar.Domain;
using HMS.Modules.HR.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.Calendar.Application;

/// <summary>
/// Public (not internal): EventsController — which ASP.NET Core requires to be a
/// public class with a public constructor for controller discovery/DI activation —
/// takes this as a constructor dependency; a public constructor cannot have an
/// internal parameter type (CS0051). Mirrors HR's IShiftService.
/// </summary>
public interface IEventService
{
    Task<Result<EventResponse>> CreateAsync(CreateEventRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<EventResponse>> UpdateAsync(Guid id, UpdateEventRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<EventResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<EventResponse>> GetPagedAsync(EventListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<PagedResult<EventResponse>> GetForMonthAsync(MonthlyEventQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// Orchestrates Event use cases. Two business rules from the Phase 1 spec are enforced
/// here (they need a database query, so they can't live in FluentValidation):
/// Department existence (cross-module, via HR's IDepartmentService — the same seam HR's
/// own WeeklyRosterService/ShiftAssignmentService use for the same check) and Holiday
/// date uniqueness (same-module, via IEventRepository).
///
/// A third rule from the spec — "Doctor Leave cannot overlap another approved leave for
/// the same doctor" — is intentionally NOT implemented. The Event table (per the
/// approved Phase 1 field list) has no field identifying which doctor a Doctor Leave
/// event belongs to, and no approval-status field. Without either, there is nothing to
/// compare "the same doctor" or "approved" against — the rule cannot be evaluated, let
/// alone enforced. Per explicit instruction, this gap is documented here rather than
/// resolved by adding an unrequested StaffId/DoctorId or Status column to the schema.
/// If this rule needs to become real, the schema will need to grow — there is no way
/// around it — and that decision belongs to whoever owns this module's next phase.
/// </summary>
internal class EventService : IEventService
{
    private readonly IEventRepository _repository;
    private readonly IDepartmentService _departmentService;

    public EventService(IEventRepository repository, IDepartmentService departmentService)
    {
        _repository = repository;
        _departmentService = departmentService;
    }

    public async Task<Result<EventResponse>> CreateAsync(CreateEventRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var referenceError = await ValidateDepartmentAsync(request.DepartmentId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<EventResponse>.Failure(referenceError.ErrorCode!, referenceError.Error!);
        }

        if (request.EventType == EventType.Holiday &&
            await _repository.ExistsHolidayOnDateAsync(request.StartDate, excludingId: null, cancellationToken))
        {
            return Result<EventResponse>.Failure(
                CalendarErrorCodes.DuplicateHoliday,
                $"A holiday already exists on {request.StartDate:yyyy-MM-dd}.");
        }

        var calendarEvent = Event.Create(
            request.Title,
            request.Description,
            request.EventType!.Value,
            request.StartDate,
            request.EndDate,
            request.IsAllDay,
            request.DepartmentId,
            actorId);

        await _repository.AddAsync(calendarEvent, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<EventResponse>.Success(calendarEvent.ToResponse());
    }

    public async Task<Result<EventResponse>> UpdateAsync(Guid id, UpdateEventRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var calendarEvent = await _repository.GetByIdAsync(id, cancellationToken);
        if (calendarEvent is null)
        {
            return Result<EventResponse>.Failure(CalendarErrorCodes.NotFound, $"Event '{id}' was not found.");
        }

        var referenceError = await ValidateDepartmentAsync(request.DepartmentId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<EventResponse>.Failure(referenceError.ErrorCode!, referenceError.Error!);
        }

        if (request.EventType == EventType.Holiday &&
            await _repository.ExistsHolidayOnDateAsync(request.StartDate, excludingId: id, cancellationToken))
        {
            return Result<EventResponse>.Failure(
                CalendarErrorCodes.DuplicateHoliday,
                $"A holiday already exists on {request.StartDate:yyyy-MM-dd}.");
        }

        calendarEvent.Update(
            request.Title,
            request.Description,
            request.EventType!.Value,
            request.StartDate,
            request.EndDate,
            request.IsAllDay,
            request.DepartmentId,
            actorId);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<EventResponse>.Success(calendarEvent.ToResponse());
    }

    public async Task<Result<EventResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var calendarEvent = await _repository.GetByIdAsync(id, cancellationToken);
        return calendarEvent is null
            ? Result<EventResponse>.Failure(CalendarErrorCodes.NotFound, $"Event '{id}' was not found.")
            : Result<EventResponse>.Success(calendarEvent.ToResponse());
    }

    public async Task<PagedResult<EventResponse>> GetPagedAsync(EventListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<EventResponse>(items.Select(e => e.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var calendarEvent = await _repository.GetByIdAsync(id, cancellationToken);
        if (calendarEvent is null)
        {
            return Result.Failure(CalendarErrorCodes.NotFound, $"Event '{id}' was not found.");
        }

        calendarEvent.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<PagedResult<EventResponse>> GetForMonthAsync(MonthlyEventQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetForMonthAsync(query, cancellationToken);
        return new PagedResult<EventResponse>(items.Select(e => e.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    private async Task<Result?> ValidateDepartmentAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        if (!departmentId.HasValue)
        {
            return null;
        }

        var departmentResult = await _departmentService.GetByIdAsync(departmentId.Value, cancellationToken);
        return departmentResult.IsSuccess
            ? null
            : Result.Failure(CalendarErrorCodes.InvalidDepartment, $"Department '{departmentId}' was not found.");
    }
}
