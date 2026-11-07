using FluentAssertions;
using HMS.Modules.Identity.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Identity.Domain;

public class RoleTests
{
    [Fact]
    public void NewRole_HidesNoMenus()
    {
        Role.Create("Receptionist", null, false, 0, null).HiddenMenus.Should().BeEmpty();
    }

    [Fact]
    public void SetHiddenMenus_TrimsDropsBlanksAndRepeats_InAStableOrder()
    {
        var role = Role.Create("Receptionist", null, false, 0, null);

        role.SetHiddenMenus([" /diagnostics/lab ", "/clinical/ipd", "", "   ", "/clinical/ipd"]);

        role.HiddenMenus.Should().Equal("/clinical/ipd", "/diagnostics/lab");
    }

    [Fact]
    public void SetHiddenMenus_WithNothing_ShowsEveryMenuAgain()
    {
        var role = Role.Create("Receptionist", null, false, 0, null);
        role.SetHiddenMenus(["/clinical/ipd"]);

        role.SetHiddenMenus(null);

        role.HiddenMenus.Should().BeEmpty();
    }
}
