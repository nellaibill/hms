using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.IPD.Infrastructure.Repositories;

internal class BedRepository : IBedRepository
{
    private readonly IPDDbContext _dbContext;

    public BedRepository(IPDDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Bed bed, CancellationToken cancellationToken)
        => await _dbContext.Beds.AddAsync(bed, cancellationToken);

    public Task<Bed?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Beds.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<bool> ExistsByBedNumberAsync(Guid wardId, string bedNumber, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Beds.AnyAsync(b => b.WardId == wardId && b.BedNumber == bedNumber && b.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Bed> Items, int TotalCount)> GetPagedAsync(BedListQuery query, CancellationToken cancellationToken)
    {
        var beds = _dbContext.Beds.AsQueryable();

        if (query.WardId.HasValue)
        {
            beds = beds.Where(b => b.WardId == query.WardId.Value);
        }

        if (query.Status.HasValue)
        {
            beds = beds.Where(b => b.Status == query.Status.Value);
        }

        if (query.IsActive.HasValue)
        {
            beds = beds.Where(b => b.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            beds = beds.Where(b => EF.Functions.ILike(b.BedNumber, term) || EF.Functions.ILike(b.BedType, term));
        }

        beds = ApplySort(beds, query.Sort);

        var totalCount = await beds.CountAsync(cancellationToken);
        var items = await beds.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Bed>> GetAvailableAsync(Guid? wardId, CancellationToken cancellationToken)
    {
        var beds = _dbContext.Beds.Where(b => b.Status == BedStatus.Available && b.IsActive);

        if (wardId.HasValue)
        {
            beds = beds.Where(b => b.WardId == wardId.Value);
        }

        return await beds.OrderBy(b => b.BedNumber).ToListAsync(cancellationToken);
    }

    public Task<int> CountByStatusAsync(BedStatus status, CancellationToken cancellationToken)
        => _dbContext.Beds.CountAsync(b => b.Status == status, cancellationToken);

    public async Task<(int Total, int Occupied)> GetIcuOccupancyAsync(CancellationToken cancellationToken)
    {
        var icuBeds = from bed in _dbContext.Beds
                      join ward in _dbContext.Wards on bed.WardId equals ward.Id
                      where ward.WardType == WardType.ICU
                      select bed.Status;

        var statuses = await icuBeds.ToListAsync(cancellationToken);
        return (statuses.Count, statuses.Count(s => s == BedStatus.Occupied));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Bed> ApplySort(IQueryable<Bed> beds, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return beds.OrderBy(b => b.BedNumber);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "status" => descending ? beds.OrderByDescending(b => b.Status) : beds.OrderBy(b => b.Status),
            "updatedat" => descending ? beds.OrderByDescending(b => b.UpdatedAt) : beds.OrderBy(b => b.UpdatedAt),
            _ => descending ? beds.OrderByDescending(b => b.BedNumber) : beds.OrderBy(b => b.BedNumber),
        };
    }
}
