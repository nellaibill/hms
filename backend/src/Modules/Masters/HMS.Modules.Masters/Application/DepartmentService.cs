using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): DepartmentsController requires a public constructor dependency
/// (CS0051 otherwise). Interface and implementation share this file, matching the other
/// Masters entities' {Entity}Service.cs convention.
/// </summary>
public interface IDepartmentService
{
    Task<Result<DepartmentResponse>> CreateAsync(CreateDepartmentRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DepartmentResponse>> UpdateAsync(Guid id, UpdateDepartmentRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DepartmentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<DepartmentResponse>> GetPagedAsync(DepartmentListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repository;

    public DepartmentService(IDepartmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DepartmentResponse>> CreateAsync(CreateDepartmentRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.Code.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<DepartmentResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Department code '{request.Code}' is already in use.");
        }

        var department = Department.Create(request.Code, request.Name, request.IsActive, actorId);

        await _repository.AddAsync(department, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DepartmentResponse>.Success(department.ToResponse());
    }

    public async Task<Result<DepartmentResponse>> UpdateAsync(Guid id, UpdateDepartmentRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var department = await _repository.GetByIdAsync(id, cancellationToken);
        if (department is null)
        {
            return Result<DepartmentResponse>.Failure(MastersErrorCodes.NotFound, $"Department '{id}' was not found.");
        }

        department.Update(request.Name, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DepartmentResponse>.Success(department.ToResponse());
    }

    public async Task<Result<DepartmentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var department = await _repository.GetByIdAsync(id, cancellationToken);
        return department is null
            ? Result<DepartmentResponse>.Failure(MastersErrorCodes.NotFound, $"Department '{id}' was not found.")
            : Result<DepartmentResponse>.Success(department.ToResponse());
    }

    public async Task<PagedResult<DepartmentResponse>> GetPagedAsync(DepartmentListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<DepartmentResponse>(items.Select(d => d.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var department = await _repository.GetByIdAsync(id, cancellationToken);
        if (department is null)
        {
            return Result.Failure(MastersErrorCodes.NotFound, $"Department '{id}' was not found.");
        }

        department.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
