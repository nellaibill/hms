using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Application.Mapping;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Application;

/// <summary>
/// Public (not internal): ShiftSwapRequestsController — which ASP.NET Core requires to be
/// a public class with a public constructor for controller discovery/DI activation — takes
/// this as a constructor dependency; a public constructor cannot have an internal parameter
/// type (CS0051). Interface and implementation share this file, matching IShiftService.
/// </summary>
public interface IShiftSwapRequestService
{
    Task<Result<SwapRequestResponse>> CreateAsync(CreateSwapRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<SwapRequestResponse>> UpdateAsync(Guid id, UpdateSwapRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<SwapRequestResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<SwapRequestResponse>> GetPagedAsync(SwapRequestListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class ShiftSwapRequestService : IShiftSwapRequestService
{
    private readonly IShiftSwapRequestRepository _repository;
    private readonly IShiftAssignmentRepository _shiftAssignmentRepository;

    public ShiftSwapRequestService(IShiftSwapRequestRepository repository, IShiftAssignmentRepository shiftAssignmentRepository)
    {
        _repository = repository;
        _shiftAssignmentRepository = shiftAssignmentRepository;
    }

    public async Task<Result<SwapRequestResponse>> CreateAsync(CreateSwapRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var referentialCheck = await ValidateShiftAssignmentsExistAsync(request.CurrentShiftAssignmentId, request.RequestedShiftAssignmentId, cancellationToken);
        if (referentialCheck is not null)
        {
            return Result<SwapRequestResponse>.Failure(referentialCheck.ErrorCode!, referentialCheck.Error!);
        }

        var shiftSwapRequest = ShiftSwapRequest.Create(
            request.RequestedByStaffId,
            request.RequestedToStaffId,
            request.CurrentShiftAssignmentId,
            request.RequestedShiftAssignmentId,
            request.Status!.Value,
            request.RequestedDate,
            request.ApprovedDate,
            request.ApprovedBy,
            request.Remarks,
            actorId);

        await _repository.AddAsync(shiftSwapRequest, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<SwapRequestResponse>.Success(shiftSwapRequest.ToResponse());
    }

    public async Task<Result<SwapRequestResponse>> UpdateAsync(Guid id, UpdateSwapRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var shiftSwapRequest = await _repository.GetByIdAsync(id, cancellationToken);
        if (shiftSwapRequest is null)
        {
            return Result<SwapRequestResponse>.Failure(HRErrorCodes.NotFound, $"Shift swap request '{id}' was not found.");
        }

        var referentialCheck = await ValidateShiftAssignmentsExistAsync(request.CurrentShiftAssignmentId, request.RequestedShiftAssignmentId, cancellationToken);
        if (referentialCheck is not null)
        {
            return Result<SwapRequestResponse>.Failure(referentialCheck.ErrorCode!, referentialCheck.Error!);
        }

        shiftSwapRequest.Update(
            request.RequestedByStaffId,
            request.RequestedToStaffId,
            request.CurrentShiftAssignmentId,
            request.RequestedShiftAssignmentId,
            request.Status!.Value,
            request.RequestedDate,
            request.ApprovedDate,
            request.ApprovedBy,
            request.Remarks,
            actorId);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<SwapRequestResponse>.Success(shiftSwapRequest.ToResponse());
    }

    public async Task<Result<SwapRequestResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var shiftSwapRequest = await _repository.GetByIdAsync(id, cancellationToken);
        return shiftSwapRequest is null
            ? Result<SwapRequestResponse>.Failure(HRErrorCodes.NotFound, $"Shift swap request '{id}' was not found.")
            : Result<SwapRequestResponse>.Success(shiftSwapRequest.ToResponse());
    }

    public async Task<PagedResult<SwapRequestResponse>> GetPagedAsync(SwapRequestListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<SwapRequestResponse>(items.Select(s => s.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var shiftSwapRequest = await _repository.GetByIdAsync(id, cancellationToken);
        if (shiftSwapRequest is null)
        {
            return Result.Failure(HRErrorCodes.NotFound, $"Shift swap request '{id}' was not found.");
        }

        shiftSwapRequest.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    // Referential validation only (clarification #1) — confirms both ids resolve to real
    // ShiftAssignment records. Deliberately not a database foreign key: no new FK
    // relationship was requested for this phase, and this is not conflict detection —
    // it never inspects the assignments' contents, only that they exist.
    private async Task<Result?> ValidateShiftAssignmentsExistAsync(Guid currentShiftAssignmentId, Guid requestedShiftAssignmentId, CancellationToken cancellationToken)
    {
        var currentAssignment = await _shiftAssignmentRepository.GetByIdAsync(currentShiftAssignmentId, cancellationToken);
        if (currentAssignment is null)
        {
            return Result.Failure(HRErrorCodes.InvalidShiftAssignment, $"Current shift assignment '{currentShiftAssignmentId}' was not found.");
        }

        var requestedAssignment = await _shiftAssignmentRepository.GetByIdAsync(requestedShiftAssignmentId, cancellationToken);
        if (requestedAssignment is null)
        {
            return Result.Failure(HRErrorCodes.InvalidShiftAssignment, $"Requested shift assignment '{requestedShiftAssignmentId}' was not found.");
        }

        return null;
    }
}
