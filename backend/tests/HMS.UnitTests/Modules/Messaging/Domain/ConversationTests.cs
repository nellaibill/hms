using FluentAssertions;
using HMS.Modules.Messaging.Contracts;
using HMS.Modules.Messaging.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Messaging.Domain;

public class ConversationTests
{
    [Fact]
    public void CreateOneToOne_SetsTypeAndNullTitle()
    {
        var actorId = Guid.NewGuid();

        var conversation = Conversation.CreateOneToOne(actorId);

        conversation.Type.Should().Be(ConversationType.OneToOne);
        conversation.Title.Should().BeNull();
        conversation.CreatedBy.Should().Be(actorId);
        conversation.LastMessageAt.Should().BeNull();
    }

    [Fact]
    public void CreateGroup_SetsTypeAndTitle()
    {
        var conversation = Conversation.CreateGroup("Ward 4 Team", null);

        conversation.Type.Should().Be(ConversationType.Group);
        conversation.Title.Should().Be("Ward 4 Team");
    }

    [Fact]
    public void CreateGroup_WithNullOrWhitespaceTitle_Throws()
    {
        var act = () => Conversation.CreateGroup("   ", null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RenameGroup_UpdatesTitleAndSetsUpdatedAudit()
    {
        var conversation = Conversation.CreateGroup("Ward 4 Team", null);
        var updatedBy = Guid.NewGuid();

        conversation.RenameGroup("Ward 4 Night Shift", updatedBy);

        conversation.Title.Should().Be("Ward 4 Night Shift");
        conversation.UpdatedBy.Should().Be(updatedBy);
        conversation.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void RenameGroup_OnOneToOneConversation_Throws()
    {
        var conversation = Conversation.CreateOneToOne(null);

        var act = () => conversation.RenameGroup("New Title", null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TouchLastMessage_SetsLastMessageAt()
    {
        var conversation = Conversation.CreateOneToOne(null);
        var sentAt = DateTime.UtcNow;

        conversation.TouchLastMessage(sentAt);

        conversation.LastMessageAt.Should().Be(sentAt);
    }
}
