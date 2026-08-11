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

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Patients.AnyAsync(p => p.Id == id, cancellationToken);

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

        // The four dedicated filters below are independent and AND together — a receptionist
        // filling in more than one search box (e.g. Name + Phone) narrows the result further,
        // rather than the fields OR-ing against each other.
        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var term = $"%{query.Name.Trim()}%";
            patients = patients.Where(p => EF.Functions.ILike(p.FirstName, term) || EF.Functions.ILike(p.LastName, term));
        }

        if (!string.IsNullOrWhiteSpace(query.Uhid))
        {
            var term = $"%{query.Uhid.Trim()}%";
            patients = patients.Where(p => EF.Functions.ILike(p.Uhid, term));
        }

        if (!string.IsNullOrWhiteSpace(query.Phone))
        {
            var term = $"%{query.Phone.Trim()}%";
            patients = patients.Where(p =>
                EF.Functions.ILike(p.PrimaryPhone, term) ||
                (p.AlternatePhone != null && EF.Functions.ILike(p.AlternatePhone, term)) ||
                (p.AlternatePhone2 != null && EF.Functions.ILike(p.AlternatePhone2, term)));
        }

        if (query.Age.HasValue && query.Age.Value >= 0)
        {
            // Age isn't a stored column (Patient.Age is always derived from DateOfBirth —
            // see Domain/Patient.cs), so "age equals N" is expressed as the DateOfBirth
            // range that produces age N as of today: born on or before N years ago, but
            // not more than N+1 years ago.
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var maxDateOfBirth = today.AddYears(-query.Age.Value);
            var minDateOfBirthExclusive = today.AddYears(-(query.Age.Value + 1));
            patients = patients.Where(p => p.DateOfBirth <= maxDateOfBirth && p.DateOfBirth > minDateOfBirthExclusive);
        }

        patients = ApplySort(patients, query.Sort);

        var totalCount = await patients.CountAsync(cancellationToken);

        var items = await patients
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Patient?> FindDuplicateAsync(string primaryPhone, string firstName, string lastName, CancellationToken cancellationToken)
        => _dbContext.Patients.FirstOrDefaultAsync(
            p => p.PrimaryPhone == primaryPhone && EF.Functions.ILike(p.FirstName, firstName) && EF.Functions.ILike(p.LastName, lastName),
            cancellationToken);

    public string GetRowVersion(Patient patient)
        => _dbContext.Entry(patient).Property<uint>("xmin").CurrentValue.ToString();

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
