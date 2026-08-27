using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Application.Mapping;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Application;

/// <summary>
/// Public (not internal): consumed both by LeaveTypesController (CS0051 otherwise) and by
/// EmployeeService's leave-balance calculation. Interface and implementation share this file,
/// matching the module's other {Entity}Service.cs convention.
/// </summary>
public interface ILeaveTypeService
{
    Task<Result<LeaveTypeResponse>> CreateAsync(CreateLeaveTypeRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LeaveTypeResponse>> UpdateAsync(Guid id, UpdateLeaveTypeRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<LeaveTypeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<LeaveTypeResponse>> GetPagedAsync(LeaveTypeListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class LeaveTypeService : ILeaveTypeService
{
    private readonly ILeaveTypeRepository _repository;

    public LeaveTypeService(ILeaveTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<LeaveTypeResponse>> CreateAsync(CreateLeaveTypeRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.Code.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<LeaveTypeResponse>.Failure(HRErrorCodes.DuplicateCode, $"Leave type code '{request.Code}' is already in use.");
        }

        var leaveType = LeaveType.Create(request.Code, request.Name, request.MaxDaysPerYear, request.IsPaid, request.IsActive, actorId);

        await _repository.AddAsync(leaveType, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<LeaveTypeResponse>.Success(leaveType.ToResponse());
    }

    public async Task<Result<LeaveTypeResponse>> UpdateAsync(Guid id, UpdateLeaveTypeRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var leaveType = await _repository.GetByIdAsync(id, cancellationToken);
        if (leaveType is null)
        {
            return Result<LeaveTypeResponse>.Failure(HRErrorCodes.NotFound, $"Leave type '{id}' was not found.");
        }

        leaveType.Update(request.Name, request.MaxDaysPerYear, request.IsPaid, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<LeaveTypeResponse>.Success(leaveType.ToResponse());
    }

    public async Task<Result<LeaveTypeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var leaveType = await _repository.GetByIdAsync(id, cancellationToken);
        return leaveType is null
            ? Result<LeaveTypeResponse>.Failure(HRErrorCodes.NotFound, $"Leave type '{id}' was not found.")
            : Result<LeaveTypeResponse>.Success(leaveType.ToResponse());
    }

    public async Task<PagedResult<LeaveTypeResponse>> GetPagedAsync(LeaveTypeListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<LeaveTypeResponse>(items.Select(l => l.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var leaveType = await _repository.GetByIdAsync(id, cancellationToken);
        if (leaveType is null)
        {
            return Result.Failure(HRErrorCodes.NotFound, $"Leave type '{id}' was not found.");
        }

        leaveType.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
