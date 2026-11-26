using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
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

    private readonly IActivityLogService _activityLog;

    public DiagnosticPackageService(IDiagnosticPackageRepository repository, IDiagnosticServiceService diagnosticServiceService, IActivityLogService activityLog)
    {
        _activityLog = activityLog;
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
            request.CostPrice,
            request.IsActive,
            request.ServiceIds,
            actorId);
        package.SetSortOrder(request.SortOrder);

        await _repository.AddAsync(package, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var created = package.ToResponse();
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Create, "DiagnosticPackage", created.Id, created.Name,
            $"Added lab package '{created.Name}' — price {MastersActivityLog.Money(created.TotalPrice)}, running cost {PackageCost(created.CostPrice)}",
            null, created, actorId), cancellationToken);

        return Result<DiagnosticPackageResponse>.Success(created);
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

        var before = package.ToResponse();
        package.Update(request.Code, request.Name, request.Description, request.TotalPrice, request.CostPrice, request.IsActive, actorId);
        package.SetSortOrder(request.SortOrder);
        await _repository.SaveChangesAsync(cancellationToken);

        var after = package.ToResponse();
        var changes = new List<string>();
        if (before.TotalPrice != after.TotalPrice) changes.Add($"price {MastersActivityLog.Money(before.TotalPrice)} → {MastersActivityLog.Money(after.TotalPrice)}");
        if (before.CostPrice != after.CostPrice) changes.Add($"running cost {PackageCost(before.CostPrice)} → {PackageCost(after.CostPrice)}");
        if (before.Name != after.Name) changes.Add($"renamed from '{before.Name}'");
        changes.Add(MastersActivityLog.Status(before.IsActive, after.IsActive));
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Update, "DiagnosticPackage", after.Id, after.Name,
            $"Updated lab package '{after.Name}'" + MastersActivityLog.Join(changes), before, after, actorId), cancellationToken);

        return Result<DiagnosticPackageResponse>.Success(after);
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

        var deleted = package.ToResponse();
        package.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Delete, "DiagnosticPackage", deleted.Id, deleted.Name,
            $"Deleted lab package '{deleted.Name}'", deleted, null, actorId), cancellationToken);

        return Result.Success();
    }

    public async Task<Result<DiagnosticPackageResponse>> AddItemAsync(Guid id, AddDiagnosticPackageItemRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var package = await _repository.GetByIdAsync(id, cancellationToken);
        if (package is null)
        {
            return Result<DiagnosticPackageResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic package '{id}' was not found.");
        }

        var service = await _diagnosticServiceService.GetByIdAsync(request.ServiceId, cancellationToken);
        if (!service.IsSuccess)
        {
            return Result<DiagnosticPackageResponse>.Failure(MastersErrorCodes.InvalidPackageItemService, $"Diagnostic service '{request.ServiceId}' was not found.");
        }

        package.AddItem(request.ServiceId, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        var after = package.ToResponse();
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Update, "DiagnosticPackage", after.Id, after.Name,
            $"Added test '{service.Value!.Name}' to lab package '{after.Name}'", null, after, actorId), cancellationToken);

        return Result<DiagnosticPackageResponse>.Success(after);
    }

    public async Task<Result<DiagnosticPackageResponse>> RemoveItemAsync(Guid id, Guid itemId, Guid? actorId, CancellationToken cancellationToken)
    {
        var package = await _repository.GetByIdAsync(id, cancellationToken);
        if (package is null)
        {
            return Result<DiagnosticPackageResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic package '{id}' was not found.");
        }

        var removedServiceId = package.Items.FirstOrDefault(item => item.Id == itemId)?.ServiceId;
        if (!package.RemoveItem(itemId, actorId))
        {
            return Result<DiagnosticPackageResponse>.Failure(MastersErrorCodes.NotFound, $"Package item '{itemId}' was not found on this package.");
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var after = package.ToResponse();
        var removed = removedServiceId is { } serviceId ? await _diagnosticServiceService.GetByIdAsync(serviceId, cancellationToken) : null;
        var testName = removed is { IsSuccess: true } ? removed.Value!.Name : "a test";
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Update, "DiagnosticPackage", after.Id, after.Name,
            $"Removed test '{testName}' from lab package '{after.Name}'", null, after, actorId), cancellationToken);

        return Result<DiagnosticPackageResponse>.Success(after);
    }

    /// <summary>A package's running cost for the log: its own figure, or "tests' total" for 0
    /// (ADR-139/140).</summary>
    private static string PackageCost(decimal costPrice)
        => costPrice > 0 ? MastersActivityLog.Money(costPrice) : "tests' total";
}
