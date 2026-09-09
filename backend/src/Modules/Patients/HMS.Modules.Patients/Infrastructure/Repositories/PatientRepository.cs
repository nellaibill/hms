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
            .Include(p => p.Address)
            .Include(p => p.Allergies)
            .Include(p => p.EmergencyContacts)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetPagedAsync(PatientListQuery query, CancellationToken cancellationToken)
    {
        var patients = BuildFilteredQuery(query)
            .Include(p => p.Address)
            .Include(p => p.Allergies)
            .Include(p => p.EmergencyContacts)
            .AsQueryable();

        patients = ApplySort(patients, query.Sort);

        var totalCount = await patients.CountAsync(cancellationToken);

        var items = await patients
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>Total/new/returning/visit counts for Patient Reports' summary cards — reuses
    /// the exact same filters GetPagedAsync applies (via BuildFilteredQuery), so the numbers
    /// always agree with what the table below them actually shows. "New" is the subset of the
    /// activity-scoped population (see PatientListQuery.From's own doc comment) whose
    /// registration itself falls in range; "Returning" is simply Total minus New.</summary>
    public async Task<(int TotalPatients, int NewPatients, int TotalVisits)> GetReportSummaryAsync(PatientListQuery query, CancellationToken cancellationToken)
    {
        var patients = BuildFilteredQuery(query);
        var totalPatients = await patients.CountAsync(cancellationToken);

        var newPatients = 0;
        var totalVisits = 0;
        if (query.From.HasValue && query.To.HasValue)
        {
            var from = DateTime.SpecifyKind(query.From.Value, DateTimeKind.Utc);
            var to = DateTime.SpecifyKind(query.To.Value, DateTimeKind.Utc);
            newPatients = await patients.CountAsync(p => p.CreatedAt >= from && p.CreatedAt <= to, cancellationToken);

            // Visits by patients matching every other filter, within range — reuses the same
            // patient-level filtered set (minus its own From/To clause, which is patient-level
            // activity, not a visit-level date filter) as the scope for "whose visits count".
            var patientIds = patients.Select(p => p.Id);
            totalVisits = await _dbContext.PatientVisits
                .Where(v => patientIds.Contains(v.PatientId) && v.CreatedAt >= from && v.CreatedAt <= to)
                .CountAsync(cancellationToken);
        }

        return (totalPatients, newPatients, totalVisits);
    }

    /// <summary>The most recent visit (with its Consultations, for the report table's
    /// Department column) for each of the given patients — deliberately fetched as a second,
    /// bounded query against just this page's ids (≤ PagedRequest.MaxPageSize) rather than a
    /// correlated subquery per row in GetPagedAsync's main query, so that query stays a plain
    /// indexable filter/sort/page. In-memory grouping (not EF GroupBy+Include, which doesn't
    /// compose cleanly) — bounded by how many visits this small id set can have, not by the
    /// tenant's total visit count.</summary>
    public async Task<IReadOnlyDictionary<Guid, PatientVisit>> GetLastVisitsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken cancellationToken)
    {
        if (patientIds.Count == 0) return new Dictionary<Guid, PatientVisit>();

        var visits = await _dbContext.PatientVisits
            .Include(v => v.Consultations)
            .Where(v => patientIds.Contains(v.PatientId))
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);

        return visits
            .GroupBy(v => v.PatientId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task<IReadOnlyList<Patient>> GetExportRowsAsync(PatientListQuery query, int maxRows, CancellationToken cancellationToken)
    {
        var patients = ApplySort(
            BuildFilteredQuery(query).Include(p => p.Address).Include(p => p.Allergies).Include(p => p.EmergencyContacts).AsQueryable(),
            query.Sort);
        return await patients.Take(maxRows).ToListAsync(cancellationToken);
    }

    private IQueryable<Patient> BuildFilteredQuery(PatientListQuery query)
    {
        var patients = _dbContext.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            patients = patients.Where(p =>
                EF.Functions.ILike(p.FirstName, term) ||
                EF.Functions.ILike(p.LastName, term) ||
                EF.Functions.ILike(p.Uhid, term) ||
                EF.Functions.ILike(p.PrimaryPhone, term));
        }

        // The dedicated filters below are independent and AND together — a receptionist
        // filling in more than one search box narrows the result further.
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
            patients = patients.Where(p => EF.Functions.ILike(p.PrimaryPhone, term) || (p.SecondaryPhone != null && EF.Functions.ILike(p.SecondaryPhone, term)));
        }

        if (query.RequiresDataVerification.HasValue)
        {
            patients = patients.Where(p => p.RequiresDataVerification == query.RequiresDataVerification.Value);
        }

        if (query.RegisteredToday == true)
        {
            // Driven by patient_visits, not the patient record's own timestamps — OPD Billing
            // wants "who has a visit to bill today", which for a returning patient (registered
            // long ago) is about today's visit, not their original registration date.
            var today = DateTime.UtcNow.Date;
            patients = patients.Where(p => _dbContext.PatientVisits.Any(v => v.PatientId == p.Id
                && (v.CreatedAt.Date == today || (v.UpdatedAt.HasValue && v.UpdatedAt.Value.Date == today))));
        }

        if (query.Age.HasValue && query.Age.Value >= 0)
        {
            // Age isn't a stored column (Patient.Age is always derived from DateOfBirth), so
            // "age equals N" is expressed as the DateOfBirth range that produces age N today.
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var maxDateOfBirth = today.AddYears(-query.Age.Value);
            var minDateOfBirthExclusive = today.AddYears(-(query.Age.Value + 1));
            patients = patients.Where(p => p.DateOfBirth <= maxDateOfBirth && p.DateOfBirth > minDateOfBirthExclusive);
        }

        if (query.Gender.HasValue)
        {
            patients = patients.Where(p => p.Gender == query.Gender.Value);
        }

        if (query.BloodGroup.HasValue)
        {
            patients = patients.Where(p => p.BloodGroup == query.BloodGroup.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            // Department lives on PatientVisitConsultation, not Patient — same EXISTS-via-
            // visits shape RegisteredToday above already uses.
            var departmentId = query.DepartmentId.Value;
            patients = patients.Where(p => _dbContext.PatientVisits.Any(v => v.PatientId == p.Id
                && v.Consultations.Any(c => c.DepartmentId == departmentId)));
        }

        // Model binding produces DateTime.Kind = Unspecified for a plain query-string date —
        // Npgsql rejects that against a `timestamp with time zone` column ("only UTC is
        // supported"), so it must be normalized before use (same fix as
        // PatientVisitRepository.GetPagedAsync). From/To are an "activity" scope — registered
        // OR visited in range, not registration-date alone — see PatientListQuery.From's own
        // doc comment for why.
        if (query.From.HasValue && query.To.HasValue)
        {
            var from = DateTime.SpecifyKind(query.From.Value, DateTimeKind.Utc);
            var to = DateTime.SpecifyKind(query.To.Value, DateTimeKind.Utc);
            patients = patients.Where(p =>
                (p.CreatedAt >= from && p.CreatedAt <= to) ||
                _dbContext.PatientVisits.Any(v => v.PatientId == p.Id && v.CreatedAt >= from && v.CreatedAt <= to));
        }

        return patients;
    }

    public Task<Patient?> FindDuplicateAsync(string primaryPhone, string firstName, string lastName, string? idProofNumber, CancellationToken cancellationToken)
    {
        var query = _dbContext.Patients.Where(
            p => p.PrimaryPhone == primaryPhone && EF.Functions.ILike(p.FirstName, firstName) && EF.Functions.ILike(p.LastName, lastName));

        // When an ID number is supplied, require it to also match — the strongest of the
        // three signals. When it isn't supplied, name+phone alone still catches the common case.
        if (!string.IsNullOrWhiteSpace(idProofNumber))
        {
            query = query.Where(p => p.IdProofNumber == idProofNumber);
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

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
