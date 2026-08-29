using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class DiagnosticCategoryRepository : IDiagnosticCategoryRepository
{
    private readonly MastersDbContext _dbContext;

    public DiagnosticCategoryRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DiagnosticCategory category, CancellationToken cancellationToken)
        => await _dbContext.DiagnosticCategories.AddAsync(category, cancellationToken);

    public Task<DiagnosticCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.DiagnosticCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.DiagnosticCategories.AnyAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.DiagnosticCategories.AnyAsync(c => EF.Functions.ILike(c.Code, code) && c.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<DiagnosticCategory> Items, int TotalCount)> GetPagedAsync(DiagnosticCategoryListQuery query, CancellationToken cancellationToken)
    {
        var categories = _dbContext.DiagnosticCategories.AsQueryable();

        if (query.IsActive.HasValue)
        {
            categories = categories.Where(c => c.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            categories = categories.Where(c => EF.Functions.ILike(c.Code, term) || EF.Functions.ILike(c.Name, term));
        }

        categories = ApplySort(categories, query.Sort);

        var totalCount = await categories.CountAsync(cancellationToken);
        var items = await categories.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<DiagnosticCategory> ApplySort(IQueryable<DiagnosticCategory> categories, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return categories.OrderBy(c => c.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "code" => descending ? categories.OrderByDescending(c => c.Code) : categories.OrderBy(c => c.Code),
            "updatedat" => descending ? categories.OrderByDescending(c => c.UpdatedAt) : categories.OrderBy(c => c.UpdatedAt),
            _ => descending ? categories.OrderByDescending(c => c.Name) : categories.OrderBy(c => c.Name),
        };
    }
}
