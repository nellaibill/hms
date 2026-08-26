using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IAppointmentTypeRepository
{
    Task AddAsync(AppointmentType appointmentType, CancellationToken cancellationToken);

    Task<AppointmentType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<AppointmentType> Items, int TotalCount)> GetPagedAsync(AppointmentTypeListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
