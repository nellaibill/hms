using FluentAssertions;
using HMS.Modules.Messaging.Domain;
using Xunit;

namespace HMS.UnitTests.Modules.Messaging.Domain;

public class MessageTests
{
    [Fact]
    public void Create_SetsFieldsAndCreatedAudit()
    {
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();

        var message = Message.Create(conversationId, senderId, "Patient in bed 4 needs a review.", senderId);

        message.ConversationId.Should().Be(conversationId);
        message.SenderId.Should().Be(senderId);
        message.Body.Should().Be("Patient in bed 4 needs a review.");
        message.CreatedBy.Should().Be(senderId);
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_TrimsBody()
    {
        var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), "  padded  ", null);

        message.Body.Should().Be("padded");
    }

    [Fact]
    public void Create_WithNullOrWhitespaceBody_Throws()
    {
        var act = () => Message.Create(Guid.NewGuid(), Guid.NewGuid(), "   ", null);

        act.Should().Throw<ArgumentException>();
    }
}
