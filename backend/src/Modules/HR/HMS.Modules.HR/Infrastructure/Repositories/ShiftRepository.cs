using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.HR.Infrastructure.Repositories;

internal class ShiftRepository : IShiftRepository
{
    private readonly HRDbContext _dbContext;

    public ShiftRepository(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Shift shift, CancellationToken cancellationToken)
        => await _dbContext.Shifts.AddAsync(shift, cancellationToken);

    public Task<Shift?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Shifts.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Shifts.AnyAsync(s => s.Code == code && s.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Shift> Items, int TotalCount)> GetPagedAsync(ShiftListQuery query, CancellationToken cancellationToken)
    {
        var shifts = _dbContext.Shifts.AsQueryable();

        if (query.IsActive.HasValue)
        {
            shifts = shifts.Where(s => s.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            shifts = shifts.Where(s => EF.Functions.ILike(s.Code, term) || EF.Functions.ILike(s.Name, term));
        }

        shifts = ApplySort(shifts, query.Sort);

        var totalCount = await shifts.CountAsync(cancellationToken);
        var items = await shifts.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Shift> ApplySort(IQueryable<Shift> shifts, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return shifts.OrderBy(s => s.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "code" => descending ? shifts.OrderByDescending(s => s.Code) : shifts.OrderBy(s => s.Code),
            "starttime" => descending ? shifts.OrderByDescending(s => s.StartTime) : shifts.OrderBy(s => s.StartTime),
            "updatedat" => descending ? shifts.OrderByDescending(s => s.UpdatedAt) : shifts.OrderBy(s => s.UpdatedAt),
            _ => descending ? shifts.OrderByDescending(s => s.Name) : shifts.OrderBy(s => s.Name),
        };
    }
}
