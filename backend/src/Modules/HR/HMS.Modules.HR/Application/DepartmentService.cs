using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Application.Mapping;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Application;

/// <summary>
/// Public (not internal): DepartmentsController — which ASP.NET Core requires to be a
/// public class with a public constructor for controller discovery/DI activation — takes
/// this as a constructor dependency; a public constructor cannot have an internal parameter
/// type (CS0051). Interface and implementation share this file, matching IShiftService.
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
            return Result<DepartmentResponse>.Failure(HRErrorCodes.DuplicateCode, $"Department code '{request.Code}' is already in use.");
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
            return Result<DepartmentResponse>.Failure(HRErrorCodes.NotFound, $"Department '{id}' was not found.");
        }

        department.Update(request.Name, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DepartmentResponse>.Success(department.ToResponse());
    }

    public async Task<Result<DepartmentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var department = await _repository.GetByIdAsync(id, cancellationToken);
        return department is null
            ? Result<DepartmentResponse>.Failure(HRErrorCodes.NotFound, $"Department '{id}' was not found.")
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
            return Result.Failure(HRErrorCodes.NotFound, $"Department '{id}' was not found.");
        }

        department.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
