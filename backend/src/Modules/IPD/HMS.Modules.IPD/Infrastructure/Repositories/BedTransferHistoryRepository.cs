using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class BedTransferHistoryRepository : IBedTransferHistoryRepository
{
    private readonly IPDDbContext _dbContext;

    public BedTransferHistoryRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(BedTransferHistory history, CancellationToken cancellationToken)
        => await _dbContext.BedTransferHistories.AddAsync(history, cancellationToken);

    public async Task<IReadOnlyList<BedTransferHistory>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
        => await _dbContext.BedTransferHistories
            .Where(h => h.AdmissionId == admissionId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
}
