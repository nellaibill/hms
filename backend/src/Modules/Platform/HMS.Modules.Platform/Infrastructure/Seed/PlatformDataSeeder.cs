using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HMS.Modules.Platform.Infrastructure.Seed;

/// <summary>
/// Idempotent startup data seeder: ensures the one default Platform Admin account exists,
/// and (HMS Multi-Tenancy Phase C) that the pre-existing single dev database
/// (ConnectionStrings:Default) has a corresponding platform.tenants row — see
/// LegacyTenantSeedOptions's own doc comment. Runs once per startup, after
/// PlatformDbContext.Database.Migrate() — see Program.cs.
/// </summary>
internal sealed class PlatformDataSeeder
{
    private readonly IPlatformUserRepository _repository;
    private readonly IPlatformPasswordHasher _passwordHasher;
    private readonly ITenantRepository _tenantRepository;
    private readonly PlatformAdminSeedOptions _options;
    private readonly LegacyTenantSeedOptions _legacyTenantOptions;
    private readonly string _legacyDatabaseName;
    private readonly ILogger<PlatformDataSeeder> _logger;

    public PlatformDataSeeder(
        IPlatformUserRepository repository,
        IPlatformPasswordHasher passwordHasher,
        ITenantRepository tenantRepository,
        IOptions<PlatformAdminSeedOptions> options,
        IOptions<LegacyTenantSeedOptions> legacyTenantOptions,
        IConfiguration configuration,
        ILogger<PlatformDataSeeder> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _tenantRepository = tenantRepository;
        _options = options.Value;
        _legacyTenantOptions = legacyTenantOptions.Value;
        var defaultConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' configuration value.");
        _legacyDatabaseName = new NpgsqlConnectionStringBuilder(defaultConnectionString).Database
            ?? throw new InvalidOperationException("'ConnectionStrings:Default' does not specify a Database.");
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken, bool seedLegacyTenant = true)
    {
        await SeedPlatformAdminAsync(cancellationToken);
        if (seedLegacyTenant)
        {
            await SeedLegacyTenantAsync(cancellationToken);
        }
    }

    private async Task SeedPlatformAdminAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.FullName) ||
            string.IsNullOrWhiteSpace(_options.Email) ||
            string.IsNullOrWhiteSpace(_options.Password))
        {
            _logger.LogWarning(
                "Skipped Platform Admin seeding: 'PlatformAdminSeed' configuration is incomplete (FullName, Email and Password are required).");
            return;
        }

        var existing = await _repository.GetByEmailAsync(_options.Email, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var user = PlatformUser.Create(
            _options.FullName,
            _options.Email,
            _passwordHasher.HashPassword(_options.Password),
            PlatformRole.SuperAdmin,
            createdBy: null);

        await _repository.AddAsync(user, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded Platform Admin user '{Email}'", user.Email);
    }

    private async Task SeedLegacyTenantAsync(CancellationToken cancellationToken)
    {
        var existing = await _tenantRepository.GetByHospitalCodeAsync(_legacyTenantOptions.HospitalCode, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var tenant = Tenant.Create(
            _legacyTenantOptions.HospitalName,
            _legacyTenantOptions.HospitalCode,
            _legacyTenantOptions.MobileNumber,
            _legacyTenantOptions.Email,
            _legacyTenantOptions.Address,
            _legacyTenantOptions.City,
            _legacyTenantOptions.State,
            _legacyTenantOptions.Pincode,
            _legacyDatabaseName,
            createdBy: null);

        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _tenantRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded legacy tenant '{HospitalCode}' -> database '{DatabaseName}'",
            tenant.HospitalCode,
            tenant.DatabaseName);
    }
}
