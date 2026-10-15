using HMS.Modules.Appointments.Application.Abstractions;
using HMS.Modules.Appointments.Contracts;
using HMS.Modules.Appointments.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Appointments.Infrastructure.Repositories;

internal class ConsultantBlockRepository : IConsultantBlockRepository
{
    private readonly AppointmentsDbContext _dbContext;

    public ConsultantBlockRepository(AppointmentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ConsultantBlock block, CancellationToken cancellationToken)
        => await _dbContext.ConsultantBlocks.AddAsync(block, cancellationToken);

    public Task<ConsultantBlock?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ConsultantBlocks.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ConsultantBlock>> GetOverlappingAsync(Guid consultantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
        => await _dbContext.ConsultantBlocks
            .AsNoTracking()
            .Where(b => b.ConsultantId == consultantId && b.FromDate <= toDate && b.ToDate >= fromDate)
            .OrderBy(b => b.FromDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ConsultantBlock>> ListAsync(ConsultantBlockListQuery query, int maxRows, CancellationToken cancellationToken)
    {
        var blocks = _dbContext.ConsultantBlocks.AsNoTracking();

        if (query.ConsultantId.HasValue)
        {
            blocks = blocks.Where(b => b.ConsultantId == query.ConsultantId.Value);
        }

        // Overlap, not containment: a block that started before FromDate but is still running
        // belongs in the list.
        if (query.FromDate.HasValue)
        {
            blocks = blocks.Where(b => b.ToDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            blocks = blocks.Where(b => b.FromDate <= query.ToDate.Value);
        }

        return await blocks.OrderBy(b => b.FromDate).ThenBy(b => b.ConsultantId).Take(maxRows).ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
