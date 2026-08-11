using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): ConsultantsController requires a public constructor dependency
/// (CS0051 otherwise). Interface and implementation share this file, matching the other
/// Masters entities' {Entity}Service.cs convention.
/// </summary>
public interface IConsultantService
{
    Task<Result<ConsultantResponse>> CreateAsync(CreateConsultantRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ConsultantResponse>> UpdateAsync(Guid id, UpdateConsultantRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ConsultantResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ConsultantResponse>> GetPagedAsync(ConsultantListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class ConsultantService : IConsultantService
{
    private readonly IConsultantRepository _repository;
    private readonly IDepartmentRepository _departmentRepository;

    public ConsultantService(IConsultantRepository repository, IDepartmentRepository departmentRepository)
    {
        _repository = repository;
        _departmentRepository = departmentRepository;
    }

    public async Task<Result<ConsultantResponse>> CreateAsync(CreateConsultantRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.Code.Trim().ToUpperInvariant(), excludingId: null, cancellationToken))
        {
            return Result<ConsultantResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Consultant code '{request.Code}' is already in use.");
        }

        if (request.DepartmentId.HasValue && !await _departmentRepository.ExistsAsync(request.DepartmentId.Value, cancellationToken))
        {
            return Result<ConsultantResponse>.Failure(MastersErrorCodes.InvalidReference, $"Department '{request.DepartmentId}' was not found.");
        }

        var consultant = Consultant.Create(request.Code, request.Name, request.DepartmentId, request.Specialization, request.IsActive, actorId);

        await _repository.AddAsync(consultant, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ConsultantResponse>.Success(consultant.ToResponse());
    }

    public async Task<Result<ConsultantResponse>> UpdateAsync(Guid id, UpdateConsultantRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var consultant = await _repository.GetByIdAsync(id, cancellationToken);
        if (consultant is null)
        {
            return Result<ConsultantResponse>.Failure(MastersErrorCodes.NotFound, $"Consultant '{id}' was not found.");
        }

        if (request.DepartmentId.HasValue && !await _departmentRepository.ExistsAsync(request.DepartmentId.Value, cancellationToken))
        {
            return Result<ConsultantResponse>.Failure(MastersErrorCodes.InvalidReference, $"Department '{request.DepartmentId}' was not found.");
        }

        consultant.Update(request.Name, request.DepartmentId, request.Specialization, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ConsultantResponse>.Success(consultant.ToResponse());
    }

    public async Task<Result<ConsultantResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var consultant = await _repository.GetByIdAsync(id, cancellationToken);
        return consultant is null
            ? Result<ConsultantResponse>.Failure(MastersErrorCodes.NotFound, $"Consultant '{id}' was not found.")
            : Result<ConsultantResponse>.Success(consultant.ToResponse());
    }

    public async Task<PagedResult<ConsultantResponse>> GetPagedAsync(ConsultantListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<ConsultantResponse>(items.Select(c => c.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var consultant = await _repository.GetByIdAsync(id, cancellationToken);
        if (consultant is null)
        {
            return Result.Failure(MastersErrorCodes.NotFound, $"Consultant '{id}' was not found.");
        }

        consultant.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
