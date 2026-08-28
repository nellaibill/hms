using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): DiagnosticPackagesController requires a public constructor
/// dependency (CS0051 otherwise). Interface and implementation share this file, matching
/// DiagnosticTestService's convention.
/// </summary>
public interface IDiagnosticPackageService
{
    Task<Result<DiagnosticPackageResponse>> CreateAsync(CreateDiagnosticPackageRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosticPackageResponse>> UpdateAsync(Guid id, UpdateDiagnosticPackageRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosticPackageResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<DiagnosticPackageResponse>> GetPagedAsync(DiagnosticPackageListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosticPackageResponse>> AddItemAsync(Guid id, AddDiagnosticPackageItemRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosticPackageResponse>> RemoveItemAsync(Guid id, Guid itemId, Guid? actorId, CancellationToken cancellationToken);
}

internal class DiagnosticPackageService : IDiagnosticPackageService
{
    private readonly IDiagnosticPackageRepository _repository;
    private readonly IDiagnosticServiceService _diagnosticServiceService;

    public DiagnosticPackageService(IDiagnosticPackageRepository repository, IDiagnosticServiceService diagnosticServiceService)
    {
        _repository = repository;
        _diagnosticServiceService = diagnosticServiceService;
    }

    public async Task<Result<DiagnosticPackageResponse>> CreateAsync(CreateDiagnosticPackageRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.Code.Trim(), excludingId: null, cancellationToken))
        {
            return Result<DiagnosticPackageResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Package code '{request.Code}' is already in use.");
        }

        foreach (var serviceId in request.ServiceIds)
        {
            if (!(await _diagnosticServiceService.GetByIdAsync(serviceId, cancellationToken)).IsSuccess)
            {
                return Result<DiagnosticPackageResponse>.Failure(MastersErrorCodes.InvalidPackageItemService, $"Diagnostic service '{serviceId}' was not found.");
            }
        }

        var package = DiagnosticPackage.Create(
            request.Code,
            request.Name,
            request.Description,
            request.TotalPrice,
            request.IsActive,
            request.ServiceIds,
            actorId);

        await _repository.AddAsync(package, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DiagnosticPackageResponse>.Success(package.ToResponse());
    }

    public async Task<Result<DiagnosticPackageResponse>> UpdateAsync(Guid id, UpdateDiagnosticPackageRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var package = await _repository.GetByIdAsync(id, cancellationToken);
        if (package is null)
        {
            return Result<DiagnosticPackageResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic package '{id}' was not found.");
        }

        if (await _repository.ExistsByCodeAsync(request.Code.Trim(), excludingId: id, cancellationToken))
        {
            return Result<DiagnosticPackageResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Package code '{request.Code}' is already in use.");
        }

        package.Update(request.Code, request.Name, request.Description, request.TotalPrice, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DiagnosticPackageResponse>.Success(package.ToResponse());
    }

    public async Task<Result<DiagnosticPackageResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var package = await _repository.GetByIdAsync(id, cancellationToken);
        return package is null
            ? Result<DiagnosticPackageResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic package '{id}' was not found.")
            : Result<DiagnosticPackageResponse>.Success(package.ToResponse());
    }

    public async Task<PagedResult<DiagnosticPackageResponse>> GetPagedAsync(DiagnosticPackageListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<DiagnosticPackageResponse>(items.Select(p => p.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var package = await _repository.GetByIdAsync(id, cancellationToken);
        if (package is null)
        {
            return Result.Failure(MastersErrorCodes.NotFound, $"Diagnostic package '{id}' was not found.");
        }

        package.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<DiagnosticPackageResponse>> AddItemAsync(Guid id, AddDiagnosticPackageItemRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var package = await _repository.GetByIdAsync(id, cancellationToken);
        if (package is null)
        {
            return Result<DiagnosticPackageResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic package '{id}' was not found.");
        }

        if (!(await _diagnosticServiceService.GetByIdAsync(request.ServiceId, cancellationToken)).IsSuccess)
        {
            return Result<DiagnosticPackageResponse>.Failure(MastersErrorCodes.InvalidPackageItemService, $"Diagnostic service '{request.ServiceId}' was not found.");
        }

        package.AddItem(request.ServiceId, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DiagnosticPackageResponse>.Success(package.ToResponse());
    }

    public async Task<Result<DiagnosticPackageResponse>> RemoveItemAsync(Guid id, Guid itemId, Guid? actorId, CancellationToken cancellationToken)
    {
        var package = await _repository.GetByIdAsync(id, cancellationToken);
        if (package is null)
        {
            return Result<DiagnosticPackageResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic package '{id}' was not found.");
        }

        if (!package.RemoveItem(itemId, actorId))
        {
            return Result<DiagnosticPackageResponse>.Failure(MastersErrorCodes.NotFound, $"Package item '{itemId}' was not found on this package.");
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DiagnosticPackageResponse>.Success(package.ToResponse());
    }
}
