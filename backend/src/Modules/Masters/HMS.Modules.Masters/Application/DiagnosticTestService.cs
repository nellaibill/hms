using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): DiagnosticTestsController requires a public constructor
/// dependency (CS0051 otherwise). Interface and implementation share this file, matching
/// ConsultationTypeService's convention.
/// </summary>
public interface IDiagnosticTestService
{
    Task<Result<DiagnosticTestResponse>> CreateAsync(CreateDiagnosticTestRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosticTestResponse>> UpdateAsync(Guid id, UpdateDiagnosticTestRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosticTestResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<DiagnosticTestResponse>> GetPagedAsync(DiagnosticTestListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class DiagnosticTestService : IDiagnosticTestService
{
    private readonly IDiagnosticTestRepository _repository;

    public DiagnosticTestService(IDiagnosticTestRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DiagnosticTestResponse>> CreateAsync(CreateDiagnosticTestRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByNameAsync(request.Name.Trim(), request.ServiceType, request.IsOutsourced, excludingId: null, cancellationToken))
        {
            return Result<DiagnosticTestResponse>.Failure(MastersErrorCodes.DuplicateCode, $"A {request.ServiceType} test named '{request.Name}' already exists for this outsourcing setting.");
        }

        var diagnosticTest = DiagnosticTest.Create(
            request.Name,
            request.ServiceType,
            request.Category,
            request.Price,
            request.IsOutsourced,
            request.ReferenceLab,
            request.IsActive,
            actorId);

        await _repository.AddAsync(diagnosticTest, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DiagnosticTestResponse>.Success(diagnosticTest.ToResponse());
    }

    public async Task<Result<DiagnosticTestResponse>> UpdateAsync(Guid id, UpdateDiagnosticTestRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var diagnosticTest = await _repository.GetByIdAsync(id, cancellationToken);
        if (diagnosticTest is null)
        {
            return Result<DiagnosticTestResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic test '{id}' was not found.");
        }

        if (await _repository.ExistsByNameAsync(request.Name.Trim(), request.ServiceType, request.IsOutsourced, excludingId: id, cancellationToken))
        {
            return Result<DiagnosticTestResponse>.Failure(MastersErrorCodes.DuplicateCode, $"A {request.ServiceType} test named '{request.Name}' already exists for this outsourcing setting.");
        }

        diagnosticTest.Update(
            request.Name,
            request.ServiceType,
            request.Category,
            request.Price,
            request.IsOutsourced,
            request.ReferenceLab,
            request.IsActive,
            actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DiagnosticTestResponse>.Success(diagnosticTest.ToResponse());
    }

    public async Task<Result<DiagnosticTestResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var diagnosticTest = await _repository.GetByIdAsync(id, cancellationToken);
        return diagnosticTest is null
            ? Result<DiagnosticTestResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic test '{id}' was not found.")
            : Result<DiagnosticTestResponse>.Success(diagnosticTest.ToResponse());
    }

    public async Task<PagedResult<DiagnosticTestResponse>> GetPagedAsync(DiagnosticTestListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<DiagnosticTestResponse>(items.Select(d => d.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var diagnosticTest = await _repository.GetByIdAsync(id, cancellationToken);
        if (diagnosticTest is null)
        {
            return Result.Failure(MastersErrorCodes.NotFound, $"Diagnostic test '{id}' was not found.");
        }

        diagnosticTest.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
