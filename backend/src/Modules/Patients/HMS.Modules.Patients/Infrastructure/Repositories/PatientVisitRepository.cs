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
        // Deliberately not factored through a shared helper returning IQueryable<TSomeRecord>:
        // EF Core can't translate a Where/OrderBy applied *after* a Select that already
        // projected into a user-defined record/class — it inlines the constructor call at the
        // point of member access instead of pushing the predicate down to the underlying
        // columns, producing "could not be translated" (confirmed live against a real query).
        // Anonymous types don't have this problem, so every filter below stays on the plain
        // `select new { consultation, visit, patient }` shape all the way through to the single
        // final Select into OpdPatientListRow — which, as the terminal projection, is never
        // followed by more query composition and is therefore safe. This duplicates the
        // From/To/Department/Search filters against GetOpdConsultationSummaryRowsAsync below
        // rather than sharing a helper, since a helper can't return/re-accept this anonymous
        // type across a method boundary without reintroducing the same problem.
        var rows =
            from visit in _dbContext.PatientVisits
            from consultation in visit.Consultations
            join patient in _dbContext.Patients on visit.PatientId equals patient.Id
            select new { consultation, visit, patient };

        // Model binding produces DateTime.Kind = Unspecified for a plain query-string date —
        // Npgsql rejects that against a `timestamp with time zone` column ("only UTC is
        // supported"), so it must be normalized before use — same fix as GetPagedAsync above.
        if (query.From.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(query.From.Value, DateTimeKind.Utc);
            rows = rows.Where(r => r.consultation.AppointmentTime >= fromUtc);
        }

        if (query.To.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(query.To.Value, DateTimeKind.Utc);
            rows = rows.Where(r => r.consultation.AppointmentTime <= toUtc);
        }

        if (query.DepartmentId.HasValue)
        {
            rows = rows.Where(r => r.consultation.DepartmentId == query.DepartmentId.Value);
        }

        if (query.ConsultantId.HasValue)
        {
            rows = rows.Where(r => r.consultation.ConsultantId == query.ConsultantId.Value);
        }

        if (query.Status.HasValue)
        {
            rows = rows.Where(r => r.consultation.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            rows = rows.Where(r =>
                EF.Functions.ILike(r.patient.FirstName, term) ||
                EF.Functions.ILike(r.patient.LastName, term) ||
                EF.Functions.ILike(r.patient.Uhid, term) ||
                EF.Functions.ILike(r.patient.PrimaryPhone, term));
        }

        var totalCount = await rows.CountAsync(cancellationToken);

        var items = await rows
            .OrderBy(r => r.consultation.AppointmentTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new OpdPatientListRow(
                r.consultation.Id,
                r.visit.Id,
                r.patient.Id,
                r.patient.Uhid,
                r.patient.PrimaryPhone,
                r.patient.FirstName,
                r.patient.LastName,
                r.patient.DateOfBirth,
                r.patient.Gender,
                r.consultation.AppointmentTime,
                r.visit.AppointmentTypeId,
                r.consultation.DepartmentId,
                r.consultation.ConsultantId,
                r.consultation.Status))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<OpdPatientListRow>> GetOpdConsultationSummaryRowsAsync(OpdConsultationSummaryQuery query, CancellationToken cancellationToken)
    {
        // See GetOpdPatientListPagedAsync's own comment for why this duplicates the filter
        // logic instead of sharing a helper — an EF Core translation constraint, not a
        // stylistic choice.
        var rows =
            from visit in _dbContext.PatientVisits
            from consultation in visit.Consultations
            join patient in _dbContext.Patients on visit.PatientId equals patient.Id
            select new { consultation, visit, patient };

        if (query.From.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(query.From.Value, DateTimeKind.Utc);
            rows = rows.Where(r => r.consultation.AppointmentTime >= fromUtc);
        }

        if (query.To.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(query.To.Value, DateTimeKind.Utc);
            rows = rows.Where(r => r.consultation.AppointmentTime <= toUtc);
        }

        if (query.DepartmentId.HasValue)
        {
            rows = rows.Where(r => r.consultation.DepartmentId == query.DepartmentId.Value);
        }

        return await rows
            .Select(r => new OpdPatientListRow(
                r.consultation.Id,
                r.visit.Id,
                r.patient.Id,
                r.patient.Uhid,
                r.patient.PrimaryPhone,
                r.patient.FirstName,
                r.patient.LastName,
                r.patient.DateOfBirth,
                r.patient.Gender,
                r.consultation.AppointmentTime,
                r.visit.AppointmentTypeId,
                r.consultation.DepartmentId,
                r.consultation.ConsultantId,
                r.consultation.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<OpdPatientListRow?> GetOpdConsultationDetailAsync(Guid consultationId, CancellationToken cancellationToken)
    {
        // See GetOpdPatientListPagedAsync's own comment for why this duplicates the join/
        // projection instead of sharing a helper — an EF Core translation constraint, not a
        // stylistic choice.
        var rows =
            from visit in _dbContext.PatientVisits
            from consultation in visit.Consultations
            join patient in _dbContext.Patients on visit.PatientId equals patient.Id
            where consultation.Id == consultationId
            select new { consultation, visit, patient };

        return await rows
            .Select(r => new OpdPatientListRow(
                r.consultation.Id,
                r.visit.Id,
                r.patient.Id,
                r.patient.Uhid,
                r.patient.PrimaryPhone,
                r.patient.FirstName,
                r.patient.LastName,
                r.patient.DateOfBirth,
                r.patient.Gender,
                r.consultation.AppointmentTime,
                r.visit.AppointmentTypeId,
                r.consultation.DepartmentId,
                r.consultation.ConsultantId,
                r.consultation.Status))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
