using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class DepartmentRepository : IDepartmentRepository
{
    private readonly MastersDbContext _dbContext;

    public DepartmentRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Department department, CancellationToken cancellationToken)
        => await _dbContext.Departments.AddAsync(department, cancellationToken);

    public Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Departments.AnyAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Departments.AnyAsync(d => d.Code == code && d.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Department> Items, int TotalCount)> GetPagedAsync(DepartmentListQuery query, CancellationToken cancellationToken)
    {
        var departments = _dbContext.Departments.AsQueryable();

        if (query.IsActive.HasValue)
        {
            departments = departments.Where(d => d.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            departments = departments.Where(d => EF.Functions.ILike(d.Code, term) || EF.Functions.ILike(d.Name, term));
        }

        departments = ApplySort(departments, query.Sort);

        var totalCount = await departments.CountAsync(cancellationToken);
        var items = await departments.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Department> ApplySort(IQueryable<Department> departments, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return departments.OrderBy(d => d.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "code" => descending ? departments.OrderByDescending(d => d.Code) : departments.OrderBy(d => d.Code),
            "updatedat" => descending ? departments.OrderByDescending(d => d.UpdatedAt) : departments.OrderBy(d => d.UpdatedAt),
            _ => descending ? departments.OrderByDescending(d => d.Name) : departments.OrderBy(d => d.Name),
        };
    }
}
