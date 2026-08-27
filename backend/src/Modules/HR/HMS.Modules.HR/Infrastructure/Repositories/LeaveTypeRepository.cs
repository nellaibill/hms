using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.HR.Infrastructure.Repositories;

internal class LeaveTypeRepository : ILeaveTypeRepository
{
    private readonly HRDbContext _dbContext;

    public LeaveTypeRepository(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(LeaveType leaveType, CancellationToken cancellationToken)
        => await _dbContext.LeaveTypes.AddAsync(leaveType, cancellationToken);

    public Task<LeaveType?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.LeaveTypes.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.LeaveTypes.AnyAsync(l => l.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.LeaveTypes.AnyAsync(l => l.Code == code && l.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<LeaveType> Items, int TotalCount)> GetPagedAsync(LeaveTypeListQuery query, CancellationToken cancellationToken)
    {
        var leaveTypes = _dbContext.LeaveTypes.AsQueryable();

        if (query.IsActive.HasValue)
        {
            leaveTypes = leaveTypes.Where(l => l.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            leaveTypes = leaveTypes.Where(l => EF.Functions.ILike(l.Code, term) || EF.Functions.ILike(l.Name, term));
        }

        leaveTypes = ApplySort(leaveTypes, query.Sort);

        var totalCount = await leaveTypes.CountAsync(cancellationToken);
        var items = await leaveTypes.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<LeaveType>> GetActiveAsync(CancellationToken cancellationToken)
        => await _dbContext.LeaveTypes.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<LeaveType> ApplySort(IQueryable<LeaveType> leaveTypes, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return leaveTypes.OrderBy(l => l.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "code" => descending ? leaveTypes.OrderByDescending(l => l.Code) : leaveTypes.OrderBy(l => l.Code),
            "updatedat" => descending ? leaveTypes.OrderByDescending(l => l.UpdatedAt) : leaveTypes.OrderBy(l => l.UpdatedAt),
            _ => descending ? leaveTypes.OrderByDescending(l => l.Name) : leaveTypes.OrderBy(l => l.Name),
        };
    }
}
