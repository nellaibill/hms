using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.HR.Infrastructure.Repositories;

internal class StaffAvailabilityRepository : IStaffAvailabilityRepository
{
    private readonly HRDbContext _dbContext;

    public StaffAvailabilityRepository(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(StaffAvailability staffAvailability, CancellationToken cancellationToken)
        => await _dbContext.StaffAvailabilities.AddAsync(staffAvailability, cancellationToken);

    public Task<StaffAvailability?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.StaffAvailabilities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<StaffAvailability> Items, int TotalCount)> GetPagedAsync(StaffAvailabilityListQuery query, CancellationToken cancellationToken)
    {
        var availabilities = _dbContext.StaffAvailabilities.AsQueryable();

        // Only Reason is free text — AvailabilityStatus is a fixed enum, not a useful
        // ILike target. Mirrors ShiftAssignment's Remarks-only search.
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            availabilities = availabilities.Where(a => a.Reason != null && EF.Functions.ILike(a.Reason, term));
        }

        availabilities = ApplySort(availabilities, query.Sort);

        var totalCount = await availabilities.CountAsync(cancellationToken);
        var items = await availabilities.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<StaffAvailability> ApplySort(IQueryable<StaffAvailability> availabilities, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return availabilities.OrderByDescending(a => a.StartDate);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "startdate" => descending ? availabilities.OrderByDescending(a => a.StartDate) : availabilities.OrderBy(a => a.StartDate),
            "enddate" => descending ? availabilities.OrderByDescending(a => a.EndDate) : availabilities.OrderBy(a => a.EndDate),
            "updatedat" => descending ? availabilities.OrderByDescending(a => a.UpdatedAt) : availabilities.OrderBy(a => a.UpdatedAt),
            _ => descending ? availabilities.OrderByDescending(a => a.StartDate) : availabilities.OrderBy(a => a.StartDate),
        };
    }
}
