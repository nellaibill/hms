using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class ConsultationTypeRepository : IConsultationTypeRepository
{
    private readonly MastersDbContext _dbContext;

    public ConsultationTypeRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ConsultationType consultationType, CancellationToken cancellationToken)
        => await _dbContext.ConsultationTypes.AddAsync(consultationType, cancellationToken);

    public Task<ConsultationType?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ConsultationTypes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.ConsultationTypes.AnyAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.ConsultationTypes.AnyAsync(c => EF.Functions.ILike(c.Name, name) && c.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<ConsultationType> Items, int TotalCount)> GetPagedAsync(ConsultationTypeListQuery query, CancellationToken cancellationToken)
    {
        var consultationTypes = _dbContext.ConsultationTypes.AsQueryable();

        if (query.IsActive.HasValue)
        {
            consultationTypes = consultationTypes.Where(c => c.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            consultationTypes = consultationTypes.Where(c => EF.Functions.ILike(c.Name, term));
        }

        consultationTypes = ApplySort(consultationTypes, query.Sort);

        var totalCount = await consultationTypes.CountAsync(cancellationToken);
        var items = await consultationTypes.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<ConsultationType> ApplySort(IQueryable<ConsultationType> consultationTypes, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return consultationTypes.OrderBy(c => c.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "amount" => descending ? consultationTypes.OrderByDescending(c => c.Amount) : consultationTypes.OrderBy(c => c.Amount),
            "updatedat" => descending ? consultationTypes.OrderByDescending(c => c.UpdatedAt) : consultationTypes.OrderBy(c => c.UpdatedAt),
            _ => descending ? consultationTypes.OrderByDescending(c => c.Name) : consultationTypes.OrderBy(c => c.Name),
        };
    }
}
