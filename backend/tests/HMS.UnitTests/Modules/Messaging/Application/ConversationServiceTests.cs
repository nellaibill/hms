using FluentAssertions;
using HMS.Modules.Messaging.Application;
using HMS.Modules.Messaging.Application.Abstractions;
using HMS.Modules.Messaging.Contracts;
using HMS.Modules.Messaging.Domain;
using HMS.Modules.Notifications.Application;
using HMS.Modules.Notifications.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Messaging.Application;

public class ConversationServiceTests
{
    private readonly IConversationRepository _conversationRepository = Substitute.For<IConversationRepository>();
    private readonly IConversationParticipantRepository _participantRepository = Substitute.For<IConversationParticipantRepository>();
    private readonly IMessageRepository _messageRepository = Substitute.For<IMessageRepository>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly ConversationService _sut;

    public ConversationServiceTests()
    {
        _notificationService
            .NotifyAsync(Arg.Any<NotifyRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<NotificationBroadcastResponse>.Success(new NotificationBroadcastResponse()));

        _sut = new ConversationService(_conversationRepository, _participantRepository, _messageRepository, _notificationService, NullLogger<ConversationService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_OneToOne_WithExactlyOneOtherParticipant_CreatesConversation()
    {
        var actorId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        _participantRepository.FindOneToOneConversationIdAsync(actorId, otherId, Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var request = new CreateConversationRequest { Type = ConversationType.OneToOne, ParticipantUserIds = [otherId] };

        var result = await _sut.CreateAsync(request, actorId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Type.Should().Be(ConversationType.OneToOne);
        result.Value.ParticipantUserIds.Should().BeEquivalentTo([actorId, otherId]);
        await _conversationRepository.Received(1).AddAsync(Arg.Any<Conversation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_OneToOne_WithZeroOrMultipleOthers_ReturnsInvalidParticipantsFailure()
    {
        var actorId = Guid.NewGuid();
        var request = new CreateConversationRequest { Type = ConversationType.OneToOne, ParticipantUserIds = [Guid.NewGuid(), Guid.NewGuid()] };

        var result = await _sut.CreateAsync(request, actorId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ConversationErrorCodes.InvalidParticipants);
        await _conversationRepository.DidNotReceive().AddAsync(Arg.Any<Conversation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_OneToOne_WhenConversationAlreadyExists_ReturnsExistingWithoutCreatingDuplicate()
    {
        var actorId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var existing = Conversation.CreateOneToOne(actorId);
        var existingParticipants = new[]
        {
            ConversationParticipant.Create(existing.Id, actorId, DateTime.UtcNow, actorId),
            ConversationParticipant.Create(existing.Id, otherId, DateTime.UtcNow, actorId),
        };

        _participantRepository.FindOneToOneConversationIdAsync(actorId, otherId, Arg.Any<CancellationToken>()).Returns(existing.Id);
        _conversationRepository.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        _participantRepository.GetByConversationAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existingParticipants);
        _messageRepository.GetUnreadCountAsync(existing.Id, actorId, Arg.Any<DateTime?>(), Arg.Any<CancellationToken>()).Returns(0);

        var request = new CreateConversationRequest { Type = ConversationType.OneToOne, ParticipantUserIds = [otherId] };

        var result = await _sut.CreateAsync(request, actorId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(existing.Id);
        await _conversationRepository.DidNotReceive().AddAsync(Arg.Any<Conversation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_Group_WithFewerThanTwoOthers_ReturnsInvalidParticipantsFailure()
    {
        var actorId = Guid.NewGuid();
        var request = new CreateConversationRequest { Type = ConversationType.Group, Title = "Ward Team", ParticipantUserIds = [Guid.NewGuid()] };

        var result = await _sut.CreateAsync(request, actorId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ConversationErrorCodes.InvalidParticipants);
    }

    [Fact]
    public async Task CreateAsync_Group_WithEnoughParticipants_CreatesConversation()
    {
        var actorId = Guid.NewGuid();
        var request = new CreateConversationRequest
        {
            Type = ConversationType.Group,
            Title = "Ward Team",
            ParticipantUserIds = [Guid.NewGuid(), Guid.NewGuid()],
        };

        var result = await _sut.CreateAsync(request, actorId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Ward Team");
        result.Value.ParticipantUserIds.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetMessagesAsync_ForNonParticipant_ReturnsNotParticipantFailure()
    {
        _participantRepository
            .GetByConversationAndUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ConversationParticipant?)null);

        var result = await _sut.GetMessagesAsync(Guid.NewGuid(), Guid.NewGuid(), 1, 20, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ConversationErrorCodes.NotParticipant);
    }

    [Fact]
    public async Task GetMessagesAsync_ForParticipant_ReturnsPagedMessages()
    {
        var conversationId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        _participantRepository
            .GetByConversationAndUserAsync(conversationId, callerId, Arg.Any<CancellationToken>())
            .Returns(ConversationParticipant.Create(conversationId, callerId, DateTime.UtcNow, null));
        var message = Message.Create(conversationId, callerId, "Hello", callerId);
        _messageRepository
            .GetByConversationAsync(conversationId, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Message>([message], 1, 20, 1));

        var result = await _sut.GetMessagesAsync(conversationId, callerId, 1, 20, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(m => m.Body == "Hello");
    }

    [Fact]
    public async Task SendMessageAsync_ForNonParticipant_ReturnsNotParticipantFailure()
    {
        _participantRepository
            .GetByConversationAndUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ConversationParticipant?)null);

        var result = await _sut.SendMessageAsync(Guid.NewGuid(), Guid.NewGuid(), new SendMessageRequest { Body = "Hi" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ConversationErrorCodes.NotParticipant);
        await _messageRepository.DidNotReceive().AddAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessageAsync_ForParticipant_SendsAndNotifiesOtherParticipantsOnly()
    {
        var senderId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var conversation = Conversation.CreateOneToOne(senderId);
        var conversationId = conversation.Id;

        _participantRepository
            .GetByConversationAndUserAsync(conversationId, senderId, Arg.Any<CancellationToken>())
            .Returns(ConversationParticipant.Create(conversationId, senderId, DateTime.UtcNow, null));
        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>()).Returns(conversation);
        _participantRepository.GetByConversationAsync(conversationId, Arg.Any<CancellationToken>()).Returns(
        [
            ConversationParticipant.Create(conversationId, senderId, DateTime.UtcNow, null),
            ConversationParticipant.Create(conversationId, otherId, DateTime.UtcNow, null),
        ]);

        var result = await _sut.SendMessageAsync(conversationId, senderId, new SendMessageRequest { Body = "Patient needs review" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _messageRepository.Received(1).AddAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>());
        await _notificationService.Received(1).NotifyAsync(
            Arg.Is<NotifyRequest>(r => r.RecipientUserIds.Count == 1 && r.RecipientUserIds[0] == otherId),
            senderId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkReadAsync_ForNonParticipant_ReturnsNotParticipantFailure()
    {
        _participantRepository
            .GetByConversationAndUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ConversationParticipant?)null);

        var result = await _sut.MarkReadAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ConversationErrorCodes.NotParticipant);
    }

    [Fact]
    public async Task MarkReadAsync_ForParticipant_MarksReadAndSucceeds()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var participant = ConversationParticipant.Create(conversationId, userId, DateTime.UtcNow, null);
        _participantRepository.GetByConversationAndUserAsync(conversationId, userId, Arg.Any<CancellationToken>()).Returns(participant);

        var result = await _sut.MarkReadAsync(conversationId, userId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        participant.LastReadAt.Should().NotBeNull();
        await _participantRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
