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

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
