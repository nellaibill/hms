using HMS.Modules.Messaging.Application.Abstractions;
using HMS.Modules.Messaging.Application.Mapping;
using HMS.Modules.Messaging.Contracts;
using HMS.Modules.Messaging.Domain;
using HMS.Modules.Notifications.Application;
using HMS.Modules.Notifications.Contracts;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Messaging.Application;

/// <summary>
/// Orchestrates Messaging use cases: expected failures (not a participant, wrong
/// participant count) are returned as <see cref="Result"/> failures, never thrown — see
/// docs/Architecture.md's exception handling strategy. The participant check
/// (GetByConversationAndUserAsync returning non-null) is the sole authorization boundary for
/// every per-conversation action — see ConversationParticipant's own doc comment.
/// </summary>
internal class ConversationService : IConversationService
{
    /// <summary>A OneToOne conversation has exactly 2 participants (the caller + one other);
    /// a Group needs at least one more than that. Mirrors Conversation's own Create*
    /// factory split.</summary>
    private const int OneToOneParticipantCount = 2;

    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationParticipantRepository _participantRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        IConversationRepository conversationRepository,
        IConversationParticipantRepository participantRepository,
        IMessageRepository messageRepository,
        INotificationService notificationService,
        ILogger<ConversationService> logger)
    {
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _messageRepository = messageRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<ConversationResponse>> CreateAsync(CreateConversationRequest request, Guid actorId, CancellationToken cancellationToken)
    {
        var participantIds = new HashSet<Guid>(request.ParticipantUserIds) { actorId };

        if (request.Type == ConversationType.OneToOne)
        {
            if (participantIds.Count != OneToOneParticipantCount)
            {
                return Result<ConversationResponse>.Failure(
                    ConversationErrorCodes.InvalidParticipants,
                    "A one-to-one conversation requires exactly one other participant.");
            }

            var otherUserId = participantIds.First(id => id != actorId);
            var existingId = await _participantRepository.FindOneToOneConversationIdAsync(actorId, otherUserId, cancellationToken);
            if (existingId is not null)
            {
                var existing = await _conversationRepository.GetByIdAsync(existingId.Value, cancellationToken);
                return Result<ConversationResponse>.Success(await ToResponseAsync(existing!, actorId, cancellationToken));
            }
        }
        else if (participantIds.Count <= OneToOneParticipantCount)
        {
            return Result<ConversationResponse>.Failure(
                ConversationErrorCodes.InvalidParticipants,
                "A group conversation requires at least two other participants.");
        }

        var conversation = request.Type == ConversationType.OneToOne
            ? Conversation.CreateOneToOne(actorId)
            : Conversation.CreateGroup(request.Title!, actorId);

        await _conversationRepository.AddAsync(conversation, cancellationToken);

        var joinedAt = DateTime.UtcNow;
        var participants = participantIds
            .Select(userId => ConversationParticipant.Create(conversation.Id, userId, joinedAt, actorId))
            .ToList();
        await _participantRepository.AddRangeAsync(participants, cancellationToken);

        // One SaveChanges for both the Conversation and its ConversationParticipant rows —
        // both repositories share the same DbContext/scope, so this commits atomically.
        // Mirrors the two-phase-save bug fixed in HMS.Modules.Pharmacy's DispenseService
        // (docs/DecisionLog.md).
        await _conversationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {ConversationType} conversation {ConversationId} with {ParticipantCount} participant(s)", conversation.Type, conversation.Id, participants.Count);

        return Result<ConversationResponse>.Success(conversation.ToResponse(participantIds.ToList(), unreadCount: 0));
    }

    public async Task<IReadOnlyList<ConversationResponse>> GetMyConversationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var myParticipations = await _participantRepository.GetByUserAsync(userId, cancellationToken);
        if (myParticipations.Count == 0)
        {
            return [];
        }

        var conversationIds = myParticipations.Select(p => p.ConversationId).Distinct();
        var conversations = await _conversationRepository.GetManyByIdsAsync(conversationIds, cancellationToken);
        var conversationById = conversations.ToDictionary(c => c.Id);

        var responses = new List<ConversationResponse>();
        foreach (var myParticipation in myParticipations)
        {
            if (!conversationById.TryGetValue(myParticipation.ConversationId, out var conversation))
            {
                continue;
            }

            // One query per conversation for participants and for unread count — accepted
            // N+1 at MVP scale (a user's conversation count is realistically dozens, not
            // thousands), same trade-off already documented elsewhere in this codebase
            // (e.g. HMS.Modules.Pharmacy's product/batch/patient lookups on Dispense list).
            var allParticipants = await _participantRepository.GetByConversationAsync(conversation.Id, cancellationToken);
            var unreadCount = await _messageRepository.GetUnreadCountAsync(conversation.Id, userId, myParticipation.LastReadAt, cancellationToken);

            responses.Add(conversation.ToResponse(allParticipants.Select(p => p.UserId).ToList(), unreadCount));
        }

        return responses.OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt).ToList();
    }

    public async Task<Result<PagedResult<MessageResponse>>> GetMessagesAsync(Guid conversationId, Guid callerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var participant = await _participantRepository.GetByConversationAndUserAsync(conversationId, callerId, cancellationToken);
        if (participant is null)
        {
            return Result<PagedResult<MessageResponse>>.Failure(ConversationErrorCodes.NotParticipant, "You are not a participant of this conversation.");
        }

        var page1 = await _messageRepository.GetByConversationAsync(conversationId, page, pageSize, cancellationToken);
        var mapped = page1.Items.Select(m => m.ToResponse()).ToList();

        return Result<PagedResult<MessageResponse>>.Success(new PagedResult<MessageResponse>(mapped, page1.Page, page1.PageSize, page1.TotalCount));
    }

    public async Task<Result<MessageResponse>> SendMessageAsync(Guid conversationId, Guid senderId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        var participant = await _participantRepository.GetByConversationAndUserAsync(conversationId, senderId, cancellationToken);
        if (participant is null)
        {
            return Result<MessageResponse>.Failure(ConversationErrorCodes.NotParticipant, "You are not a participant of this conversation.");
        }

        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            return Result<MessageResponse>.Failure(ConversationErrorCodes.NotParticipant, "You are not a participant of this conversation.");
        }

        var message = Message.Create(conversationId, senderId, request.Body, senderId);
        await _messageRepository.AddAsync(message, cancellationToken);

        conversation.TouchLastMessage(message.CreatedAt);

        // Shares the same DbContext/scope as the message insert above, so both commit
        // together.
        await _messageRepository.SaveChangesAsync(cancellationToken);

        await NotifyOtherParticipantsAsync(conversation, senderId, message.Body, cancellationToken);

        return Result<MessageResponse>.Success(message.ToResponse());
    }

    public async Task<Result> MarkReadAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        var participant = await _participantRepository.GetByConversationAndUserAsync(conversationId, userId, cancellationToken);
        if (participant is null)
        {
            return Result.Failure(ConversationErrorCodes.NotParticipant, "You are not a participant of this conversation.");
        }

        participant.MarkRead(DateTime.UtcNow);
        await _participantRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<ConversationResponse> ToResponseAsync(Conversation conversation, Guid requestingUserId, CancellationToken cancellationToken)
    {
        var participants = await _participantRepository.GetByConversationAsync(conversation.Id, cancellationToken);
        var myParticipation = participants.First(p => p.UserId == requestingUserId);
        var unreadCount = await _messageRepository.GetUnreadCountAsync(conversation.Id, requestingUserId, myParticipation.LastReadAt, cancellationToken);

        return conversation.ToResponse(participants.Select(p => p.UserId).ToList(), unreadCount);
    }

    /// <summary>
    /// The "one-line hook" into Notifications the design doc calls for — every participant
    /// except the sender gets one in-app notification. No presence/"currently active in this
    /// conversation" tracking exists (out of scope, per the design doc), so this fires
    /// unconditionally rather than only for participants who are away; NotifyAsync's own
    /// preference check still governs whether Email/Sms also go out.
    /// </summary>
    private async Task NotifyOtherParticipantsAsync(Conversation conversation, Guid senderId, string messageBody, CancellationToken cancellationToken)
    {
        var participants = await _participantRepository.GetByConversationAsync(conversation.Id, cancellationToken);
        var recipientIds = participants.Where(p => p.UserId != senderId).Select(p => p.UserId).ToList();
        if (recipientIds.Count == 0)
        {
            return;
        }

        const int previewLength = 200;
        var preview = messageBody.Length > previewLength ? string.Concat(messageBody.AsSpan(0, previewLength), "…") : messageBody;

        await _notificationService.NotifyAsync(
            new NotifyRequest
            {
                TemplateKey = "message.received",
                Category = "message",
                Title = "New message",
                Body = preview,
                SourceModule = "Messaging",
                SourceEntityType = "Conversation",
                SourceEntityId = conversation.Id,
                Severity = NotificationSeverity.Normal,
                RecipientUserIds = recipientIds,
            },
            actorId: senderId,
            cancellationToken);
    }
}
