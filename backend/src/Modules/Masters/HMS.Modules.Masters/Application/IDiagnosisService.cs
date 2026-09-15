using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): DiagnosesController requires a public constructor dependency
/// (CS0051 otherwise). Interface and implementation share this file, matching
/// ConsultationTypeService's convention.
/// </summary>
public interface IDiagnosisService
{
    Task<Result<DiagnosisResponse>> CreateAsync(CreateDiagnosisRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosisResponse>> UpdateAsync(Guid id, UpdateDiagnosisRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosisResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<DiagnosisResponse>> GetPagedAsync(DiagnosisListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class DiagnosisService : IDiagnosisService
{
    private readonly IDiagnosisRepository _repository;

    public DiagnosisService(IDiagnosisRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DiagnosisResponse>> CreateAsync(CreateDiagnosisRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByNameAsync(request.Name.Trim(), excludingId: null, cancellationToken))
        {
            return Result<DiagnosisResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Diagnosis name '{request.Name}' is already in use.");
        }

        var diagnosis = Diagnosis.Create(request.Name, request.IcdCode, request.IsActive, actorId);

        await _repository.AddAsync(diagnosis, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DiagnosisResponse>.Success(diagnosis.ToResponse());
    }

    public async Task<Result<DiagnosisResponse>> UpdateAsync(Guid id, UpdateDiagnosisRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var diagnosis = await _repository.GetByIdAsync(id, cancellationToken);
        if (diagnosis is null)
        {
            return Result<DiagnosisResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnosis '{id}' was not found.");
        }

        if (await _repository.ExistsByNameAsync(request.Name.Trim(), excludingId: id, cancellationToken))
        {
            return Result<DiagnosisResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Diagnosis name '{request.Name}' is already in use.");
        }

        diagnosis.Update(request.Name, request.IcdCode, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DiagnosisResponse>.Success(diagnosis.ToResponse());
    }

    public async Task<Result<DiagnosisResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var diagnosis = await _repository.GetByIdAsync(id, cancellationToken);
        return diagnosis is null
            ? Result<DiagnosisResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnosis '{id}' was not found.")
            : Result<DiagnosisResponse>.Success(diagnosis.ToResponse());
    }

    public async Task<PagedResult<DiagnosisResponse>> GetPagedAsync(DiagnosisListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<DiagnosisResponse>(items.Select(d => d.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var diagnosis = await _repository.GetByIdAsync(id, cancellationToken);
        if (diagnosis is null)
        {
            return Result.Failure(MastersErrorCodes.NotFound, $"Diagnosis '{id}' was not found.");
        }

        diagnosis.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
