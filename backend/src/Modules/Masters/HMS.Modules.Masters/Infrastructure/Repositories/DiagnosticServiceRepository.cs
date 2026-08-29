using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class DiagnosticServiceRepository : IDiagnosticServiceRepository
{
    private readonly MastersDbContext _dbContext;

    public DiagnosticServiceRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DiagnosticService diagnosticService, CancellationToken cancellationToken)
        => await _dbContext.DiagnosticServices.AddAsync(diagnosticService, cancellationToken);

    public Task<DiagnosticService?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.DiagnosticServices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.DiagnosticServices.AnyAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.DiagnosticServices.AnyAsync(d => EF.Functions.ILike(d.Code, code) && d.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<DiagnosticService> Items, int TotalCount)> GetPagedAsync(DiagnosticServiceListQuery query, CancellationToken cancellationToken)
    {
        var diagnosticServices = _dbContext.DiagnosticServices.AsQueryable();

        if (query.CategoryId.HasValue)
        {
            diagnosticServices = diagnosticServices.Where(d => d.CategoryId == query.CategoryId.Value);
        }

        if (query.ServiceType.HasValue)
        {
            diagnosticServices = diagnosticServices.Where(d => d.ServiceType == query.ServiceType.Value);
        }

        if (query.IsOutsourced.HasValue)
        {
            diagnosticServices = diagnosticServices.Where(d => d.IsOutsourced == query.IsOutsourced.Value);
        }

        if (query.IsActive.HasValue)
        {
            diagnosticServices = diagnosticServices.Where(d => d.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            diagnosticServices = diagnosticServices.Where(d => EF.Functions.ILike(d.Code, term) || EF.Functions.ILike(d.Name, term));
        }

        diagnosticServices = ApplySort(diagnosticServices, query.Sort);

        var totalCount = await diagnosticServices.CountAsync(cancellationToken);
        var items = await diagnosticServices.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<DiagnosticService> ApplySort(IQueryable<DiagnosticService> diagnosticServices, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return diagnosticServices.OrderBy(d => d.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "code" => descending ? diagnosticServices.OrderByDescending(d => d.Code) : diagnosticServices.OrderBy(d => d.Code),
            "price" => descending ? diagnosticServices.OrderByDescending(d => d.Price) : diagnosticServices.OrderBy(d => d.Price),
            "updatedat" => descending ? diagnosticServices.OrderByDescending(d => d.UpdatedAt) : diagnosticServices.OrderBy(d => d.UpdatedAt),
            _ => descending ? diagnosticServices.OrderByDescending(d => d.Name) : diagnosticServices.OrderBy(d => d.Name),
        };
    }
}
