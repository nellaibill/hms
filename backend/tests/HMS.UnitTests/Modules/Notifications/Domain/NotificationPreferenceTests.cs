using FluentAssertions;
using HMS.Modules.Notifications.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Notifications.Domain;

public class NotificationPreferenceTests
{
    [Fact]
    public void Create_DefaultsToInAppAndEmailOn_SmsOff()
    {
        var userId = Guid.NewGuid();

        var preference = NotificationPreference.Create(userId, "Appointment", null);

        preference.UserId.Should().Be(userId);
        preference.Category.Should().Be("appointment");
        preference.InAppEnabled.Should().BeTrue();
        preference.EmailEnabled.Should().BeTrue();
        preference.SmsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Create_NormalizesCategoryToLowercase()
    {
        var preference = NotificationPreference.Create(Guid.NewGuid(), "BILLING", null);

        preference.Category.Should().Be("billing");
    }

    [Fact]
    public void Create_WithNullOrWhitespaceCategory_Throws()
    {
        var act = () => NotificationPreference.Create(Guid.NewGuid(), "  ", null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateChannels_UpdatesFieldsAndSetsUpdatedAudit()
    {
        var preference = NotificationPreference.Create(Guid.NewGuid(), "billing", null);
        var updatedBy = Guid.NewGuid();

        preference.UpdateChannels(inAppEnabled: true, emailEnabled: false, smsEnabled: true, updatedBy);

        preference.EmailEnabled.Should().BeFalse();
        preference.SmsEnabled.Should().BeTrue();
        preference.UpdatedBy.Should().Be(updatedBy);
        preference.UpdatedAt.Should().NotBeNull();
    }
}
