using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HMS.Modules.Platform.Infrastructure.Seed;

/// <summary>
/// Idempotent startup data seeder: ensures the one default Platform Admin account exists.
/// Runs once per startup, after PlatformDbContext.Database.Migrate() — see Program.cs.
/// Mirrors HMS.Modules.Identity.Infrastructure.Seed.IdentityDataSeeder's shape.
/// </summary>
internal sealed class PlatformDataSeeder
{
    private readonly IPlatformUserRepository _repository;
    private readonly IPlatformPasswordHasher _passwordHasher;
    private readonly PlatformAdminSeedOptions _options;
    private readonly ILogger<PlatformDataSeeder> _logger;

    public PlatformDataSeeder(
        IPlatformUserRepository repository,
        IPlatformPasswordHasher passwordHasher,
        IOptions<PlatformAdminSeedOptions> options,
        ILogger<PlatformDataSeeder> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
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
            createdBy: null);

        await _repository.AddAsync(user, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded Platform Admin user '{Email}'", user.Email);
    }
}
