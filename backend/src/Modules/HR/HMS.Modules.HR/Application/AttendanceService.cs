using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Application.Mapping;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Application;

/// <summary>
/// Public (not internal): AttendanceController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as a
/// constructor dependency (CS0051 otherwise). Interface and implementation share this file,
/// matching IShiftService/IEmployeeService.
/// </summary>
public interface IAttendanceService
{
    Task<Result<AttendanceResponse>> CreateAsync(CreateAttendanceRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<AttendanceResponse>> UpdateAsync(Guid id, UpdateAttendanceRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<AttendanceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<AttendanceResponse>> GetPagedAsync(AttendanceListQuery query, CancellationToken cancellationToken);

    Task<Result<AttendanceResponse>> CheckInAsync(CheckInRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<AttendanceResponse>> CheckOutAsync(CheckOutRequest request, Guid? actorId, CancellationToken cancellationToken);
}

internal class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;

    public AttendanceService(IAttendanceRepository repository, IEmployeeRepository employeeRepository)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<AttendanceResponse>> CreateAsync(CreateAttendanceRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result<AttendanceResponse>.Failure(HRErrorCodes.InvalidEmployee, $"Employee '{request.EmployeeId}' was not found.");
        }

        var attendanceDate = request.AttendanceDate!.Value;
        if (await _repository.ExistsForEmployeeAndDateAsync(request.EmployeeId, attendanceDate, excludingId: null, cancellationToken))
        {
            return Result<AttendanceResponse>.Failure(HRErrorCodes.DuplicateAttendance, $"Employee '{request.EmployeeId}' already has an attendance record for {attendanceDate}.");
        }

        var attendance = Attendance.Create(request.EmployeeId, attendanceDate, request.CheckInTime, request.CheckOutTime, request.Status, request.Remarks, actorId);

        await _repository.AddAsync(attendance, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<AttendanceResponse>.Success(attendance.ToResponse(employee.EmployeeCode, $"{employee.FirstName} {employee.LastName}"));
    }

    public async Task<Result<AttendanceResponse>> UpdateAsync(Guid id, UpdateAttendanceRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var attendance = await _repository.GetByIdAsync(id, cancellationToken);
        if (attendance is null)
        {
            return Result<AttendanceResponse>.Failure(HRErrorCodes.NotFound, $"Attendance record '{id}' was not found.");
        }

        attendance.Update(request.CheckInTime, request.CheckOutTime, request.Status, request.Remarks, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        var employee = await _employeeRepository.GetByIdAsync(attendance.EmployeeId, cancellationToken);
        return Result<AttendanceResponse>.Success(attendance.ToResponse(employee?.EmployeeCode ?? string.Empty, employee is null ? string.Empty : $"{employee.FirstName} {employee.LastName}"));
    }

    public async Task<Result<AttendanceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var attendance = await _repository.GetByIdAsync(id, cancellationToken);
        if (attendance is null)
        {
            return Result<AttendanceResponse>.Failure(HRErrorCodes.NotFound, $"Attendance record '{id}' was not found.");
        }

        var employee = await _employeeRepository.GetByIdAsync(attendance.EmployeeId, cancellationToken);
        return Result<AttendanceResponse>.Success(attendance.ToResponse(employee?.EmployeeCode ?? string.Empty, employee is null ? string.Empty : $"{employee.FirstName} {employee.LastName}"));
    }

    public async Task<PagedResult<AttendanceResponse>> GetPagedAsync(AttendanceListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<AttendanceResponse>(items.Select(i => i.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<AttendanceResponse>> CheckInAsync(CheckInRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result<AttendanceResponse>.Failure(HRErrorCodes.InvalidEmployee, $"Employee '{request.EmployeeId}' was not found.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var time = request.CheckInTime ?? DateTime.UtcNow;

        var attendance = await _repository.GetByEmployeeAndDateAsync(request.EmployeeId, today, cancellationToken);
        if (attendance is null)
        {
            // Fresh row — check-in itself decides the day's status (Present) since nothing
            // was "already explicitly set" via the manual create/update path.
            attendance = Attendance.Create(request.EmployeeId, today, time, null, AttendanceStatus.Present, null, actorId);
            await _repository.AddAsync(attendance, cancellationToken);
        }
        else
        {
            if (attendance.CheckInTime is not null)
            {
                return Result<AttendanceResponse>.Failure(HRErrorCodes.AlreadyCheckedIn, $"Employee '{request.EmployeeId}' has already checked in today.");
            }

            // A row already existed (e.g. a manual correction created it first) — its Status
            // was already explicitly set, so check-in only records the time, per the spec.
            attendance.RecordCheckIn(time, actorId);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<AttendanceResponse>.Success(attendance.ToResponse(employee.EmployeeCode, $"{employee.FirstName} {employee.LastName}"));
    }

    public async Task<Result<AttendanceResponse>> CheckOutAsync(CheckOutRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result<AttendanceResponse>.Failure(HRErrorCodes.InvalidEmployee, $"Employee '{request.EmployeeId}' was not found.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var attendance = await _repository.GetByEmployeeAndDateAsync(request.EmployeeId, today, cancellationToken);
        if (attendance is null || attendance.CheckInTime is null)
        {
            return Result<AttendanceResponse>.Failure(HRErrorCodes.NotCheckedIn, $"Employee '{request.EmployeeId}' has not checked in today.");
        }

        if (attendance.CheckOutTime is not null)
        {
            return Result<AttendanceResponse>.Failure(HRErrorCodes.AlreadyCheckedOut, $"Employee '{request.EmployeeId}' has already checked out today.");
        }

        var time = request.CheckOutTime ?? DateTime.UtcNow;
        attendance.RecordCheckOut(time, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<AttendanceResponse>.Success(attendance.ToResponse(employee.EmployeeCode, $"{employee.FirstName} {employee.LastName}"));
    }
}
