using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class StateRepository : IStateRepository
{
    private readonly MastersDbContext _dbContext;

    public StateRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<State>> GetAllAsync(CancellationToken cancellationToken)
        => await _dbContext.States.OrderBy(s => s.Name).ToListAsync(cancellationToken);
}
