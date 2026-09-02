using FluentAssertions;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
using HMS.Modules.Platform.Infrastructure.Seed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Platform.Infrastructure.Seed;

public class PlatformDataSeederTests
{
    private readonly IPlatformUserRepository _platformUserRepository = Substitute.For<IPlatformUserRepository>();
    private readonly IPlatformPasswordHasher _passwordHasher = Substitute.For<IPlatformPasswordHasher>();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();

    public PlatformDataSeederTests()
    {
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns(callInfo => $"hashed:{callInfo.Arg<string>()}");
    }

    private static IConfiguration DefaultConfiguration(string? defaultConnectionString = "Host=localhost;Database=hms_legacy") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(defaultConnectionString is null
                ? []
                : new Dictionary<string, string?> { ["ConnectionStrings:Default"] = defaultConnectionString })
            .Build();

    private PlatformDataSeeder CreateSut(
        PlatformAdminSeedOptions? platformAdminOptions = null,
        LegacyTenantSeedOptions? legacyTenantOptions = null,
        IConfiguration? configuration = null)
    {
        platformAdminOptions ??= new PlatformAdminSeedOptions
        {
            FullName = "Platform Support",
            Email = "support@yourhms.com",
            Password = "PlatformAdmin@123",
        };
        legacyTenantOptions ??= new LegacyTenantSeedOptions();

        return new PlatformDataSeeder(
            _platformUserRepository,
            _passwordHasher,
            _tenantRepository,
            Options.Create(platformAdminOptions),
            Options.Create(legacyTenantOptions),
            configuration ?? DefaultConfiguration(),
            NullLogger<PlatformDataSeeder>.Instance);
    }

    [Fact]
    public async Task SeedAsync_WhenPlatformAdminAndLegacyTenantAreMissing_CreatesBoth()
    {
        _platformUserRepository.GetByEmailAsync("support@yourhms.com", Arg.Any<CancellationToken>()).Returns((PlatformUser?)null);
        _tenantRepository.GetByHospitalCodeAsync("legacy", Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        PlatformUser? createdUser = null;
        _platformUserRepository.When(x => x.AddAsync(Arg.Any<PlatformUser>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => createdUser = callInfo.Arg<PlatformUser>());

        Tenant? createdTenant = null;
        _tenantRepository.When(x => x.AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => createdTenant = callInfo.Arg<Tenant>());

        var sut = CreateSut();

        await sut.SeedAsync(CancellationToken.None);

        await _platformUserRepository.Received(1).AddAsync(Arg.Any<PlatformUser>(), Arg.Any<CancellationToken>());
        await _platformUserRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be("support@yourhms.com");
        createdUser.Role.Should().Be(PlatformRole.SuperAdmin);

        await _tenantRepository.Received(1).AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
        await _tenantRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        createdTenant.Should().NotBeNull();
        createdTenant!.HospitalCode.Should().Be("legacy");
        createdTenant.DatabaseName.Should().Be("hms_legacy");
    }

    [Fact]
    public async Task SeedAsync_WhenPlatformAdminAlreadyExists_DoesNotCreateAnother()
    {
        var existing = PlatformUser.Create("Existing Admin", "support@yourhms.com", "existing-hash", PlatformRole.SuperAdmin, null);
        _platformUserRepository.GetByEmailAsync("support@yourhms.com", Arg.Any<CancellationToken>()).Returns(existing);
        _tenantRepository.GetByHospitalCodeAsync("legacy", Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var sut = CreateSut();

        await sut.SeedAsync(CancellationToken.None);

        await _platformUserRepository.DidNotReceive().AddAsync(Arg.Any<PlatformUser>(), Arg.Any<CancellationToken>());
        await _platformUserRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedAsync_WhenPlatformAdminSeedConfigIsIncomplete_SkipsAdminCreationButStillSeedsTheLegacyTenant()
    {
        _tenantRepository.GetByHospitalCodeAsync("legacy", Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        var sut = CreateSut(platformAdminOptions: new PlatformAdminSeedOptions
        {
            FullName = "Platform Support",
            Email = "support@yourhms.com",
            Password = string.Empty, // Missing password — the incomplete-config case.
        });

        var act = () => sut.SeedAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _platformUserRepository.DidNotReceive().AddAsync(Arg.Any<PlatformUser>(), Arg.Any<CancellationToken>());

        // The legacy tenant seed is independent of whether the Platform Admin's config is set.
        await _tenantRepository.Received(1).AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedAsync_WhenLegacyTenantAlreadyExists_DoesNotCreateAnother()
    {
        _platformUserRepository.GetByEmailAsync("support@yourhms.com", Arg.Any<CancellationToken>()).Returns((PlatformUser?)null);
        var existingTenant = Tenant.Create("Legacy Hospital", "legacy", "0000000000", "legacy-tenant@hms.local", "n/a", "n/a", "n/a", "000000", "hms_legacy", 40000, null);
        _tenantRepository.GetByHospitalCodeAsync("legacy", Arg.Any<CancellationToken>()).Returns(existingTenant);

        var sut = CreateSut();

        await sut.SeedAsync(CancellationToken.None);

        await _tenantRepository.DidNotReceive().AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
        await _tenantRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_WhenDefaultConnectionStringIsMissing_Throws()
    {
        var act = () => CreateSut(configuration: DefaultConfiguration(defaultConnectionString: null));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WhenDefaultConnectionStringHasNoDatabase_Throws()
    {
        var act = () => CreateSut(configuration: DefaultConfiguration(defaultConnectionString: "Host=localhost"));

        act.Should().Throw<InvalidOperationException>();
    }
}
