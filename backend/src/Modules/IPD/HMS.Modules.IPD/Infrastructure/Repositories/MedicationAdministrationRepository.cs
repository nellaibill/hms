using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class MedicationAdministrationRepository : IMedicationAdministrationRepository
{
    private readonly IPDDbContext _dbContext;

    public MedicationAdministrationRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(MedicationAdministration administration, CancellationToken cancellationToken)
        => await _dbContext.MedicationAdministrations.AddAsync(administration, cancellationToken);

    public async Task<IReadOnlyList<MedicationAdministration>> GetByMedicationOrderIdAsync(Guid medicationOrderId, CancellationToken cancellationToken)
        => await _dbContext.MedicationAdministrations
            .Where(a => a.MedicationOrderId == medicationOrderId)
            .OrderByDescending(a => a.ScheduledTime)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
