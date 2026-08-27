using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class DesignationRepository : IDesignationRepository
{
    private readonly MastersDbContext _dbContext;

    public DesignationRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Designation designation, CancellationToken cancellationToken)
        => await _dbContext.Designations.AddAsync(designation, cancellationToken);

    public Task<Designation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Designations.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Designations.AnyAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Designations.AnyAsync(d => d.Code == code && d.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Designation> Items, int TotalCount)> GetPagedAsync(DesignationListQuery query, CancellationToken cancellationToken)
    {
        var designations = _dbContext.Designations.AsQueryable();

        if (query.IsActive.HasValue)
        {
            designations = designations.Where(d => d.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            designations = designations.Where(d => EF.Functions.ILike(d.Code, term) || EF.Functions.ILike(d.Name, term));
        }

        designations = ApplySort(designations, query.Sort);

        var totalCount = await designations.CountAsync(cancellationToken);
        var items = await designations.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Designation> ApplySort(IQueryable<Designation> designations, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return designations.OrderBy(d => d.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "code" => descending ? designations.OrderByDescending(d => d.Code) : designations.OrderBy(d => d.Code),
            "updatedat" => descending ? designations.OrderByDescending(d => d.UpdatedAt) : designations.OrderBy(d => d.UpdatedAt),
            _ => descending ? designations.OrderByDescending(d => d.Name) : designations.OrderBy(d => d.Name),
        };
    }
}
