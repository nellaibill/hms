using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Application.Mapping;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Modules.Identity.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Application;

/// <summary>
/// Public (not internal): StaffAvailabilityController — which ASP.NET Core requires to be
/// a public class with a public constructor for controller discovery/DI activation — takes
/// this as a constructor dependency; a public constructor cannot have an internal parameter
/// type (CS0051). Interface and implementation share this file, matching IShiftService.
/// </summary>
public interface IStaffAvailabilityService
{
    Task<Result<StaffAvailabilityResponse>> CreateAsync(CreateStaffAvailabilityRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<StaffAvailabilityResponse>> UpdateAsync(Guid id, UpdateStaffAvailabilityRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<StaffAvailabilityResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<StaffAvailabilityResponse>> GetPagedAsync(StaffAvailabilityListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class StaffAvailabilityService : IStaffAvailabilityService
{
    private readonly IStaffAvailabilityRepository _repository;
    private readonly IUserService _userService;

    public StaffAvailabilityService(IStaffAvailabilityRepository repository, IUserService userService)
    {
        _repository = repository;
        _userService = userService;
    }

    public async Task<Result<StaffAvailabilityResponse>> CreateAsync(CreateStaffAvailabilityRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var staffResult = await _userService.GetByIdAsync(request.StaffId, cancellationToken);
        if (!staffResult.IsSuccess)
        {
            return Result<StaffAvailabilityResponse>.Failure(HRErrorCodes.InvalidStaff, $"Staff '{request.StaffId}' was not found.");
        }

        var staffAvailability = StaffAvailability.Create(
            request.StaffId,
            request.StartDate,
            request.EndDate,
            request.AvailabilityStatus!.Value,
            request.Reason,
            actorId);

        await _repository.AddAsync(staffAvailability, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<StaffAvailabilityResponse>.Success(staffAvailability.ToResponse());
    }

    public async Task<Result<StaffAvailabilityResponse>> UpdateAsync(Guid id, UpdateStaffAvailabilityRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var staffAvailability = await _repository.GetByIdAsync(id, cancellationToken);
        if (staffAvailability is null)
        {
            return Result<StaffAvailabilityResponse>.Failure(HRErrorCodes.NotFound, $"Staff availability '{id}' was not found.");
        }

        var staffResult = await _userService.GetByIdAsync(request.StaffId, cancellationToken);
        if (!staffResult.IsSuccess)
        {
            return Result<StaffAvailabilityResponse>.Failure(HRErrorCodes.InvalidStaff, $"Staff '{request.StaffId}' was not found.");
        }

        staffAvailability.Update(
            request.StaffId,
            request.StartDate,
            request.EndDate,
            request.AvailabilityStatus!.Value,
            request.Reason,
            actorId);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<StaffAvailabilityResponse>.Success(staffAvailability.ToResponse());
    }

    public async Task<Result<StaffAvailabilityResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var staffAvailability = await _repository.GetByIdAsync(id, cancellationToken);
        return staffAvailability is null
            ? Result<StaffAvailabilityResponse>.Failure(HRErrorCodes.NotFound, $"Staff availability '{id}' was not found.")
            : Result<StaffAvailabilityResponse>.Success(staffAvailability.ToResponse());
    }

    public async Task<PagedResult<StaffAvailabilityResponse>> GetPagedAsync(StaffAvailabilityListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<StaffAvailabilityResponse>(items.Select(a => a.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var staffAvailability = await _repository.GetByIdAsync(id, cancellationToken);
        if (staffAvailability is null)
        {
            return Result.Failure(HRErrorCodes.NotFound, $"Staff availability '{id}' was not found.");
        }

        staffAvailability.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
