using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): DiagnosticServicesController requires a public constructor
/// dependency (CS0051 otherwise); DiagnosticPackageService also depends on this to validate
/// each DiagnosticPackageItem.ServiceId. Interface and implementation share this file, matching
/// DiagnosticTestService's convention.
/// </summary>
public interface IDiagnosticServiceService
{
    Task<Result<DiagnosticServiceResponse>> CreateAsync(CreateDiagnosticServiceRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosticServiceResponse>> UpdateAsync(Guid id, UpdateDiagnosticServiceRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosticServiceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<DiagnosticServiceResponse>> GetPagedAsync(DiagnosticServiceListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class DiagnosticServiceService : IDiagnosticServiceService
{
    private readonly IDiagnosticServiceRepository _repository;
    private readonly IDiagnosticCategoryService _categoryService;
    private readonly IDiagnosticProviderService _providerService;

    private readonly IActivityLogService _activityLog;

    public DiagnosticServiceService(
        IDiagnosticServiceRepository repository,
        IDiagnosticCategoryService categoryService,
        IDiagnosticProviderService providerService,
        IActivityLogService activityLog)
    {
        _activityLog = activityLog;
        _repository = repository;
        _categoryService = categoryService;
        _providerService = providerService;
    }

    public async Task<Result<DiagnosticServiceResponse>> CreateAsync(CreateDiagnosticServiceRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.Code.Trim(), excludingId: null, cancellationToken))
        {
            return Result<DiagnosticServiceResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Service code '{request.Code}' is already in use.");
        }

        var referenceError = await ValidateReferencesAsync(request.CategoryId, request.IsOutsourced, request.ProviderId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<DiagnosticServiceResponse>.Failure(referenceError.ErrorCode!, referenceError.Error!);
        }

        var diagnosticService = DiagnosticService.Create(
            request.Code,
            request.Name,
            request.CategoryId,
            request.ServiceType,
            request.IsOutsourced,
            request.ProviderId,
            request.Price,
            request.CostPrice,
            request.IsActive,
            actorId);
        diagnosticService.SetTariff(request.DoctorCharge, request.BedsidePrice, request.BedsideDoctorCharge, ToRateOptionValues(request.RateOptions));

        await _repository.AddAsync(diagnosticService, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var created = diagnosticService.ToResponse();
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Create, "DiagnosticService", created.Id, created.Name,
            $"Added {Kind(created)} '{created.Name}' — price {MastersActivityLog.Money(created.Price)}"
            + (created.ServiceType == DiagnosticTestServiceType.Radiology ? $", doctor's share {MastersActivityLog.Money(created.DoctorCharge)}" : string.Empty)
            + $", running cost {MastersActivityLog.Money(created.CostPrice)}",
            null, created, actorId), cancellationToken);

        return Result<DiagnosticServiceResponse>.Success(created);
    }

    public async Task<Result<DiagnosticServiceResponse>> UpdateAsync(Guid id, UpdateDiagnosticServiceRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var diagnosticService = await _repository.GetByIdAsync(id, cancellationToken);
        if (diagnosticService is null)
        {
            return Result<DiagnosticServiceResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic service '{id}' was not found.");
        }

        if (await _repository.ExistsByCodeAsync(request.Code.Trim(), excludingId: id, cancellationToken))
        {
            return Result<DiagnosticServiceResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Service code '{request.Code}' is already in use.");
        }

        var referenceError = await ValidateReferencesAsync(request.CategoryId, request.IsOutsourced, request.ProviderId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<DiagnosticServiceResponse>.Failure(referenceError.ErrorCode!, referenceError.Error!);
        }

        var before = diagnosticService.ToResponse();
        diagnosticService.Update(
            request.Code,
            request.Name,
            request.CategoryId,
            request.ServiceType,
            request.IsOutsourced,
            request.ProviderId,
            request.Price,
            request.CostPrice,
            request.IsActive,
            actorId);
        diagnosticService.SetTariff(request.DoctorCharge, request.BedsidePrice, request.BedsideDoctorCharge, ToRateOptionValues(request.RateOptions));
        await _repository.SaveChangesAsync(cancellationToken);

        var after = diagnosticService.ToResponse();
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Update, "DiagnosticService", after.Id, after.Name,
            $"Updated {Kind(after)} '{after.Name}'" + DescribeChanges(before, after), before, after, actorId), cancellationToken);

        return Result<DiagnosticServiceResponse>.Success(after);
    }

    public async Task<Result<DiagnosticServiceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var diagnosticService = await _repository.GetByIdAsync(id, cancellationToken);
        return diagnosticService is null
            ? Result<DiagnosticServiceResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic service '{id}' was not found.")
            : Result<DiagnosticServiceResponse>.Success(diagnosticService.ToResponse());
    }

    public async Task<PagedResult<DiagnosticServiceResponse>> GetPagedAsync(DiagnosticServiceListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<DiagnosticServiceResponse>(items.Select(d => d.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var diagnosticService = await _repository.GetByIdAsync(id, cancellationToken);
        if (diagnosticService is null)
        {
            return Result.Failure(MastersErrorCodes.NotFound, $"Diagnostic service '{id}' was not found.");
        }

        var deleted = diagnosticService.ToResponse();
        diagnosticService.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Delete, "DiagnosticService", deleted.Id, deleted.Name,
            $"Deleted {Kind(deleted)} '{deleted.Name}'", deleted, null, actorId), cancellationToken);

        return Result.Success();
    }

    /// <summary>Cross-entity references stay app-level-only (no DB FK) — validated here via
    /// the sibling services' own GetByIdAsync, same pattern PatientService uses for its
    /// State/District references.</summary>
    private static string Kind(DiagnosticServiceResponse service)
        => service.ServiceType == DiagnosticTestServiceType.Radiology ? "radiology service" : service.IsOutsourced ? "outsourced lab test" : "lab test";

    /// <summary>" — price ₹50 → ₹60, rate options changed, deactivated" — what an update changed.</summary>
    private static string DescribeChanges(DiagnosticServiceResponse before, DiagnosticServiceResponse after)
    {
        var parts = new List<string>();
        var amounts = MastersActivityLog.Changes(
            ("price", before.Price, after.Price),
            ("doctor's share", before.DoctorCharge, after.DoctorCharge),
            ("running cost", before.CostPrice, after.CostPrice),
            ("bedside price", before.BedsidePrice, after.BedsidePrice),
            ("bedside doctor's share", before.BedsideDoctorCharge, after.BedsideDoctorCharge));
        if (amounts.Length > 0) parts.Add(amounts[3..]);
        static string Options(DiagnosticServiceResponse service) => string.Join("|", service.RateOptions.Select(o => $"{o.Name}:{o.Price}:{o.DoctorCharge}:{o.CostPrice}"));
        if (Options(before) != Options(after)) parts.Add("rate options changed");
        if (before.Name != after.Name) parts.Add($"renamed from '{before.Name}'");
        parts.Add(MastersActivityLog.Status(before.IsActive, after.IsActive));
        return MastersActivityLog.Join(parts);
    }

    private static List<RateOptionValues> ToRateOptionValues(IReadOnlyList<DiagnosticServiceRateOptionDto> options)
        => options.Select(option => new RateOptionValues(option.Id, option.Name, option.Price, option.DoctorCharge, option.CostPrice)).ToList();

    private async Task<Result?> ValidateReferencesAsync(Guid categoryId, bool isOutsourced, Guid? providerId, CancellationToken cancellationToken)
    {
        if (!(await _categoryService.GetByIdAsync(categoryId, cancellationToken)).IsSuccess)
        {
            return Result.Failure(MastersErrorCodes.InvalidCategory, $"Category '{categoryId}' was not found.");
        }

        if (isOutsourced)
        {
            if (!providerId.HasValue)
            {
                return Result.Failure(MastersErrorCodes.InvalidProvider, "A provider is required when the service is outsourced.");
            }

            if (!(await _providerService.GetByIdAsync(providerId.Value, cancellationToken)).IsSuccess)
            {
                return Result.Failure(MastersErrorCodes.InvalidProvider, $"Provider '{providerId}' was not found.");
            }
        }

        return null;
    }
}
