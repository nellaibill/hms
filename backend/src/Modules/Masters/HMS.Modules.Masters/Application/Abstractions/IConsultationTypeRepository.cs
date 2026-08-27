using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;

namespace HMS.Modules.Masters.Application.Abstractions;

internal interface IConsultationTypeRepository
{
    Task AddAsync(ConsultationType consultationType, CancellationToken cancellationToken);

    Task<ConsultationType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ConsultationType> Items, int TotalCount)> GetPagedAsync(ConsultationTypeListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
