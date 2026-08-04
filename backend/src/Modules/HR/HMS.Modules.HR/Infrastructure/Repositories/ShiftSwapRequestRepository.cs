using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Contracts;
using HMS.Modules.HR.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.HR.Infrastructure.Repositories;

internal class ShiftSwapRequestRepository : IShiftSwapRequestRepository
{
    private readonly HRDbContext _dbContext;

    public ShiftSwapRequestRepository(HRDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ShiftSwapRequest shiftSwapRequest, CancellationToken cancellationToken)
        => await _dbContext.ShiftSwapRequests.AddAsync(shiftSwapRequest, cancellationToken);

    public Task<ShiftSwapRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ShiftSwapRequests.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    // Only Remarks is free text — Status is a fixed enum, not a useful ILike target.
    // Mirrors ShiftAssignment's/StaffAvailability's Remarks/Reason-only search.
    public async Task<(IReadOnlyList<ShiftSwapRequest> Items, int TotalCount)> GetPagedAsync(SwapRequestListQuery query, CancellationToken cancellationToken)
    {
        var swapRequests = _dbContext.ShiftSwapRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            swapRequests = swapRequests.Where(s => s.Remarks != null && EF.Functions.ILike(s.Remarks, term));
        }

        swapRequests = ApplySort(swapRequests, query.Sort);

        var totalCount = await swapRequests.CountAsync(cancellationToken);
        var items = await swapRequests.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<ShiftSwapRequest> ApplySort(IQueryable<ShiftSwapRequest> swapRequests, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return swapRequests.OrderByDescending(s => s.RequestedDate);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "requesteddate" => descending ? swapRequests.OrderByDescending(s => s.RequestedDate) : swapRequests.OrderBy(s => s.RequestedDate),
            "status" => descending ? swapRequests.OrderByDescending(s => s.Status) : swapRequests.OrderBy(s => s.Status),
            "updatedat" => descending ? swapRequests.OrderByDescending(s => s.UpdatedAt) : swapRequests.OrderBy(s => s.UpdatedAt),
            _ => descending ? swapRequests.OrderByDescending(s => s.RequestedDate) : swapRequests.OrderBy(s => s.RequestedDate),
        };
    }
}
