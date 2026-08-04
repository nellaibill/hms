using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Application.Mapping;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Application;

/// <summary>
/// Public (not internal): ShiftsController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as
/// a constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051). Mirrors HMS.Modules.Masters.IWarehouseService, which keeps the interface and
/// implementation in the same file rather than splitting them (Masters' convention, used
/// here as the primary CRUD reference).
/// </summary>
public interface IShiftService
{
    Task<Result<ShiftResponse>> CreateAsync(CreateShiftRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ShiftResponse>> UpdateAsync(Guid id, UpdateShiftRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ShiftResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ShiftResponse>> GetPagedAsync(ShiftListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class ShiftService : IShiftService
{
    private readonly IShiftRepository _repository;

    public ShiftService(IShiftRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ShiftResponse>> CreateAsync(CreateShiftRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.Code.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<ShiftResponse>.Failure(HRErrorCodes.DuplicateCode, $"Shift code '{request.Code}' is already in use.");
        }

        var shift = Shift.Create(
            request.Code,
            request.Name,
            request.StartTime!.Value,
            request.EndTime!.Value,
            request.BreakMinutes,
            request.GraceMinutes,
            request.IsNightShift,
            request.IsActive,
            actorId);

        await _repository.AddAsync(shift, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ShiftResponse>.Success(shift.ToResponse());
    }

    public async Task<Result<ShiftResponse>> UpdateAsync(Guid id, UpdateShiftRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var shift = await _repository.GetByIdAsync(id, cancellationToken);
        if (shift is null)
        {
            return Result<ShiftResponse>.Failure(HRErrorCodes.NotFound, $"Shift '{id}' was not found.");
        }

        shift.Update(
            request.Name,
            request.StartTime!.Value,
            request.EndTime!.Value,
            request.BreakMinutes,
            request.GraceMinutes,
            request.IsNightShift,
            request.IsActive,
            actorId);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ShiftResponse>.Success(shift.ToResponse());
    }

    public async Task<Result<ShiftResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var shift = await _repository.GetByIdAsync(id, cancellationToken);
        return shift is null
            ? Result<ShiftResponse>.Failure(HRErrorCodes.NotFound, $"Shift '{id}' was not found.")
            : Result<ShiftResponse>.Success(shift.ToResponse());
    }

    public async Task<PagedResult<ShiftResponse>> GetPagedAsync(ShiftListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<ShiftResponse>(items.Select(s => s.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var shift = await _repository.GetByIdAsync(id, cancellationToken);
        if (shift is null)
        {
            return Result.Failure(HRErrorCodes.NotFound, $"Shift '{id}' was not found.");
        }

        shift.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
