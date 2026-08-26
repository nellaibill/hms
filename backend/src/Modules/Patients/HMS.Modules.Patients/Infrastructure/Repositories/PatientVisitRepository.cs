using HMS.Modules.Patients.Application.Abstractions;
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

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
