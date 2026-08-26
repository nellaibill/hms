using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): AppointmentTypesController requires a public constructor dependency
/// (CS0051 otherwise). Interface and implementation share this file, matching the other
/// Masters entities' {Entity}Service.cs convention.
/// </summary>
public interface IAppointmentTypeService
{
    Task<Result<AppointmentTypeResponse>> CreateAsync(CreateAppointmentTypeRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<AppointmentTypeResponse>> UpdateAsync(Guid id, UpdateAppointmentTypeRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<AppointmentTypeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<AppointmentTypeResponse>> GetPagedAsync(AppointmentTypeListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class AppointmentTypeService : IAppointmentTypeService
{
    private readonly IAppointmentTypeRepository _repository;

    public AppointmentTypeService(IAppointmentTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<AppointmentTypeResponse>> CreateAsync(CreateAppointmentTypeRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByNameAsync(request.Name.Trim(), excludingId: null, cancellationToken))
        {
            return Result<AppointmentTypeResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Appointment type name '{request.Name}' is already in use.");
        }

        var appointmentType = AppointmentType.Create(request.Name, request.IsActive, actorId);

        await _repository.AddAsync(appointmentType, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<AppointmentTypeResponse>.Success(appointmentType.ToResponse());
    }

    public async Task<Result<AppointmentTypeResponse>> UpdateAsync(Guid id, UpdateAppointmentTypeRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var appointmentType = await _repository.GetByIdAsync(id, cancellationToken);
        if (appointmentType is null)
        {
            return Result<AppointmentTypeResponse>.Failure(MastersErrorCodes.NotFound, $"Appointment type '{id}' was not found.");
        }

        // Name is now the unique natural key (Code is gone) — an update can collide with
        // another record the way Create always could, so it needs the same guard; without
        // this, a rename would surface as a raw unhandled DB constraint violation instead of
        // a clean validation error.
        if (await _repository.ExistsByNameAsync(request.Name.Trim(), excludingId: id, cancellationToken))
        {
            return Result<AppointmentTypeResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Appointment type name '{request.Name}' is already in use.");
        }

        appointmentType.Update(request.Name, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<AppointmentTypeResponse>.Success(appointmentType.ToResponse());
    }

    public async Task<Result<AppointmentTypeResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var appointmentType = await _repository.GetByIdAsync(id, cancellationToken);
        return appointmentType is null
            ? Result<AppointmentTypeResponse>.Failure(MastersErrorCodes.NotFound, $"Appointment type '{id}' was not found.")
            : Result<AppointmentTypeResponse>.Success(appointmentType.ToResponse());
    }

    public async Task<PagedResult<AppointmentTypeResponse>> GetPagedAsync(AppointmentTypeListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<AppointmentTypeResponse>(items.Select(a => a.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var appointmentType = await _repository.GetByIdAsync(id, cancellationToken);
        if (appointmentType is null)
        {
            return Result.Failure(MastersErrorCodes.NotFound, $"Appointment type '{id}' was not found.");
        }

        appointmentType.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
