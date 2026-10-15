using HMS.Modules.Appointments.Contracts;
using HMS.Modules.Appointments.Domain;

namespace HMS.Modules.Appointments.Application.Abstractions;

internal interface IConsultantBlockRepository
{
    Task AddAsync(ConsultantBlock block, CancellationToken cancellationToken);

    Task<ConsultantBlock?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Blocks of one consultant that overlap the date range (inclusive).</summary>
    Task<IReadOnlyList<ConsultantBlock>> GetOverlappingAsync(Guid consultantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConsultantBlock>> ListAsync(ConsultantBlockListQuery query, int maxRows, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
