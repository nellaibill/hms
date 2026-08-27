using FluentAssertions;
using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Notifications.Domain;

public class NotificationDeliveryTests
{
    [Fact]
    public void Create_StartsPendingWithZeroAttempts()
    {
        var recipientId = Guid.NewGuid();

        var delivery = NotificationDelivery.Create(recipientId, NotificationChannel.Email, null);

        delivery.NotificationRecipientId.Should().Be(recipientId);
        delivery.Channel.Should().Be(NotificationChannel.Email);
        delivery.Status.Should().Be(DeliveryStatus.Pending);
        delivery.Attempts.Should().Be(0);
    }

    [Fact]
    public void Create_WithInAppChannel_Throws()
    {
        // InApp delivery has no NotificationDelivery row — NotificationRecipient existing
        // already means "delivered" (see NotificationRecipient's doc comment).
        var act = () => NotificationDelivery.Create(Guid.NewGuid(), NotificationChannel.InApp, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkSent_SetsStatusAndSentAt_AndIncrementsAttempts()
    {
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), NotificationChannel.Sms, null);
        var sentAt = DateTime.UtcNow;

        delivery.MarkSent(sentAt);

        delivery.Status.Should().Be(DeliveryStatus.Sent);
        delivery.SentAt.Should().Be(sentAt);
        delivery.Attempts.Should().Be(1);
        delivery.LastError.Should().BeNull();
    }

    [Fact]
    public void MarkFailed_SetsStatusAndError_AndIncrementsAttempts()
    {
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), NotificationChannel.Email, null);

        delivery.MarkFailed("SMTP timeout");

        delivery.Status.Should().Be(DeliveryStatus.Failed);
        delivery.LastError.Should().Be("SMTP timeout");
        delivery.Attempts.Should().Be(1);
    }

    [Fact]
    public void MarkFailed_TwiceInARow_AccumulatesAttempts()
    {
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), NotificationChannel.Email, null);

        delivery.MarkFailed("first failure");
        delivery.MarkFailed("second failure");

        delivery.Attempts.Should().Be(2);
        delivery.LastError.Should().Be("second failure");
    }

    [Fact]
    public void MarkSkipped_SetsStatusAndReason_WithoutIncrementingAttempts()
    {
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), NotificationChannel.Sms, null);

        delivery.MarkSkipped("Recipient opted out of SMS for this category");

        delivery.Status.Should().Be(DeliveryStatus.Skipped);
        delivery.LastError.Should().Be("Recipient opted out of SMS for this category");
        delivery.Attempts.Should().Be(0);
    }
}
