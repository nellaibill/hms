using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Masters.Infrastructure.Repositories;

internal class DiagnosisRepository : IDiagnosisRepository
{
    private readonly MastersDbContext _dbContext;

    public DiagnosisRepository(MastersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Diagnosis diagnosis, CancellationToken cancellationToken)
        => await _dbContext.Diagnoses.AddAsync(diagnosis, cancellationToken);

    public Task<Diagnosis?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Diagnoses.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Diagnoses.AnyAsync(d => d.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
        => _dbContext.Diagnoses.AnyAsync(d => EF.Functions.ILike(d.Name, name) && d.Id != excludingId, cancellationToken);

    public async Task<(IReadOnlyList<Diagnosis> Items, int TotalCount)> GetPagedAsync(DiagnosisListQuery query, CancellationToken cancellationToken)
    {
        var diagnoses = _dbContext.Diagnoses.AsQueryable();

        if (query.IsActive.HasValue)
        {
            diagnoses = diagnoses.Where(d => d.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            diagnoses = diagnoses.Where(d => EF.Functions.ILike(d.Name, term) || (d.IcdCode != null && EF.Functions.ILike(d.IcdCode, term)));
        }

        diagnoses = ApplySort(diagnoses, query.Sort);

        var totalCount = await diagnoses.CountAsync(cancellationToken);
        var items = await diagnoses.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Diagnosis> ApplySort(IQueryable<Diagnosis> diagnoses, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return diagnoses.OrderBy(d => d.Name);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "updatedat" => descending ? diagnoses.OrderByDescending(d => d.UpdatedAt) : diagnoses.OrderBy(d => d.UpdatedAt),
            _ => descending ? diagnoses.OrderByDescending(d => d.Name) : diagnoses.OrderBy(d => d.Name),
        };
    }
}
