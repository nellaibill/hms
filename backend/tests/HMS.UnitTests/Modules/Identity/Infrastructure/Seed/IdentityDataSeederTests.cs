using FluentAssertions;
using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Domain;
using HMS.Modules.Identity.Infrastructure.Seed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Identity.Infrastructure.Seed;

public class IdentityDataSeederTests
{
    private readonly IPermissionRepository _permissionRepository = Substitute.For<IPermissionRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    private static readonly IReadOnlyList<Permission> SeedPermissions =
    [
        Permission.CreateSeed(Guid.NewGuid(), "patient-management", "view", "patient-management.view", "View", 1),
        Permission.CreateSeed(Guid.NewGuid(), "patient-management", "create", "patient-management.create", "Create", 1),
    ];

    public IdentityDataSeederTests()
    {
        _permissionRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(SeedPermissions);
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns(callInfo => $"hashed:{callInfo.Arg<string>()}");
    }

    private IdentityDataSeeder CreateSut(SuperAdminSeedOptions? options = null)
    {
        options ??= new SuperAdminSeedOptions
        {
            Username = "superadmin",
            FirstName = "Super",
            LastName = "Admin",
            Email = "superadmin@hms.local",
            Password = "SuperAdmin@123",
        };

        return new IdentityDataSeeder(
            _permissionRepository,
            _roleRepository,
            _userRepository,
            _passwordHasher,
            Options.Create(options),
            NullLogger<IdentityDataSeeder>.Instance);
    }

    [Fact]
    public async Task SeedAsync_WhenRoleAndUserAreMissing_CreatesRoleWithEveryPermissionAndCreatesUser()
    {
        _roleRepository.GetByNameAsync("Super Admin", Arg.Any<CancellationToken>()).Returns((Role?)null);
        _userRepository.GetByUsernameAsync("superadmin", Arg.Any<CancellationToken>()).Returns((User?)null);

        Role? createdRole = null;
        _roleRepository.When(x => x.AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => createdRole = callInfo.Arg<Role>());

        User? createdUser = null;
        _userRepository.When(x => x.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => createdUser = callInfo.Arg<User>());

        var sut = CreateSut();

        await sut.SeedAsync(CancellationToken.None);

        await _roleRepository.Received(1).AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
        await _roleRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        createdRole.Should().NotBeNull();
        createdRole!.Name.Should().Be("Super Admin");
        createdRole.IsSystemRole.Should().BeTrue();
        createdRole.RolePermissions.Select(rp => rp.PermissionId)
            .Should().BeEquivalentTo(SeedPermissions.Select(p => p.Id));

        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        createdUser.Should().NotBeNull();
        createdUser!.Username.Should().Be("superadmin");
        createdUser.RoleId.Should().Be(createdRole.Id);
        createdUser.PasswordHash.Should().Be("hashed:SuperAdmin@123");
    }

    [Fact]
    public async Task SeedAsync_WhenRoleAlreadyExists_DoesNotCreateAnotherRoleAndReusesItsId()
    {
        var existingRole = Role.Create("Super Admin", "Existing", true, 0, null);
        _roleRepository.GetByNameAsync("Super Admin", Arg.Any<CancellationToken>()).Returns(existingRole);
        _userRepository.GetByUsernameAsync("superadmin", Arg.Any<CancellationToken>()).Returns((User?)null);

        User? createdUser = null;
        _userRepository.When(x => x.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => createdUser = callInfo.Arg<User>());

        var sut = CreateSut();

        await sut.SeedAsync(CancellationToken.None);

        await _roleRepository.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
        await _permissionRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());

        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        createdUser!.RoleId.Should().Be(existingRole.Id);
    }

    [Fact]
    public async Task SeedAsync_WhenUserAlreadyExists_DoesNotCreateAnotherUser()
    {
        var existingRole = Role.Create("Super Admin", "Existing", true, 0, null);
        _roleRepository.GetByNameAsync("Super Admin", Arg.Any<CancellationToken>()).Returns(existingRole);

        var existingUser = User.Create("superadmin", "Existing", "Admin", "existing@hms.local", null, existingRole.Id, null);
        _userRepository.GetByUsernameAsync("superadmin", Arg.Any<CancellationToken>()).Returns(existingUser);

        var sut = CreateSut();

        await sut.SeedAsync(CancellationToken.None);

        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeedAsync_WhenSuperAdminSeedConfigurationIsIncomplete_SkipsUserCreationWithoutThrowing()
    {
        _roleRepository.GetByNameAsync("Super Admin", Arg.Any<CancellationToken>()).Returns((Role?)null);

        var sut = CreateSut(new SuperAdminSeedOptions
        {
            Username = "superadmin",
            FirstName = "Super",
            LastName = "Admin",
            Email = "superadmin@hms.local",
            Password = string.Empty, // Missing password — the incomplete-config case.
        });

        var act = () => sut.SeedAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());

        // The role still gets seeded independently of whether user credentials are configured.
        await _roleRepository.Received(1).AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }
}
