using FluentAssertions;
using HMS.Modules.Identity.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Identity.Domain;

public class UserTests
{
    // Domain-level tests only construct a User in isolation — whether this Guid maps to a
    // real Role is UserService's concern (verified against a mocked IRoleRepository in
    // UserServiceTests), not User's own invariant.
    private static readonly Guid RoleId = Guid.NewGuid();

    [Fact]
    public void Create_SetsIsActiveTrueAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", "+1 555 0100", RoleId, actorId);

        user.IsActive.Should().BeTrue();
        user.IsDeleted.Should().BeFalse();
        user.EmailVerified.Should().BeFalse();
        user.LastLoginAt.Should().BeNull();
        user.PasswordHash.Should().BeNull();
        user.RoleId.Should().Be(RoleId);
        user.CreatedBy.Should().Be(actorId);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.PhoneNumber.Should().Be("+1 555 0100");
        user.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsNamesAndNormalizesUsernameAndEmailToLowercase()
    {
        var user = User.Create("  Ada.Lovelace  ", "  Ada  ", "  Lovelace  ", "  ADA@EXAMPLE.COM  ", null, RoleId, null);

        user.Username.Should().Be("ada.lovelace");
        user.FirstName.Should().Be("Ada");
        user.LastName.Should().Be("Lovelace");
        user.Email.Should().Be("ada@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidUsername_ThrowsArgumentException(string invalidUsername)
    {
        var act = () => User.Create(invalidUsername, "Ada", "Lovelace", "ada@example.com", null, RoleId, null);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidFirstName_ThrowsArgumentException(string invalidFirstName)
    {
        var act = () => User.Create("ada.lovelace", invalidFirstName, "Lovelace", "ada@example.com", null, RoleId, null);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidLastName_ThrowsArgumentException(string invalidLastName)
    {
        var act = () => User.Create("ada.lovelace", "Ada", invalidLastName, "ada@example.com", null, RoleId, null);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidEmail_ThrowsArgumentException(string invalidEmail)
    {
        var act = () => User.Create("ada.lovelace", "Ada", "Lovelace", invalidEmail, null, RoleId, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateProfile_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);
        var updatedBy = Guid.NewGuid();

        user.UpdateProfile("Grace", "Hopper", "+1 999 0100", updatedBy);

        user.FirstName.Should().Be("Grace");
        user.LastName.Should().Be("Hopper");
        user.PhoneNumber.Should().Be("+1 999 0100");
        user.UpdatedBy.Should().Be(updatedBy);
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ChangeUsername_NormalizesAndSetsUpdatedAudit()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);
        var updatedBy = Guid.NewGuid();

        user.ChangeUsername("ADA.NEW", updatedBy);

        user.Username.Should().Be("ada.new");
        user.UpdatedBy.Should().Be(updatedBy);
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ChangeEmail_NormalizesAndSetsUpdatedAudit()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);
        var updatedBy = Guid.NewGuid();

        user.ChangeEmail("NEW@EXAMPLE.COM", updatedBy);

        user.Email.Should().Be("new@example.com");
        user.UpdatedBy.Should().Be(updatedBy);
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ChangeEmail_ResetsEmailVerified()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);
        user.VerifyEmail(null);
        user.EmailVerified.Should().BeTrue();

        user.ChangeEmail("new@example.com", null);

        user.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public void ChangeRole_SetsRoleIdAndUpdatedAudit()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);
        var newRoleId = Guid.NewGuid();
        var updatedBy = Guid.NewGuid();

        user.ChangeRole(newRoleId, updatedBy);

        user.RoleId.Should().Be(newRoleId);
        user.UpdatedBy.Should().Be(updatedBy);
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetPasswordHash_SetsHashAndUpdatedAudit()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);
        var updatedBy = Guid.NewGuid();

        user.SetPasswordHash("a-computed-hash", updatedBy);

        user.PasswordHash.Should().Be("a-computed-hash");
        user.UpdatedBy.Should().Be(updatedBy);
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetProfilePhoto_SetsProfilePhotoUrlAndUpdatedAudit()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);
        var updatedBy = Guid.NewGuid();

        user.SetProfilePhoto("uploads/users/019fb880.jpg", updatedBy);

        user.ProfilePhotoUrl.Should().Be("uploads/users/019fb880.jpg");
        user.UpdatedBy.Should().Be(updatedBy);
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetProfilePhoto_WhenReplacingAnExistingPhoto_OverwritesUrl()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);
        user.SetProfilePhoto("uploads/users/019fb880.jpg", null);

        user.SetProfilePhoto("uploads/users/019fb880.png", null);

        user.ProfilePhotoUrl.Should().Be("uploads/users/019fb880.png");
    }

    [Fact]
    public void VerifyEmail_WhenAlreadyVerified_IsNoOp()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);
        user.VerifyEmail(Guid.NewGuid());
        var updatedAtAfterFirstVerify = user.UpdatedAt;

        user.VerifyEmail(Guid.NewGuid());

        user.EmailVerified.Should().BeTrue();
        user.UpdatedAt.Should().Be(updatedAtAfterFirstVerify);
    }

    [Fact]
    public void RecordLogin_SetsLastLoginAtWithoutTouchingUpdatedAudit()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);
        var loginAt = DateTime.UtcNow;

        user.RecordLogin(loginAt);

        user.LastLoginAt.Should().Be(loginAt);
        user.UpdatedAt.Should().BeNull();
        user.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsNoOp()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);

        user.Activate(Guid.NewGuid());

        user.IsActive.Should().BeTrue();
        user.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesStateAndUpdatesAudit()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);
        var deactivatedBy = Guid.NewGuid();
        var activatedBy = Guid.NewGuid();

        user.Deactivate(deactivatedBy);
        user.IsActive.Should().BeFalse();
        user.UpdatedBy.Should().Be(deactivatedBy);

        user.Activate(activatedBy);
        user.IsActive.Should().BeTrue();
        user.UpdatedBy.Should().Be(activatedBy);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsNoOp()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);
        user.Deactivate(Guid.NewGuid());
        var updatedAtAfterFirstDeactivate = user.UpdatedAt;

        user.Deactivate(Guid.NewGuid());

        user.UpdatedAt.Should().Be(updatedAtAfterFirstDeactivate);
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAudit()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, RoleId, null);
        var deletedBy = Guid.NewGuid();

        user.SoftDelete(deletedBy);

        user.IsDeleted.Should().BeTrue();
        user.DeletedBy.Should().Be(deletedBy);
        user.DeletedAt.Should().NotBeNull();
    }
}
