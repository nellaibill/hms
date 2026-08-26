using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class ConsultantRepository : IConsultantRepository
{
    private readonly MastersDbContext _dbContext;

    public ConsultantRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Consultant consultant, CancellationToken cancellationToken)
        => await _dbContext.Consultants.AddAsync(consultant, cancellationToken);

    public Task<Consultant?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Consultants.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Consultants.AnyAsync(c => c.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Consultant> Items, int TotalCount)> GetPagedAsync(ConsultantListQuery query, CancellationToken cancellationToken)
    {
        var consultants = _dbContext.Consultants.AsQueryable();

        if (query.IsActive.HasValue)
        {
            consultants = consultants.Where(c => c.IsActive == query.IsActive.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            consultants = consultants.Where(c => c.DepartmentId == query.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            consultants = consultants.Where(c => EF.Functions.ILike(c.Name, term));
        }

        consultants = ApplySort(consultants, query.Sort);

        var totalCount = await consultants.CountAsync(cancellationToken);
        var items = await consultants.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Consultant> ApplySort(IQueryable<Consultant> consultants, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return consultants.OrderBy(c => c.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "updatedat" => descending ? consultants.OrderByDescending(c => c.UpdatedAt) : consultants.OrderBy(c => c.UpdatedAt),
            _ => descending ? consultants.OrderByDescending(c => c.Name) : consultants.OrderBy(c => c.Name),
        };
    }
}
