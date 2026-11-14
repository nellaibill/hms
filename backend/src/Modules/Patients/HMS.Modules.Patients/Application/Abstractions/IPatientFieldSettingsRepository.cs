using HMS.Modules.Patients.Domain;

namespace HMS.Modules.Patients.Application.Abstractions;

/// <summary>The hospital's single patient-field-settings row (ADR-135). Null until it is first saved.</summary>
internal interface IPatientFieldSettingsRepository
{
    Task<PatientFieldSettings?> GetAsync(CancellationToken cancellationToken);

    Task AddAsync(PatientFieldSettings settings, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
