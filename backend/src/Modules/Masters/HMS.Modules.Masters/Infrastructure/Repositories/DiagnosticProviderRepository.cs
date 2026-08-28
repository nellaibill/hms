using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class DiagnosticProviderRepository : IDiagnosticProviderRepository
{
    private readonly MastersDbContext _dbContext;

    public DiagnosticProviderRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DiagnosticProvider provider, CancellationToken cancellationToken)
        => await _dbContext.DiagnosticProviders.AddAsync(provider, cancellationToken);

    public Task<DiagnosticProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.DiagnosticProviders.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.DiagnosticProviders.AnyAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.DiagnosticProviders.AnyAsync(p => EF.Functions.ILike(p.Code, code) && p.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<DiagnosticProvider> Items, int TotalCount)> GetPagedAsync(DiagnosticProviderListQuery query, CancellationToken cancellationToken)
    {
        var providers = _dbContext.DiagnosticProviders.AsQueryable();

        if (query.IsActive.HasValue)
        {
            providers = providers.Where(p => p.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            providers = providers.Where(p => EF.Functions.ILike(p.Code, term) || EF.Functions.ILike(p.Name, term));
        }

        providers = ApplySort(providers, query.Sort);

        var totalCount = await providers.CountAsync(cancellationToken);
        var items = await providers.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<DiagnosticProvider> ApplySort(IQueryable<DiagnosticProvider> providers, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return providers.OrderBy(p => p.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "code" => descending ? providers.OrderByDescending(p => p.Code) : providers.OrderBy(p => p.Code),
            "updatedat" => descending ? providers.OrderByDescending(p => p.UpdatedAt) : providers.OrderBy(p => p.UpdatedAt),
            _ => descending ? providers.OrderByDescending(p => p.Name) : providers.OrderBy(p => p.Name),
        };
    }
}
