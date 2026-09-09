using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class DoctorOrderRepository : IDoctorOrderRepository
{
    private readonly IPDDbContext _dbContext;

    public DoctorOrderRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DoctorOrder order, CancellationToken cancellationToken)
        => await _dbContext.DoctorOrders.AddAsync(order, cancellationToken);

    public async Task<DoctorOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.DoctorOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DoctorOrder>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
        => await _dbContext.DoctorOrders
            .Where(o => o.AdmissionId == admissionId)
            .OrderByDescending(o => o.OrderedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
