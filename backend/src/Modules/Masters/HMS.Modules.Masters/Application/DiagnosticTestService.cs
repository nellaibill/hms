using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
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

    private readonly IActivityLogService _activityLog;

    public DiagnosticTestService(IDiagnosticTestRepository repository, IActivityLogService activityLog)
    {
        _activityLog = activityLog;
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
            request.CostPrice,
            request.IsOutsourced,
            request.ReferenceLab,
            request.IsActive,
            actorId);
        diagnosticTest.SetSortOrder(request.SortOrder);

        await _repository.AddAsync(diagnosticTest, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var created = diagnosticTest.ToResponse();
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Create, "DiagnosticTest", created.Id, created.Name,
            $"Added {created.ServiceType.ToString().ToLowerInvariant()} '{created.Name}' — price {MastersActivityLog.Money(created.Price)}, cost {MastersActivityLog.Money(created.CostPrice)}",
            null, created, actorId), cancellationToken);

        return Result<DiagnosticTestResponse>.Success(created);
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

        var before = diagnosticTest.ToResponse();
        diagnosticTest.Update(
            request.Name,
            request.ServiceType,
            request.Category,
            request.Price,
            request.CostPrice,
            request.IsOutsourced,
            request.ReferenceLab,
            request.IsActive,
            actorId);
        diagnosticTest.SetSortOrder(request.SortOrder);
        await _repository.SaveChangesAsync(cancellationToken);

        var after = diagnosticTest.ToResponse();
        var changes = new List<string>();
        var amounts = MastersActivityLog.Changes(("price", before.Price, after.Price), ("cost", before.CostPrice, after.CostPrice));
        if (amounts.Length > 0) changes.Add(amounts[3..]);
        if (before.Name != after.Name) changes.Add($"renamed from '{before.Name}'");
        changes.Add(MastersActivityLog.Status(before.IsActive, after.IsActive));
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Update, "DiagnosticTest", after.Id, after.Name,
            $"Updated {after.ServiceType.ToString().ToLowerInvariant()} '{after.Name}'" + MastersActivityLog.Join(changes), before, after, actorId), cancellationToken);

        return Result<DiagnosticTestResponse>.Success(after);
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

        var deleted = diagnosticTest.ToResponse();
        diagnosticTest.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Delete, "DiagnosticTest", deleted.Id, deleted.Name,
            $"Deleted {deleted.ServiceType.ToString().ToLowerInvariant()} '{deleted.Name}'", deleted, null, actorId), cancellationToken);

        return Result.Success();
    }
}
