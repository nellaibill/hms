using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class AppointmentTypeRepository : IAppointmentTypeRepository
{
    private readonly MastersDbContext _dbContext;

    public AppointmentTypeRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AppointmentType appointmentType, CancellationToken cancellationToken)
        => await _dbContext.AppointmentTypes.AddAsync(appointmentType, cancellationToken);

    public Task<AppointmentType?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.AppointmentTypes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.AppointmentTypes.AnyAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.AppointmentTypes.AnyAsync(a => EF.Functions.ILike(a.Name, name) && a.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<AppointmentType> Items, int TotalCount)> GetPagedAsync(AppointmentTypeListQuery query, CancellationToken cancellationToken)
    {
        var appointmentTypes = _dbContext.AppointmentTypes.AsQueryable();

        if (query.IsActive.HasValue)
        {
            appointmentTypes = appointmentTypes.Where(a => a.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            appointmentTypes = appointmentTypes.Where(a => EF.Functions.ILike(a.Name, term));
        }

        appointmentTypes = ApplySort(appointmentTypes, query.Sort);

        var totalCount = await appointmentTypes.CountAsync(cancellationToken);
        var items = await appointmentTypes.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<AppointmentType> ApplySort(IQueryable<AppointmentType> appointmentTypes, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return appointmentTypes.OrderBy(a => a.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "updatedat" => descending ? appointmentTypes.OrderByDescending(a => a.UpdatedAt) : appointmentTypes.OrderBy(a => a.UpdatedAt),
            _ => descending ? appointmentTypes.OrderByDescending(a => a.Name) : appointmentTypes.OrderBy(a => a.Name),
        };
    }
}
