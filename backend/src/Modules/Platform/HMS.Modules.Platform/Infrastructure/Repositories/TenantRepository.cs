using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Platform.Infrastructure.Repositories;

internal sealed class TenantRepository : ITenantRepository
{
    private readonly PlatformDbContext _dbContext;

    public TenantRepository(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken)
        => await _dbContext.Tenants.AddAsync(tenant, cancellationToken);

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Tenant?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Tenant?> GetByHospitalCodeAsync(string hospitalCode, CancellationToken cancellationToken)
    {
        var normalized = hospitalCode.Trim().ToLowerInvariant();
        return _dbContext.Tenants.FirstOrDefaultAsync(t => t.HospitalCode == normalized, cancellationToken);
    }

    public Task<Tenant?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _dbContext.Tenants.FirstOrDefaultAsync(t => t.Email == normalized, cancellationToken);
    }

    public async Task<(IReadOnlyList<Tenant> Items, int TotalCount)> GetPagedAsync(TenantListQuery query, CancellationToken cancellationToken)
    {
        var tenants = _dbContext.Tenants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            tenants = tenants.Where(t => EF.Functions.ILike(t.HospitalName, term) || EF.Functions.ILike(t.HospitalCode, term));
        }

        tenants = tenants.OrderByDescending(t => t.CreatedAt);

        var totalCount = await tenants.CountAsync(cancellationToken);

        var items = await tenants
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Tenant> Items, int TotalCount)> GetDeletedPagedAsync(TenantListQuery query, CancellationToken cancellationToken)
    {
        var tenants = _dbContext.Tenants.IgnoreQueryFilters().Where(t => t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            tenants = tenants.Where(t => EF.Functions.ILike(t.HospitalName, term) || EF.Functions.ILike(t.HospitalCode, term));
        }

        tenants = tenants.OrderByDescending(t => t.DeletedAt);

        var totalCount = await tenants.CountAsync(cancellationToken);

        var items = await tenants
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(int Total, int Active, int Inactive)> GetCountsAsync(CancellationToken cancellationToken)
    {
        var total = await _dbContext.Tenants.CountAsync(cancellationToken);
        var active = await _dbContext.Tenants.CountAsync(t => t.Status == TenantStatus.Active, cancellationToken);
        return (total, active, total - active);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}
