using FluentAssertions;
using HMS.Modules.Messaging.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Messaging.Domain;

public class ConversationParticipantTests
{
    [Fact]
    public void Create_SetsFieldsAndLeavesLastReadAtNull()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var joinedAt = DateTime.UtcNow;

        var participant = ConversationParticipant.Create(conversationId, userId, joinedAt, null);

        participant.ConversationId.Should().Be(conversationId);
        participant.UserId.Should().Be(userId);
        participant.JoinedAt.Should().Be(joinedAt);
        participant.LastReadAt.Should().BeNull();
    }

    [Fact]
    public void MarkRead_SetsLastReadAt()
    {
        var participant = ConversationParticipant.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, null);
        var readAt = DateTime.UtcNow;

        participant.MarkRead(readAt);

        participant.LastReadAt.Should().Be(readAt);
    }

    [Fact]
    public void MarkRead_WithEarlierTimestamp_DoesNotMoveBackwards()
    {
        var participant = ConversationParticipant.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, null);
        var laterRead = DateTime.UtcNow;
        participant.MarkRead(laterRead);

        participant.MarkRead(laterRead.AddMinutes(-5));

        participant.LastReadAt.Should().Be(laterRead);
    }
}
