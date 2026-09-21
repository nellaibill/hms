using HMS.Modules.Radiology.Application.Abstractions;
using HMS.Modules.Radiology.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Radiology.Infrastructure.Repositories;

internal class XrayAiAnalysisRepository : IXrayAiAnalysisRepository
{
    private readonly RadiologyDbContext _dbContext;

    public XrayAiAnalysisRepository(RadiologyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(XrayAiAnalysis analysis, CancellationToken cancellationToken)
        => await _dbContext.XrayAiAnalyses.AddAsync(analysis, cancellationToken);

    public Task<XrayAiAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.XrayAiAnalyses.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<XrayAiAnalysis>> ListByDocumentIdsAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken)
        => await _dbContext.XrayAiAnalyses
            .Where(a => documentIds.Contains(a.DocumentId))
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
