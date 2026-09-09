using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class AdmissionAdvanceRepository : IAdmissionAdvanceRepository
{
    private readonly IPDDbContext _dbContext;

    public AdmissionAdvanceRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AdmissionAdvance advance, CancellationToken cancellationToken)
        => await _dbContext.AdmissionAdvances.AddAsync(advance, cancellationToken);

    public async Task<IReadOnlyList<AdmissionAdvance>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
        => await _dbContext.AdmissionAdvances
            .Where(a => a.AdmissionId == admissionId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
