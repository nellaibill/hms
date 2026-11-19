using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): ConsultationTypesController requires a public constructor
/// dependency (CS0051 otherwise). Interface and implementation share this file, matching
/// AppointmentTypeService's convention.
/// </summary>
public interface IConsultationTypeService
{
    Task<Result<ConsultationTypeResponse>> CreateAsync(CreateConsultationTypeRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ConsultationTypeResponse>> UpdateAsync(Guid id, UpdateConsultationTypeRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ConsultationTypeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ConsultationTypeResponse>> GetPagedAsync(ConsultationTypeListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class ConsultationTypeService : IConsultationTypeService
{
    private readonly IConsultationTypeRepository _repository;

    private readonly IActivityLogService _activityLog;

    public ConsultationTypeService(IConsultationTypeRepository repository, IActivityLogService activityLog)
    {
        _activityLog = activityLog;
        _repository = repository;
    }

    public async Task<Result<ConsultationTypeResponse>> CreateAsync(CreateConsultationTypeRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByNameAsync(request.Name.Trim(), excludingId: null, cancellationToken))
        {
            return Result<ConsultationTypeResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Consultation type name '{request.Name}' is already in use.");
        }

        var consultationType = ConsultationType.Create(request.Name, request.ShortName, request.Amount, request.IsActive, actorId);

        await _repository.AddAsync(consultationType, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var created = consultationType.ToResponse();
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Create, "ConsultationType", created.Id, created.Name,
            $"Added consultation type '{created.Name}' — hospital charge {MastersActivityLog.Money(created.Amount)}", null, created, actorId), cancellationToken);

        return Result<ConsultationTypeResponse>.Success(created);
    }

    public async Task<Result<ConsultationTypeResponse>> UpdateAsync(Guid id, UpdateConsultationTypeRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var consultationType = await _repository.GetByIdAsync(id, cancellationToken);
        if (consultationType is null)
        {
            return Result<ConsultationTypeResponse>.Failure(MastersErrorCodes.NotFound, $"Consultation type '{id}' was not found.");
        }

        // Name is now the unique natural key (Code is gone) — see AppointmentTypeService's
        // identical guard for why Update needs this the same way Create does.
        if (await _repository.ExistsByNameAsync(request.Name.Trim(), excludingId: id, cancellationToken))
        {
            return Result<ConsultationTypeResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Consultation type name '{request.Name}' is already in use.");
        }

        var before = consultationType.ToResponse();
        consultationType.Update(request.Name, request.ShortName, request.Amount, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        var after = consultationType.ToResponse();
        var changes = new List<string>();
        var amount = MastersActivityLog.Changes(("hospital charge", before.Amount, after.Amount));
        if (amount.Length > 0) changes.Add(amount[3..]);
        if (before.Name != after.Name) changes.Add($"renamed from '{before.Name}'");
        changes.Add(MastersActivityLog.Status(before.IsActive, after.IsActive));
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Update, "ConsultationType", after.Id, after.Name,
            $"Updated consultation type '{after.Name}'" + MastersActivityLog.Join(changes), before, after, actorId), cancellationToken);

        return Result<ConsultationTypeResponse>.Success(after);
    }

    public async Task<Result<ConsultationTypeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var consultationType = await _repository.GetByIdAsync(id, cancellationToken);
        return consultationType is null
            ? Result<ConsultationTypeResponse>.Failure(MastersErrorCodes.NotFound, $"Consultation type '{id}' was not found.")
            : Result<ConsultationTypeResponse>.Success(consultationType.ToResponse());
    }

    public async Task<PagedResult<ConsultationTypeResponse>> GetPagedAsync(ConsultationTypeListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<ConsultationTypeResponse>(items.Select(c => c.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var consultationType = await _repository.GetByIdAsync(id, cancellationToken);
        if (consultationType is null)
        {
            return Result.Failure(MastersErrorCodes.NotFound, $"Consultation type '{id}' was not found.");
        }

        var deleted = consultationType.ToResponse();
        consultationType.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);
        await _activityLog.LogAsync(MastersActivityLog.Request(ActivityLogActions.Delete, "ConsultationType", deleted.Id, deleted.Name,
            $"Deleted consultation type '{deleted.Name}'", deleted, null, actorId), cancellationToken);

        return Result.Success();
    }
}
