using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): consumed both by DesignationsController (CS0051 otherwise) and by
/// HR's EmployeeService as the cross-module existence-check seam for Employee.DesignationId
/// (mirrors IDepartmentService). Interface and implementation share this file, matching the
/// other Masters entities' {Entity}Service.cs convention.
/// </summary>
public interface IDesignationService
{
    Task<Result<DesignationResponse>> CreateAsync(CreateDesignationRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DesignationResponse>> UpdateAsync(Guid id, UpdateDesignationRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DesignationResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<DesignationResponse>> GetPagedAsync(DesignationListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class DesignationService : IDesignationService
{
    private readonly IDesignationRepository _repository;

    public DesignationService(IDesignationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DesignationResponse>> CreateAsync(CreateDesignationRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.Code.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<DesignationResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Designation code '{request.Code}' is already in use.");
        }

        var designation = Designation.Create(request.Code, request.Name, request.IsActive, actorId);

        await _repository.AddAsync(designation, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DesignationResponse>.Success(designation.ToResponse());
    }

    public async Task<Result<DesignationResponse>> UpdateAsync(Guid id, UpdateDesignationRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var designation = await _repository.GetByIdAsync(id, cancellationToken);
        if (designation is null)
        {
            return Result<DesignationResponse>.Failure(MastersErrorCodes.NotFound, $"Designation '{id}' was not found.");
        }

        designation.Update(request.Name, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DesignationResponse>.Success(designation.ToResponse());
    }

    public async Task<Result<DesignationResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var designation = await _repository.GetByIdAsync(id, cancellationToken);
        return designation is null
            ? Result<DesignationResponse>.Failure(MastersErrorCodes.NotFound, $"Designation '{id}' was not found.")
            : Result<DesignationResponse>.Success(designation.ToResponse());
    }

    public async Task<PagedResult<DesignationResponse>> GetPagedAsync(DesignationListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<DesignationResponse>(items.Select(d => d.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var designation = await _repository.GetByIdAsync(id, cancellationToken);
        if (designation is null)
        {
            return Result.Failure(MastersErrorCodes.NotFound, $"Designation '{id}' was not found.");
        }

        designation.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
