using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class MedicationOrderRepository : IMedicationOrderRepository
{
    private readonly IPDDbContext _dbContext;

    public MedicationOrderRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(MedicationOrder order, CancellationToken cancellationToken)
        => await _dbContext.MedicationOrders.AddAsync(order, cancellationToken);

    public async Task<MedicationOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.MedicationOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MedicationOrder>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
        => await _dbContext.MedicationOrders
            .Where(o => o.AdmissionId == admissionId)
            .OrderByDescending(o => o.OrderedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
