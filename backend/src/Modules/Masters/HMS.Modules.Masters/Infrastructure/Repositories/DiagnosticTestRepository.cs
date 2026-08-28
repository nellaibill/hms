using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class DiagnosticTestRepository : IDiagnosticTestRepository
{
    private readonly MastersDbContext _dbContext;

    public DiagnosticTestRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DiagnosticTest diagnosticTest, CancellationToken cancellationToken)
        => await _dbContext.DiagnosticTests.AddAsync(diagnosticTest, cancellationToken);

    public Task<DiagnosticTest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.DiagnosticTests.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.DiagnosticTests.AnyAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, DiagnosticTestServiceType serviceType, bool isOutsourced, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.DiagnosticTests.AnyAsync(
            d => EF.Functions.ILike(d.Name, name) && d.ServiceType == serviceType && d.IsOutsourced == isOutsourced && d.Id != excludingId,
            cancellationToken);

    public async Task<(IReadOnlyList<DiagnosticTest> Items, int TotalCount)> GetPagedAsync(DiagnosticTestListQuery query, CancellationToken cancellationToken)
    {
        var diagnosticTests = _dbContext.DiagnosticTests.AsQueryable();

        if (query.ServiceType.HasValue)
        {
            diagnosticTests = diagnosticTests.Where(d => d.ServiceType == query.ServiceType.Value);
        }

        if (query.IsOutsourced.HasValue)
        {
            diagnosticTests = diagnosticTests.Where(d => d.IsOutsourced == query.IsOutsourced.Value);
        }

        if (query.IsActive.HasValue)
        {
            diagnosticTests = diagnosticTests.Where(d => d.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            diagnosticTests = diagnosticTests.Where(d => EF.Functions.ILike(d.Name, term) || (d.Category != null && EF.Functions.ILike(d.Category, term)));
        }

        diagnosticTests = ApplySort(diagnosticTests, query.Sort);

        var totalCount = await diagnosticTests.CountAsync(cancellationToken);
        var items = await diagnosticTests.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<DiagnosticTest> ApplySort(IQueryable<DiagnosticTest> diagnosticTests, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return diagnosticTests.OrderBy(d => d.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "price" => descending ? diagnosticTests.OrderByDescending(d => d.Price) : diagnosticTests.OrderBy(d => d.Price),
            "category" => descending ? diagnosticTests.OrderByDescending(d => d.Category) : diagnosticTests.OrderBy(d => d.Category),
            "updatedat" => descending ? diagnosticTests.OrderByDescending(d => d.UpdatedAt) : diagnosticTests.OrderBy(d => d.UpdatedAt),
            _ => descending ? diagnosticTests.OrderByDescending(d => d.Name) : diagnosticTests.OrderBy(d => d.Name),
        };
    }
}
