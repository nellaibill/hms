using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): DiagnosticCategoriesController requires a public constructor
/// dependency (CS0051 otherwise); DiagnosticServiceService also depends on this to validate
/// DiagnosticService.CategoryId. Interface and implementation share this file, matching
/// DiagnosticTestService's convention.
/// </summary>
public interface IDiagnosticCategoryService
{
    Task<Result<DiagnosticCategoryResponse>> CreateAsync(CreateDiagnosticCategoryRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosticCategoryResponse>> UpdateAsync(Guid id, UpdateDiagnosticCategoryRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosticCategoryResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<DiagnosticCategoryResponse>> GetPagedAsync(DiagnosticCategoryListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class DiagnosticCategoryService : IDiagnosticCategoryService
{
    private readonly IDiagnosticCategoryRepository _repository;

    public DiagnosticCategoryService(IDiagnosticCategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DiagnosticCategoryResponse>> CreateAsync(CreateDiagnosticCategoryRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.Code.Trim(), excludingId: null, cancellationToken))
        {
            return Result<DiagnosticCategoryResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Category code '{request.Code}' is already in use.");
        }

        var category = DiagnosticCategory.Create(request.Code, request.Name, request.Description, request.IsActive, actorId);

        await _repository.AddAsync(category, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DiagnosticCategoryResponse>.Success(category.ToResponse());
    }

    public async Task<Result<DiagnosticCategoryResponse>> UpdateAsync(Guid id, UpdateDiagnosticCategoryRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return Result<DiagnosticCategoryResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic category '{id}' was not found.");
        }

        if (await _repository.ExistsByCodeAsync(request.Code.Trim(), excludingId: id, cancellationToken))
        {
            return Result<DiagnosticCategoryResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Category code '{request.Code}' is already in use.");
        }

        category.Update(request.Code, request.Name, request.Description, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DiagnosticCategoryResponse>.Success(category.ToResponse());
    }

    public async Task<Result<DiagnosticCategoryResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        return category is null
            ? Result<DiagnosticCategoryResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic category '{id}' was not found.")
            : Result<DiagnosticCategoryResponse>.Success(category.ToResponse());
    }

    public async Task<PagedResult<DiagnosticCategoryResponse>> GetPagedAsync(DiagnosticCategoryListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<DiagnosticCategoryResponse>(items.Select(c => c.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return Result.Failure(MastersErrorCodes.NotFound, $"Diagnostic category '{id}' was not found.");
        }

        category.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
