using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class DiagnosticPackageRepository : IDiagnosticPackageRepository
{
    private readonly MastersDbContext _dbContext;

    public DiagnosticPackageRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DiagnosticPackage package, CancellationToken cancellationToken)
        => await _dbContext.DiagnosticPackages.AddAsync(package, cancellationToken);

    public Task<DiagnosticPackage?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.DiagnosticPackages
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.DiagnosticPackages.AnyAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.DiagnosticPackages.AnyAsync(p => EF.Functions.ILike(p.Code, code) && p.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<DiagnosticPackage> Items, int TotalCount)> GetPagedAsync(DiagnosticPackageListQuery query, CancellationToken cancellationToken)
    {
        var packages = _dbContext.DiagnosticPackages.Include(p => p.Items).AsQueryable();

        if (query.IsActive.HasValue)
        {
            packages = packages.Where(p => p.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            packages = packages.Where(p => EF.Functions.ILike(p.Code, term) || EF.Functions.ILike(p.Name, term));
        }

        packages = ApplySort(packages, query.Sort);

        var totalCount = await packages.CountAsync(cancellationToken);
        var items = await packages.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<DiagnosticPackage> ApplySort(IQueryable<DiagnosticPackage> packages, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return packages.OrderBy(p => p.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "code" => descending ? packages.OrderByDescending(p => p.Code) : packages.OrderBy(p => p.Code),
            "totalprice" => descending ? packages.OrderByDescending(p => p.TotalPrice) : packages.OrderBy(p => p.TotalPrice),
            "updatedat" => descending ? packages.OrderByDescending(p => p.UpdatedAt) : packages.OrderBy(p => p.UpdatedAt),
            _ => descending ? packages.OrderByDescending(p => p.Name) : packages.OrderBy(p => p.Name),
        };
    }
}
