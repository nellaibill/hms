using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Application.Mapping;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.HR.Application;

/// <summary>
/// Public (not internal): WeeklyRostersController — which ASP.NET Core requires to be a
/// public class with a public constructor for controller discovery/DI activation — takes
/// this as a constructor dependency; a public constructor cannot have an internal parameter
/// type (CS0051). Interface and implementation share this file, matching IShiftService.
/// </summary>
public interface IWeeklyRosterService
{
    Task<Result<WeeklyRosterResponse>> CreateAsync(CreateWeeklyRosterRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<WeeklyRosterResponse>> UpdateAsync(Guid id, UpdateWeeklyRosterRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<WeeklyRosterResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<WeeklyRosterResponse>> GetPagedAsync(WeeklyRosterListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<WeeklyRosterResponse>> PublishAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<WeeklyRosterResponse>> CopyAsync(Guid id, CopyWeeklyRosterRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<PagedResult<WeeklyRosterResponse>> GetForMonthAsync(MonthlyWeeklyRosterQuery query, CancellationToken cancellationToken);
}

internal class WeeklyRosterService : IWeeklyRosterService
{
    private readonly IWeeklyRosterRepository _repository;

    public WeeklyRosterService(IWeeklyRosterRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<WeeklyRosterResponse>> CreateAsync(CreateWeeklyRosterRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var weeklyRoster = WeeklyRoster.Create(request.WeekStartDate, request.DepartmentId, request.Published, request.PublishedDate, actorId);

        await _repository.AddAsync(weeklyRoster, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<WeeklyRosterResponse>.Success(weeklyRoster.ToResponse());
    }

    public async Task<Result<WeeklyRosterResponse>> UpdateAsync(Guid id, UpdateWeeklyRosterRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var weeklyRoster = await _repository.GetByIdAsync(id, cancellationToken);
        if (weeklyRoster is null)
        {
            return Result<WeeklyRosterResponse>.Failure(HRErrorCodes.NotFound, $"Weekly roster '{id}' was not found.");
        }

        weeklyRoster.Update(request.WeekStartDate, request.DepartmentId, request.Published, request.PublishedDate, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<WeeklyRosterResponse>.Success(weeklyRoster.ToResponse());
    }

    public async Task<Result<WeeklyRosterResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var weeklyRoster = await _repository.GetByIdAsync(id, cancellationToken);
        return weeklyRoster is null
            ? Result<WeeklyRosterResponse>.Failure(HRErrorCodes.NotFound, $"Weekly roster '{id}' was not found.")
            : Result<WeeklyRosterResponse>.Success(weeklyRoster.ToResponse());
    }

    public async Task<PagedResult<WeeklyRosterResponse>> GetPagedAsync(WeeklyRosterListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<WeeklyRosterResponse>(items.Select(w => w.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var weeklyRoster = await _repository.GetByIdAsync(id, cancellationToken);
        if (weeklyRoster is null)
        {
            return Result.Failure(HRErrorCodes.NotFound, $"Weekly roster '{id}' was not found.");
        }

        weeklyRoster.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<WeeklyRosterResponse>> PublishAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var weeklyRoster = await _repository.GetByIdAsync(id, cancellationToken);
        if (weeklyRoster is null)
        {
            return Result<WeeklyRosterResponse>.Failure(HRErrorCodes.NotFound, $"Weekly roster '{id}' was not found.");
        }

        weeklyRoster.Publish(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<WeeklyRosterResponse>.Success(weeklyRoster.ToResponse());
    }

    // Copy simply duplicates the roster's metadata (DepartmentId) onto a new record for the
    // caller-chosen TargetWeekStartDate. Deliberately does not carry over Published/
    // PublishedDate — a copy is always a fresh, unpublished draft. Does not touch
    // ShiftAssignments at all, per the Phase 6 spec.
    public async Task<Result<WeeklyRosterResponse>> CopyAsync(Guid id, CopyWeeklyRosterRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var source = await _repository.GetByIdAsync(id, cancellationToken);
        if (source is null)
        {
            return Result<WeeklyRosterResponse>.Failure(HRErrorCodes.NotFound, $"Weekly roster '{id}' was not found.");
        }

        var copy = WeeklyRoster.Create(request.TargetWeekStartDate, source.DepartmentId, published: false, publishedDate: null, actorId);

        await _repository.AddAsync(copy, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<WeeklyRosterResponse>.Success(copy.ToResponse());
    }

    public async Task<PagedResult<WeeklyRosterResponse>> GetForMonthAsync(MonthlyWeeklyRosterQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetForMonthAsync(query, cancellationToken);
        return new PagedResult<WeeklyRosterResponse>(items.Select(w => w.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }
}
