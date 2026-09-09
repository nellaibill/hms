using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class VitalsReadingRepository : IVitalsReadingRepository
{
    private readonly IPDDbContext _dbContext;

    public VitalsReadingRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(VitalsReading reading, CancellationToken cancellationToken)
        => await _dbContext.VitalsReadings.AddAsync(reading, cancellationToken);

    public async Task<IReadOnlyList<VitalsReading>> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
        => await _dbContext.VitalsReadings
            .Where(v => v.AdmissionId == admissionId)
            .OrderByDescending(v => v.RecordedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
