using FluentAssertions;
using HMS.Modules.Identity.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Identity.Domain;

public class UserTests
{
    [Fact]
    public void Create_SetsIsActiveTrueAndCreatedAudit()
    {
        var actorId = Guid.NewGuid();

        var user = User.Create("Ada", "Lovelace", "ada@example.com", "+1 555 0100", actorId);

        user.IsActive.Should().BeTrue();
        user.IsDeleted.Should().BeFalse();
        user.CreatedBy.Should().Be(actorId);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.PhoneNumber.Should().Be("+1 555 0100");
        user.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsNamesAndNormalizesEmailToLowercase()
    {
        var user = User.Create("  Ada  ", "  Lovelace  ", "  ADA@EXAMPLE.COM  ", null, null);

        user.FirstName.Should().Be("Ada");
        user.LastName.Should().Be("Lovelace");
        user.Email.Should().Be("ada@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidFirstName_ThrowsArgumentException(string invalidFirstName)
    {
        var act = () => User.Create(invalidFirstName, "Lovelace", "ada@example.com", null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidLastName_ThrowsArgumentException(string invalidLastName)
    {
        var act = () => User.Create("Ada", invalidLastName, "ada@example.com", null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidEmail_ThrowsArgumentException(string invalidEmail)
    {
        var act = () => User.Create("Ada", "Lovelace", invalidEmail, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateProfile_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var user = User.Create("Ada", "Lovelace", "ada@example.com", null, null);
        var updatedBy = Guid.NewGuid();

        user.UpdateProfile("Grace", "Hopper", "+1 999 0100", updatedBy);

        user.FirstName.Should().Be("Grace");
        user.LastName.Should().Be("Hopper");
        user.PhoneNumber.Should().Be("+1 999 0100");
        user.UpdatedBy.Should().Be(updatedBy);
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ChangeEmail_NormalizesAndSetsUpdatedAudit()
    {
        var user = User.Create("Ada", "Lovelace", "ada@example.com", null, null);
        var updatedBy = Guid.NewGuid();

        user.ChangeEmail("NEW@EXAMPLE.COM", updatedBy);

        user.Email.Should().Be("new@example.com");
        user.UpdatedBy.Should().Be(updatedBy);
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsNoOp()
    {
        var user = User.Create("Ada", "Lovelace", "ada@example.com", null, null);

        user.Activate(Guid.NewGuid());

        user.IsActive.Should().BeTrue();
        user.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesStateAndUpdatesAudit()
    {
        var user = User.Create("Ada", "Lovelace", "ada@example.com", null, null);
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
        var user = User.Create("Ada", "Lovelace", "ada@example.com", null, null);
        user.Deactivate(Guid.NewGuid());
        var updatedAtAfterFirstDeactivate = user.UpdatedAt;

        user.Deactivate(Guid.NewGuid());

        user.UpdatedAt.Should().Be(updatedAtAfterFirstDeactivate);
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAudit()
    {
        var user = User.Create("Ada", "Lovelace", "ada@example.com", null, null);
        var deletedBy = Guid.NewGuid();

        user.SoftDelete(deletedBy);

        user.IsDeleted.Should().BeTrue();
        user.DeletedBy.Should().Be(deletedBy);
        user.DeletedAt.Should().NotBeNull();
    }
}
