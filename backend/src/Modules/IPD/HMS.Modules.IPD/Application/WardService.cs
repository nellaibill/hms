using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Application.Mapping;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using HMS.Modules.Masters.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.IPD.Application;

/// <summary>
/// Public (not internal): WardsController — which ASP.NET Core requires to be a public
/// class with a public constructor for controller discovery/DI activation — takes this as
/// a constructor dependency; a public constructor cannot have an internal parameter type
/// (CS0051).
/// </summary>
public interface IWardService
{
    Task<Result<WardResponse>> CreateAsync(CreateWardRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<WardResponse>> UpdateAsync(Guid id, UpdateWardRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<WardResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<WardResponse>> GetPagedAsync(WardListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class WardService : IWardService
{
    private readonly IWardRepository _repository;
    private readonly IDepartmentService _departmentService;

    public WardService(IWardRepository repository, IDepartmentService departmentService)
    {
        _repository = repository;
        _departmentService = departmentService;
    }

    public async Task<Result<WardResponse>> CreateAsync(CreateWardRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.Code.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<WardResponse>.Failure(IPDErrorCodes.DuplicateCode, $"Ward code '{request.Code}' is already in use.");
        }

        if (!(await _departmentService.GetByIdAsync(request.DepartmentId, cancellationToken)).IsSuccess)
        {
            return Result<WardResponse>.Failure(IPDErrorCodes.InvalidDepartment, $"Department '{request.DepartmentId}' was not found.");
        }

        var ward = Ward.Create(request.Code, request.Name, request.DepartmentId, request.WardType, request.IsActive, actorId);

        await _repository.AddAsync(ward, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<WardResponse>.Success(ward.ToResponse());
    }

    public async Task<Result<WardResponse>> UpdateAsync(Guid id, UpdateWardRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var ward = await _repository.GetByIdAsync(id, cancellationToken);
        if (ward is null)
        {
            return Result<WardResponse>.Failure(IPDErrorCodes.NotFound, $"Ward '{id}' was not found.");
        }

        if (!(await _departmentService.GetByIdAsync(request.DepartmentId, cancellationToken)).IsSuccess)
        {
            return Result<WardResponse>.Failure(IPDErrorCodes.InvalidDepartment, $"Department '{request.DepartmentId}' was not found.");
        }

        ward.Update(request.Name, request.DepartmentId, request.WardType, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<WardResponse>.Success(ward.ToResponse());
    }

    public async Task<Result<WardResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var ward = await _repository.GetByIdAsync(id, cancellationToken);
        return ward is null
            ? Result<WardResponse>.Failure(IPDErrorCodes.NotFound, $"Ward '{id}' was not found.")
            : Result<WardResponse>.Success(ward.ToResponse());
    }

    public async Task<PagedResult<WardResponse>> GetPagedAsync(WardListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<WardResponse>(items.Select(w => w.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var ward = await _repository.GetByIdAsync(id, cancellationToken);
        if (ward is null)
        {
            return Result.Failure(IPDErrorCodes.NotFound, $"Ward '{id}' was not found.");
        }

        ward.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
