using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Patients.Infrastructure.Repositories;

internal class PatientRepository : IPatientRepository
{
    private readonly PatientsDbContext _dbContext;

    public PatientRepository(PatientsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken)
        => await _dbContext.Patients.AddAsync(patient, cancellationToken);

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Patients
            .Include(p => p.Registrations)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetPagedAsync(PatientListQuery query, CancellationToken cancellationToken)
    {
        var patients = _dbContext.Patients.Include(p => p.Registrations).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            patients = patients.Where(p =>
                EF.Functions.ILike(p.FirstName, term) ||
                EF.Functions.ILike(p.LastName, term) ||
                EF.Functions.ILike(p.Uhid, term) ||
                EF.Functions.ILike(p.PrimaryPhone, term));
        }

        patients = ApplySort(patients, query.Sort);

        var totalCount = await patients.CountAsync(cancellationToken);

        var items = await patients
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<Patient> ApplySort(IQueryable<Patient> patients, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return patients.OrderByDescending(p => p.CreatedAt);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "firstname" => descending ? patients.OrderByDescending(p => p.FirstName) : patients.OrderBy(p => p.FirstName),
            "lastname" => descending ? patients.OrderByDescending(p => p.LastName) : patients.OrderBy(p => p.LastName),
            "uhid" => descending ? patients.OrderByDescending(p => p.Uhid) : patients.OrderBy(p => p.Uhid),
            "createdat" => descending ? patients.OrderByDescending(p => p.CreatedAt) : patients.OrderBy(p => p.CreatedAt),
            _ => patients.OrderByDescending(p => p.CreatedAt),
        };
    }
}
