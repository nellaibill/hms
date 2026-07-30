using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

public interface IStockAdjustmentReasonService
{
    Task<Result<StockAdjustmentReasonResponse>> CreateAsync(CreateStockAdjustmentReasonRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<StockAdjustmentReasonResponse>> UpdateAsync(Guid id, UpdateStockAdjustmentReasonRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<StockAdjustmentReasonResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<StockAdjustmentReasonResponse>> GetPagedAsync(StockAdjustmentReasonListQuery query, CancellationToken cancellationToken);
}

internal class StockAdjustmentReasonService : IStockAdjustmentReasonService
{
    private readonly IStockAdjustmentReasonRepository _repository;

    public StockAdjustmentReasonService(IStockAdjustmentReasonRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<StockAdjustmentReasonResponse>> CreateAsync(CreateStockAdjustmentReasonRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.ReasonCode.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<StockAdjustmentReasonResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Stock adjustment reason code '{request.ReasonCode}' is already in use.");
        }

        var reason = StockAdjustmentReason.Create(request.ReasonCode, request.ReasonName, request.AffectsValuation, request.Description, request.IsActive, actorId);

        await _repository.AddAsync(reason, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<StockAdjustmentReasonResponse>.Success(reason.ToResponse());
    }

    public async Task<Result<StockAdjustmentReasonResponse>> UpdateAsync(Guid id, UpdateStockAdjustmentReasonRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var reason = await _repository.GetByIdAsync(id, cancellationToken);
        if (reason is null)
        {
            return Result<StockAdjustmentReasonResponse>.Failure(MastersErrorCodes.NotFound, $"Stock adjustment reason '{id}' was not found.");
        }

        reason.Update(request.ReasonName, request.AffectsValuation, request.Description, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<StockAdjustmentReasonResponse>.Success(reason.ToResponse());
    }

    public async Task<Result<StockAdjustmentReasonResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var reason = await _repository.GetByIdAsync(id, cancellationToken);
        return reason is null
            ? Result<StockAdjustmentReasonResponse>.Failure(MastersErrorCodes.NotFound, $"Stock adjustment reason '{id}' was not found.")
            : Result<StockAdjustmentReasonResponse>.Success(reason.ToResponse());
    }

    public async Task<PagedResult<StockAdjustmentReasonResponse>> GetPagedAsync(StockAdjustmentReasonListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<StockAdjustmentReasonResponse>(items.Select(s => s.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
