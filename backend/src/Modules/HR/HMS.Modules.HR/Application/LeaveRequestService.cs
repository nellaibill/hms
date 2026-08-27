using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Application.Mapping;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Application;

/// <summary>
/// Public (not internal): LeaveRequestsController — which ASP.NET Core requires to be a
/// public class with a public constructor for controller discovery/DI activation — takes
/// this as a constructor dependency (CS0051 otherwise). Interface and implementation share
/// this file, matching the module's other {Entity}Service.cs convention.
/// </summary>
public interface ILeaveRequestService
{
    Task<Result<LeaveRequestResponse>> CreateAsync(CreateLeaveRequestRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LeaveRequestResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<LeaveRequestResponse>> GetPagedAsync(LeaveRequestListQuery query, CancellationToken cancellationToken);

    Task<Result<LeaveRequestResponse>> ApproveAsync(Guid id, ApproveLeaveRequestRequest request, Guid? actorUserId, CancellationToken cancellationToken);

    Task<Result<LeaveRequestResponse>> RejectAsync(Guid id, RejectLeaveRequestRequest request, Guid? actorUserId, CancellationToken cancellationToken);

    Task<Result<LeaveRequestResponse>> CancelAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;

    public LeaveRequestService(
        ILeaveRequestRepository repository,
        IEmployeeRepository employeeRepository,
        ILeaveTypeRepository leaveTypeRepository)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _leaveTypeRepository = leaveTypeRepository;
    }

    public async Task<Result<LeaveRequestResponse>> CreateAsync(CreateLeaveRequestRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result<LeaveRequestResponse>.Failure(HRErrorCodes.InvalidEmployee, $"Employee '{request.EmployeeId}' was not found.");
        }

        var leaveType = await _leaveTypeRepository.GetByIdAsync(request.LeaveTypeId, cancellationToken);
        if (leaveType is null)
        {
            return Result<LeaveRequestResponse>.Failure(HRErrorCodes.InvalidLeaveType, $"Leave type '{request.LeaveTypeId}' was not found.");
        }

        var startDate = request.StartDate!.Value;
        var endDate = request.EndDate!.Value;
        if (endDate < startDate)
        {
            return Result<LeaveRequestResponse>.Failure(HRErrorCodes.InvalidDateRange, "EndDate must not be earlier than StartDate.");
        }

        var leaveRequest = LeaveRequest.Create(request.EmployeeId, request.LeaveTypeId, startDate, endDate, request.Reason, actorId);

        await _repository.AddAsync(leaveRequest, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<LeaveRequestResponse>.Success(leaveRequest.ToResponse(employee.EmployeeCode, $"{employee.FirstName} {employee.LastName}", leaveType.Name));
    }

    public async Task<Result<LeaveRequestResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var leaveRequest = await _repository.GetByIdAsync(id, cancellationToken);
        if (leaveRequest is null)
        {
            return Result<LeaveRequestResponse>.Failure(HRErrorCodes.NotFound, $"Leave request '{id}' was not found.");
        }

        return Result<LeaveRequestResponse>.Success(await BuildResponseAsync(leaveRequest, cancellationToken));
    }

    public async Task<PagedResult<LeaveRequestResponse>> GetPagedAsync(LeaveRequestListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<LeaveRequestResponse>(items.Select(i => i.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<LeaveRequestResponse>> ApproveAsync(Guid id, ApproveLeaveRequestRequest request, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var leaveRequest = await _repository.GetByIdAsync(id, cancellationToken);
        if (leaveRequest is null)
        {
            return Result<LeaveRequestResponse>.Failure(HRErrorCodes.NotFound, $"Leave request '{id}' was not found.");
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            return Result<LeaveRequestResponse>.Failure(HRErrorCodes.InvalidStatusTransition, $"Leave request '{id}' is not Pending (current status: {leaveRequest.Status}) and cannot be approved.");
        }

        leaveRequest.Approve(actorUserId, request.Notes);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<LeaveRequestResponse>.Success(await BuildResponseAsync(leaveRequest, cancellationToken));
    }

    public async Task<Result<LeaveRequestResponse>> RejectAsync(Guid id, RejectLeaveRequestRequest request, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var leaveRequest = await _repository.GetByIdAsync(id, cancellationToken);
        if (leaveRequest is null)
        {
            return Result<LeaveRequestResponse>.Failure(HRErrorCodes.NotFound, $"Leave request '{id}' was not found.");
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            return Result<LeaveRequestResponse>.Failure(HRErrorCodes.InvalidStatusTransition, $"Leave request '{id}' is not Pending (current status: {leaveRequest.Status}) and cannot be rejected.");
        }

        leaveRequest.Reject(actorUserId, request.Reason);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<LeaveRequestResponse>.Success(await BuildResponseAsync(leaveRequest, cancellationToken));
    }

    public async Task<Result<LeaveRequestResponse>> CancelAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var leaveRequest = await _repository.GetByIdAsync(id, cancellationToken);
        if (leaveRequest is null)
        {
            return Result<LeaveRequestResponse>.Failure(HRErrorCodes.NotFound, $"Leave request '{id}' was not found.");
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            return Result<LeaveRequestResponse>.Failure(HRErrorCodes.InvalidStatusTransition, $"Leave request '{id}' is not Pending (current status: {leaveRequest.Status}) and cannot be cancelled.");
        }

        leaveRequest.Cancel(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<LeaveRequestResponse>.Success(await BuildResponseAsync(leaveRequest, cancellationToken));
    }

    private async Task<LeaveRequestResponse> BuildResponseAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(leaveRequest.EmployeeId, cancellationToken);
        var leaveType = await _leaveTypeRepository.GetByIdAsync(leaveRequest.LeaveTypeId, cancellationToken);

        return leaveRequest.ToResponse(
            employee?.EmployeeCode ?? string.Empty,
            employee is null ? string.Empty : $"{employee.FirstName} {employee.LastName}",
            leaveType?.Name ?? string.Empty);
    }
}
