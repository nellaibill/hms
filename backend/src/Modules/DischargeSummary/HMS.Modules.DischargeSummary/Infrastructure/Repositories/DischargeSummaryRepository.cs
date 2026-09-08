using HMS.Modules.DischargeSummary.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.DischargeSummary.Infrastructure.Repositories;

internal class DischargeSummaryRepository : IDischargeSummaryRepository
{
    private readonly DischargeSummaryDbContext _dbContext;

    public DischargeSummaryRepository(DischargeSummaryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Domain.DischargeSummary summary, CancellationToken cancellationToken)
        => await _dbContext.DischargeSummaries.AddAsync(summary, cancellationToken);

    public Task<Domain.DischargeSummary?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.DischargeSummaries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Domain.DischargeSummary?> GetByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken)
        => _dbContext.DischargeSummaries.FirstOrDefaultAsync(x => x.AdmissionId == admissionId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
