using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Application.Mapping;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
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

    public ShiftAssignmentService(IShiftAssignmentRepository repository, IShiftRepository shiftRepository)
    {
        _repository = repository;
        _shiftRepository = shiftRepository;
    }

    public async Task<Result<ShiftAssignmentResponse>> CreateAsync(CreateShiftAssignmentRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetByIdAsync(request.ShiftId, cancellationToken);
        if (shift is null)
        {
            return Result<ShiftAssignmentResponse>.Failure(HRErrorCodes.InvalidShift, $"Shift '{request.ShiftId}' was not found.");
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
}
