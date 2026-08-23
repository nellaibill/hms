using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class DistrictRepository : IDistrictRepository
{
    private readonly MastersDbContext _dbContext;

    public DistrictRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<District>> GetByStateIdAsync(Guid stateId, CancellationToken cancellationToken)
        => await _dbContext.Districts.Where(d => d.StateId == stateId).OrderBy(d => d.Name).ToListAsync(cancellationToken);
}
