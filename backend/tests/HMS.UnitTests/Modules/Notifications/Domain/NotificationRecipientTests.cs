using FluentAssertions;
using HMS.Modules.Notifications.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Notifications.Domain;

public class NotificationRecipientTests
{
    [Fact]
    public void Create_StartsUnread()
    {
        var notificationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var recipient = NotificationRecipient.Create(notificationId, userId, null);

        recipient.NotificationId.Should().Be(notificationId);
        recipient.UserId.Should().Be(userId);
        recipient.IsRead.Should().BeFalse();
        recipient.ReadAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsRead_SetsIsReadAndReadAt()
    {
        var recipient = NotificationRecipient.Create(Guid.NewGuid(), Guid.NewGuid(), null);
        var readAt = DateTime.UtcNow;

        recipient.MarkAsRead(readAt);

        recipient.IsRead.Should().BeTrue();
        recipient.ReadAt.Should().Be(readAt);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_IsNoOp()
    {
        var recipient = NotificationRecipient.Create(Guid.NewGuid(), Guid.NewGuid(), null);
        var firstReadAt = DateTime.UtcNow;
        recipient.MarkAsRead(firstReadAt);

        recipient.MarkAsRead(firstReadAt.AddMinutes(5));

        recipient.ReadAt.Should().Be(firstReadAt);
    }
}
