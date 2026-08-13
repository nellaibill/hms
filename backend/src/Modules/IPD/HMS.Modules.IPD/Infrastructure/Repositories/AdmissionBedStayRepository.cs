using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class AdmissionBedStayRepository : IAdmissionBedStayRepository
{
    private readonly IPDDbContext _dbContext;

    public AdmissionBedStayRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AdmissionBedStay stay, CancellationToken cancellationToken)
        => await _dbContext.AdmissionBedStays.AddAsync(stay, cancellationToken);

    public async Task<AdmissionBedStay?> GetActiveByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
        => await _dbContext.AdmissionBedStays
            .Where(s => s.AdmissionId == admissionId && s.ToDateTime == null)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AdmissionBedStay>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
        => await _dbContext.AdmissionBedStays
            .Where(s => s.AdmissionId == admissionId)
            .OrderBy(s => s.FromDateTime)
            .ToListAsync(cancellationToken);
}
