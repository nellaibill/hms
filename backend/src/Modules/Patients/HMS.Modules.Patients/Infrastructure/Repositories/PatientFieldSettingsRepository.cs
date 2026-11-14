using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Patients.Infrastructure.Repositories;

internal class PatientFieldSettingsRepository : IPatientFieldSettingsRepository
{
    private readonly PatientsDbContext _dbContext;

    public PatientFieldSettingsRepository(PatientsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Oldest first, in case two first-ever saves raced and both inserted a row: every reader
    // then agrees on the same one.
    public Task<PatientFieldSettings?> GetAsync(CancellationToken cancellationToken)
        => _dbContext.PatientFieldSettings.OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(PatientFieldSettings settings, CancellationToken cancellationToken)
        => await _dbContext.PatientFieldSettings.AddAsync(settings, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}
