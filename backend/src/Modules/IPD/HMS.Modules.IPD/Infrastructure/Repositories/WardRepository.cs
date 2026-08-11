using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class WardRepository : IWardRepository
{
    private readonly IPDDbContext _dbContext;

    public WardRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Ward ward, CancellationToken cancellationToken)
        => await _dbContext.Wards.AddAsync(ward, cancellationToken);

    public Task<Ward?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Wards.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Wards.AnyAsync(w => w.Code == code && w.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Ward> Items, int TotalCount)> GetPagedAsync(WardListQuery query, CancellationToken cancellationToken)
    {
        var wards = _dbContext.Wards.AsQueryable();

        if (query.IsActive.HasValue)
        {
            wards = wards.Where(w => w.IsActive == query.IsActive.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            wards = wards.Where(w => w.DepartmentId == query.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            wards = wards.Where(w => EF.Functions.ILike(w.Code, term) || EF.Functions.ILike(w.Name, term));
        }

        wards = ApplySort(wards, query.Sort);

        var totalCount = await wards.CountAsync(cancellationToken);
        var items = await wards.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Ward> ApplySort(IQueryable<Ward> wards, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return wards.OrderBy(w => w.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "code" => descending ? wards.OrderByDescending(w => w.Code) : wards.OrderBy(w => w.Code),
            "updatedat" => descending ? wards.OrderByDescending(w => w.UpdatedAt) : wards.OrderBy(w => w.UpdatedAt),
            _ => descending ? wards.OrderByDescending(w => w.Name) : wards.OrderBy(w => w.Name),
        };
    }
}
