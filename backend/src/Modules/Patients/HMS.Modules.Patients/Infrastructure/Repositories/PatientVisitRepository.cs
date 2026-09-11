using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Patients.Infrastructure.Repositories;

internal class PatientVisitRepository : IPatientVisitRepository
{
    private readonly PatientsDbContext _dbContext;

    public PatientVisitRepository(PatientsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PatientVisit visit, CancellationToken cancellationToken)
        => await _dbContext.PatientVisits.AddAsync(visit, cancellationToken);

    public Task<PatientVisit?> GetByIdAsync(Guid visitId, CancellationToken cancellationToken)
        => _dbContext.PatientVisits
            .Include(v => v.Consultations)
            .FirstOrDefaultAsync(v => v.Id == visitId, cancellationToken);

    public async Task<IReadOnlyList<PatientVisit>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
        => await _dbContext.PatientVisits
            .Include(v => v.Consultations)
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<PatientVisit> Items, int TotalCount)> GetPagedAsync(PatientVisitListQuery query, CancellationToken cancellationToken)
    {
        var visits = _dbContext.PatientVisits.Include(v => v.Consultations).AsQueryable();

        // Model binding produces DateTime.Kind = Unspecified for a plain query-string date —
        // Npgsql rejects that against a `timestamp with time zone` column ("only UTC is
        // supported"), so it must be normalized before use, not passed through as-is.
        if (query.From.HasValue)
        {
            var from = DateTime.SpecifyKind(query.From.Value, DateTimeKind.Utc);
            visits = visits.Where(v => v.CreatedAt >= from);
        }

        if (query.To.HasValue)
        {
            var to = DateTime.SpecifyKind(query.To.Value, DateTimeKind.Utc);
            visits = visits.Where(v => v.CreatedAt <= to);
        }

        var totalCount = await visits.CountAsync(cancellationToken);
        var items = await visits
            .OrderByDescending(v => v.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<PatientVisit?> GetByConsultationIdAsync(Guid consultationId, CancellationToken cancellationToken)
        => _dbContext.PatientVisits
            .Include(v => v.Consultations)
            .FirstOrDefaultAsync(v => v.Consultations.Any(c => c.Id == consultationId), cancellationToken);

    public async Task<(IReadOnlyList<OpdPatientListRow> Items, int TotalCount)> GetOpdPatientListPagedAsync(OpdPatientListQuery query, CancellationToken cancellationToken)
    {
        var rows = BuildOpdRowQuery(query.From, query.To, query.DepartmentId, query.ConsultantId, query.Search);

        if (query.Status.HasValue)
        {
            rows = rows.Where(r => r.Consultation.Status == query.Status.Value);
        }

        var totalCount = await rows.CountAsync(cancellationToken);

        var items = await rows
            .OrderBy(r => r.Consultation.AppointmentTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new OpdPatientListRow(
                r.Consultation.Id,
                r.Visit.Id,
                r.Patient.Id,
                r.Patient.Uhid,
                r.Patient.FirstName,
                r.Patient.LastName,
                r.Patient.DateOfBirth,
                r.Patient.Gender,
                r.Consultation.AppointmentTime,
                r.Visit.AppointmentTypeId,
                r.Consultation.DepartmentId,
                r.Consultation.ConsultantId,
                r.Consultation.Status))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<OpdPatientListRow>> GetOpdConsultationSummaryRowsAsync(OpdConsultationSummaryQuery query, CancellationToken cancellationToken)
    {
        var rows = BuildOpdRowQuery(query.From, query.To, query.DepartmentId, consultantId: null, search: null);

        return await rows
            .Select(r => new OpdPatientListRow(
                r.Consultation.Id,
                r.Visit.Id,
                r.Patient.Id,
                r.Patient.Uhid,
                r.Patient.FirstName,
                r.Patient.LastName,
                r.Patient.DateOfBirth,
                r.Patient.Gender,
                r.Consultation.AppointmentTime,
                r.Visit.AppointmentTypeId,
                r.Consultation.DepartmentId,
                r.Consultation.ConsultantId,
                r.Consultation.Status))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Shared PatientVisitConsultation+PatientVisit+Patient join behind both OPD read
    /// methods above — a SelectMany over PatientVisit.Consultations (there's no DbSet of its
    /// own, see PatientsDbContext's own comment) joined to Patients, the same "cross-aggregate
    /// query within one DbContext" pattern PatientRepository.BuildFilteredQuery already uses
    /// for its own PatientVisits lookups.</summary>
    private IQueryable<OpdRow> BuildOpdRowQuery(
        DateTime? from, DateTime? to, Guid? departmentId, Guid? consultantId, string? search)
    {
        var rows =
            from visit in _dbContext.PatientVisits
            from consultation in visit.Consultations
            join patient in _dbContext.Patients on visit.PatientId equals patient.Id
            select new OpdRow(consultation, visit, patient);

        // Model binding produces DateTime.Kind = Unspecified for a plain query-string date —
        // Npgsql rejects that against a `timestamp with time zone` column ("only UTC is
        // supported"), so it must be normalized before use — same fix as
        // PatientVisitRepository.GetPagedAsync/PatientRepository.BuildFilteredQuery above.
        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
            rows = rows.Where(r => r.Consultation.AppointmentTime >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            rows = rows.Where(r => r.Consultation.AppointmentTime <= toUtc);
        }

        if (departmentId.HasValue)
        {
            rows = rows.Where(r => r.Consultation.DepartmentId == departmentId.Value);
        }

        if (consultantId.HasValue)
        {
            rows = rows.Where(r => r.Consultation.ConsultantId == consultantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            rows = rows.Where(r =>
                EF.Functions.ILike(r.Patient.FirstName, term) ||
                EF.Functions.ILike(r.Patient.LastName, term) ||
                EF.Functions.ILike(r.Patient.Uhid, term) ||
                EF.Functions.ILike(r.Patient.PrimaryPhone, term));
        }

        return rows;
    }

    /// <summary>Named projection type for BuildOpdRowQuery — plain C# tuple element names
    /// don't survive an EF Core `Skip`/`Where` chain reassigning the queryable's static type
    /// reliably across every LINQ provider version, so this is a tiny private record instead.</summary>
    private sealed record OpdRow(PatientVisitConsultation Consultation, PatientVisit Visit, Patient Patient);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
