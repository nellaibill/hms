using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.HR.Infrastructure.Repositories;

internal class ShiftAssignmentRepository : IShiftAssignmentRepository
{
    private readonly HRDbContext _dbContext;

    public ShiftAssignmentRepository(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ShiftAssignment shiftAssignment, CancellationToken cancellationToken)
        => await _dbContext.ShiftAssignments.AddAsync(shiftAssignment, cancellationToken);

    public Task<ShiftAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ShiftAssignments.FirstOrDefaultAsync(sa => sa.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ShiftAssignment>> GetByStaffAndDateAsync(Guid staffId, DateOnly rosterDate, Guid? excludingId, CancellationToken cancellationToken)
        => await _dbContext.ShiftAssignments
            .Where(sa => sa.StaffId == staffId && sa.RosterDate == rosterDate && sa.Id != excludingId)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<ShiftAssignment> Items, int TotalCount)> GetPagedAsync(ShiftAssignmentListQuery query, CancellationToken cancellationToken)
    {
        var assignments = _dbContext.ShiftAssignments.AsQueryable();

        if (query.DepartmentId.HasValue)
        {
            assignments = assignments.Where(sa => sa.DepartmentId == query.DepartmentId.Value);
        }

        if (query.RosterDateFrom.HasValue)
        {
            assignments = assignments.Where(sa => sa.RosterDate >= query.RosterDateFrom.Value);
        }

        if (query.RosterDateTo.HasValue)
        {
            assignments = assignments.Where(sa => sa.RosterDate <= query.RosterDateTo.Value);
        }

        // Only Remarks is free text — Status is a fixed enum, not a useful ILike target.
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            assignments = assignments.Where(sa => sa.Remarks != null && EF.Functions.ILike(sa.Remarks, term));
        }

        assignments = ApplySort(assignments, query.Sort);

        var totalCount = await assignments.CountAsync(cancellationToken);
        var items = await assignments.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<ShiftAssignment> ApplySort(IQueryable<ShiftAssignment> assignments, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return assignments.OrderByDescending(sa => sa.RosterDate);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "rosterdate" => descending ? assignments.OrderByDescending(sa => sa.RosterDate) : assignments.OrderBy(sa => sa.RosterDate),
            "status" => descending ? assignments.OrderByDescending(sa => sa.Status) : assignments.OrderBy(sa => sa.Status),
            "updatedat" => descending ? assignments.OrderByDescending(sa => sa.UpdatedAt) : assignments.OrderBy(sa => sa.UpdatedAt),
            _ => descending ? assignments.OrderByDescending(sa => sa.RosterDate) : assignments.OrderBy(sa => sa.RosterDate),
        };
    }
}
