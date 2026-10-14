using HMS.Modules.Pharmacy.Domain;

namespace HMS.Modules.Pharmacy.Application.Abstractions;

/// <summary>The hospital's single pharmacy-settings row. Null until it is first saved.</summary>
internal interface IPharmacySettingsRepository
{
    Task<PharmacySettings?> GetAsync(CancellationToken cancellationToken);

    Task AddAsync(PharmacySettings settings, CancellationToken cancellationToken);
}
