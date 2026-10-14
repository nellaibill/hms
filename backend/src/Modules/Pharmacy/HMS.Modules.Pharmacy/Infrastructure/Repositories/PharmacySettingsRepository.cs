using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Pharmacy.Infrastructure.Repositories;

internal class PharmacySettingsRepository : IPharmacySettingsRepository
{
    private readonly PharmacyDbContext _dbContext;

    public PharmacySettingsRepository(PharmacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PharmacySettings?> GetAsync(CancellationToken cancellationToken)
        => _dbContext.Settings.OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(PharmacySettings settings, CancellationToken cancellationToken)
        => await _dbContext.Settings.AddAsync(settings, cancellationToken);
}

internal class PharmacySettingsConfiguration : IEntityTypeConfiguration<PharmacySettings>
{
    public void Configure(EntityTypeBuilder<PharmacySettings> builder)
    {
        Configurations.ConfigurationExtensions.MapDocument(builder, "pharmacy_settings");
    }
}
