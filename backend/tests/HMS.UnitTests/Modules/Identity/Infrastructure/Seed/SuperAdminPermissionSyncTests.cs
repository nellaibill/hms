using FluentAssertions;
using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Domain;
using HMS.Modules.Identity.Infrastructure.Seed;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Identity.Infrastructure.Seed;

public class SuperAdminPermissionSyncTests
{
    private readonly IPermissionRepository _permissionRepository = Substitute.For<IPermissionRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();

    private static readonly Permission PatientsView =
        Permission.CreateSeed(Guid.NewGuid(), "patient-management", "view", "patient-management.view", "View", 1);

    // Stands in for a permission a later migration added (e.g. discharge-summary.view) after
    // the hospital's Super Admin role was provisioned.
    private static readonly Permission DischargeSummaryView =
        Permission.CreateSeed(Guid.NewGuid(), "discharge-summary", "view", "discharge-summary.view", "View", 12);

    public SuperAdminPermissionSyncTests()
    {
        _permissionRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { PatientsView, DischargeSummaryView });
    }

    private SuperAdminPermissionSync CreateSut() => new(_permissionRepository, _roleRepository);

    [Fact]
    public async Task SyncAsync_WhenRoleWasProvisionedBeforeNewPermissionsExisted_AddsThemAndSaves()
    {
        var role = Role.Create("Super Admin", "Provisioned", true, 0, null);
        role.ReplacePermissions([PatientsView.Id]);
        _roleRepository.GetByNameAsync("Super Admin", Arg.Any<CancellationToken>()).Returns(role);

        var (syncedRole, resynced) = await CreateSut().SyncAsync(CancellationToken.None);

        resynced.Should().BeTrue();
        syncedRole.Should().BeSameAs(role);
        role.RolePermissions.Select(rp => rp.PermissionId)
            .Should().BeEquivalentTo(new[] { PatientsView.Id, DischargeSummaryView.Id });
        await _roleRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_WhenRoleAlreadyHoldsEveryPermission_DoesNotSave()
    {
        var role = Role.Create("Super Admin", "Provisioned", true, 0, null);
        role.ReplacePermissions([PatientsView.Id, DischargeSummaryView.Id]);
        _roleRepository.GetByNameAsync("Super Admin", Arg.Any<CancellationToken>()).Returns(role);

        var (syncedRole, resynced) = await CreateSut().SyncAsync(CancellationToken.None);

        resynced.Should().BeFalse();
        syncedRole.Should().BeSameAs(role);
        await _roleRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_WhenNoSuperAdminRoleExistsYet_ReturnsNullWithoutCreatingOne()
    {
        // Provisioning migrates a brand-new hospital database before creating its Super Admin.
        _roleRepository.GetByNameAsync("Super Admin", Arg.Any<CancellationToken>()).Returns((Role?)null);

        var (syncedRole, resynced) = await CreateSut().SyncAsync(CancellationToken.None);

        syncedRole.Should().BeNull();
        resynced.Should().BeFalse();
        await _roleRepository.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
        await _roleRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
