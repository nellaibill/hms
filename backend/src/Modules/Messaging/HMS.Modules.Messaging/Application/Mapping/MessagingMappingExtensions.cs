using HMS.Modules.Messaging.Contracts;
using HMS.Modules.Messaging.Domain;

namespace HMS.Modules.Messaging.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping. A couple of small entities doesn't justify a mapping
/// library (Mapster/AutoMapper) at MVP scale — see docs/DecisionLog.md, ADR-003.
/// </summary>
internal static class MessagingMappingExtensions
{
    public static ConversationResponse ToResponse(this Conversation conversation, IReadOnlyList<Guid> participantUserIds, int unreadCount) => new()
    {
        Id = conversation.Id,
        Type = conversation.Type,
        Title = conversation.Title,
        LastMessageAt = conversation.LastMessageAt,
        ParticipantUserIds = participantUserIds,
        UnreadCount = unreadCount,
        CreatedAt = conversation.CreatedAt,
    };

    public static MessageResponse ToResponse(this Message message) => new()
    {
        Id = message.Id,
        ConversationId = message.ConversationId,
        SenderId = message.SenderId,
        Body = message.Body,
        CreatedAt = message.CreatedAt,
    };
}
