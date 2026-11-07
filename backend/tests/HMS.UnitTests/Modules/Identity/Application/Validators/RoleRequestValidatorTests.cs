using FluentAssertions;
using HMS.Modules.Identity.Application.Validators;
using HMS.Modules.Identity.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Identity.Application.Validators;

public class RoleRequestValidatorTests
{
    private static UpdateRoleRequest Request(params string[] hiddenMenus) => new()
    {
        Name = "Receptionist",
        IsActive = true,
        PermissionKeys = ["patient-management.view"],
        HiddenMenus = hiddenMenus,
    };

    [Fact]
    public void MenuPaths_AreValid()
    {
        new UpdateRoleRequestValidator().Validate(Request("/clinical/ipd", "/diagnostics/lab")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void NoHiddenMenus_IsValid()
    {
        new UpdateRoleRequestValidator().Validate(Request()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("clinical/ipd")]
    [InlineData("")]
    public void SomethingOtherThanAMenuPath_IsRejected(string menu)
    {
        var result = new UpdateRoleRequestValidator().Validate(Request(menu));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateRoleRequest.HiddenMenus));
    }

    [Fact]
    public void ACreateRequest_ChecksHiddenMenusToo()
    {
        var request = new CreateRoleRequest { Name = "Receptionist", PermissionKeys = ["patient-management.view"], HiddenMenus = ["ipd"] };

        new CreateRoleRequestValidator().Validate(request).IsValid.Should().BeFalse();
    }
}
