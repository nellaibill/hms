using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class AdmissionChargeRepository : IAdmissionChargeRepository
{
    private readonly IPDDbContext _dbContext;

    public AdmissionChargeRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AdmissionCharge charge, CancellationToken cancellationToken)
        => await _dbContext.AdmissionCharges.AddAsync(charge, cancellationToken);

    public async Task<IReadOnlyList<AdmissionCharge>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
        => await _dbContext.AdmissionCharges
            .Where(c => c.AdmissionId == admissionId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
