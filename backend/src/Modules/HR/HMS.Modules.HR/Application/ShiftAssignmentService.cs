using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Application.Mapping;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Modules.Identity.Application;
using HMS.Modules.Masters.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Application;

/// <summary>
/// Public (not internal): ShiftAssignmentsController — which ASP.NET Core requires to be a
/// public class with a public constructor for controller discovery/DI activation — takes
/// this as a constructor dependency; a public constructor cannot have an internal parameter
/// type (CS0051). Interface and implementation share this file, matching IShiftService.
/// </summary>
public interface IShiftAssignmentService
{
    Task<Result<ShiftAssignmentResponse>> CreateAsync(CreateShiftAssignmentRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ShiftAssignmentResponse>> UpdateAsync(Guid id, UpdateShiftAssignmentRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ShiftAssignmentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ShiftAssignmentResponse>> GetPagedAsync(ShiftAssignmentListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class ShiftAssignmentService : IShiftAssignmentService
{
    private readonly IShiftAssignmentRepository _repository;
    private readonly IShiftRepository _shiftRepository;
    private readonly IDepartmentService _departmentService;
    private readonly IUserService _userService;

    public ShiftAssignmentService(
        IShiftAssignmentRepository repository,
        IShiftRepository shiftRepository,
        IDepartmentService departmentService,
        IUserService userService)
    {
        _repository = repository;
        _shiftRepository = shiftRepository;
        _departmentService = departmentService;
        _userService = userService;
    }

    public async Task<Result<ShiftAssignmentResponse>> CreateAsync(CreateShiftAssignmentRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetByIdAsync(request.ShiftId, cancellationToken);
        if (shift is null)
        {
            return Result<ShiftAssignmentResponse>.Failure(HRErrorCodes.InvalidShift, $"Shift '{request.ShiftId}' was not found.");
        }

        var referenceError = await ValidateReferencesAsync(request.StaffId, request.DepartmentId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<ShiftAssignmentResponse>.Failure(referenceError.ErrorCode!, referenceError.Error!);
        }

        var overlapError = await ValidateNoOverlapAsync(request.StaffId, request.RosterDate, shift, excludingId: null, cancellationToken);
        if (overlapError is not null)
        {
            return Result<ShiftAssignmentResponse>.Failure(overlapError.ErrorCode!, overlapError.Error!);
        }

        var shiftAssignment = ShiftAssignment.Create(
            request.StaffId,
            request.DepartmentId,
            request.ShiftId,
            request.RosterDate,
            request.Status,
            request.Remarks,
            actorId);

        await _repository.AddAsync(shiftAssignment, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ShiftAssignmentResponse>.Success(shiftAssignment.ToResponse());
    }

    public async Task<Result<ShiftAssignmentResponse>> UpdateAsync(Guid id, UpdateShiftAssignmentRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var shiftAssignment = await _repository.GetByIdAsync(id, cancellationToken);
        if (shiftAssignment is null)
        {
            return Result<ShiftAssignmentResponse>.Failure(HRErrorCodes.NotFound, $"Shift assignment '{id}' was not found.");
        }

        var shift = await _shiftRepository.GetByIdAsync(request.ShiftId, cancellationToken);
        if (shift is null)
        {
            return Result<ShiftAssignmentResponse>.Failure(HRErrorCodes.InvalidShift, $"Shift '{request.ShiftId}' was not found.");
        }

        var referenceError = await ValidateReferencesAsync(request.StaffId, request.DepartmentId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<ShiftAssignmentResponse>.Failure(referenceError.ErrorCode!, referenceError.Error!);
        }

        var overlapError = await ValidateNoOverlapAsync(request.StaffId, request.RosterDate, shift, excludingId: id, cancellationToken);
        if (overlapError is not null)
        {
            return Result<ShiftAssignmentResponse>.Failure(overlapError.ErrorCode!, overlapError.Error!);
        }

        shiftAssignment.Update(
            request.StaffId,
            request.DepartmentId,
            request.ShiftId,
            request.RosterDate,
            request.Status,
            request.Remarks,
            actorId);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ShiftAssignmentResponse>.Success(shiftAssignment.ToResponse());
    }

    public async Task<Result<ShiftAssignmentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var shiftAssignment = await _repository.GetByIdAsync(id, cancellationToken);
        return shiftAssignment is null
            ? Result<ShiftAssignmentResponse>.Failure(HRErrorCodes.NotFound, $"Shift assignment '{id}' was not found.")
            : Result<ShiftAssignmentResponse>.Success(shiftAssignment.ToResponse());
    }

    public async Task<PagedResult<ShiftAssignmentResponse>> GetPagedAsync(ShiftAssignmentListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<ShiftAssignmentResponse>(items.Select(sa => sa.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var shiftAssignment = await _repository.GetByIdAsync(id, cancellationToken);
        if (shiftAssignment is null)
        {
            return Result.Failure(HRErrorCodes.NotFound, $"Shift assignment '{id}' was not found.");
        }

        shiftAssignment.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    // Both checks are cross-module now (StaffId → Identity's User, DepartmentId → Masters'
    // Department, since Department was consolidated out of this module — see
    // docs/DecisionLog.md), run together since every caller needs both. Before this existed,
    // StaffId/DepartmentId were accepted as-is with no existence check at all — any Guid,
    // real or not, would be silently stored.
    private async Task<Result?> ValidateReferencesAsync(Guid staffId, Guid departmentId, CancellationToken cancellationToken)
    {
        var staffResult = await _userService.GetByIdAsync(staffId, cancellationToken);
        if (!staffResult.IsSuccess)
        {
            return Result.Failure(HRErrorCodes.InvalidStaff, $"Staff '{staffId}' was not found.");
        }

        if (!(await _departmentService.GetByIdAsync(departmentId, cancellationToken)).IsSuccess)
        {
            return Result.Failure(HRErrorCodes.InvalidDepartment, $"Department '{departmentId}' was not found.");
        }

        return null;
    }

    // Rejects assigning the same staff member to two shifts on the same date whose actual
    // clock time ranges overlap — e.g. a 09:00-17:00 shift and a 16:00-00:00 shift on the
    // same day both cover 16:00-17:00. Deliberately scoped to the *same* RosterDate only:
    // it does not chase a night shift's occupied time into the next calendar day to check
    // against that day's own assignments. That cross-day case is a real, documented gap
    // (see docs/modules/HR — the "problems in this module" review), not silently ignored.
    private async Task<Result?> ValidateNoOverlapAsync(
        Guid staffId,
        DateOnly rosterDate,
        Shift shift,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        var sameDayAssignments = await _repository.GetByStaffAndDateAsync(staffId, rosterDate, excludingId, cancellationToken);
        if (sameDayAssignments.Count == 0)
        {
            return null;
        }

        var (newStart, newEnd) = ToOccupiedInterval(rosterDate, shift.StartTime, shift.EndTime);

        foreach (var existing in sameDayAssignments)
        {
            var existingShift = await _shiftRepository.GetByIdAsync(existing.ShiftId, cancellationToken);
            if (existingShift is null)
            {
                // The referenced shift was hard-deleted out from under an old assignment —
                // nothing to compare against; skip rather than fail the whole request.
                continue;
            }

            var (existingStart, existingEnd) = ToOccupiedInterval(rosterDate, existingShift.StartTime, existingShift.EndTime);

            if (newStart < existingEnd && existingStart < newEnd)
            {
                return Result.Failure(
                    HRErrorCodes.ShiftOverlap,
                    $"Staff '{staffId}' already has an overlapping shift assignment ('{existingShift.Name}') on {rosterDate}.");
            }
        }

        return null;
    }

    // A shift's actual occupied clock-time interval on a given date. EndTime earlier than
    // StartTime means the shift crosses midnight (enforced by CreateShiftRequestValidator/
    // UpdateShiftRequestValidator's IsNightShift rule), so its end falls on the next
    // calendar day.
    private static (DateTime Start, DateTime End) ToOccupiedInterval(DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        var start = date.ToDateTime(startTime);
        var end = endTime < startTime ? date.AddDays(1).ToDateTime(endTime) : date.ToDateTime(endTime);
        return (start, end);
    }
}
