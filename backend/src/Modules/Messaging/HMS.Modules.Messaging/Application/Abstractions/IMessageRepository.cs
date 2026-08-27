using HMS.Modules.Messaging.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Messaging.Application.Abstractions;

internal interface IMessageRepository
{
    Task AddAsync(Message message, CancellationToken cancellationToken);

    /// <summary>Paged history for one conversation, oldest-to-newest within the page —
    /// <paramref name="page"/> 1 is the most recent page (mirrors typical chat-scrollback
    /// pagination, not the ascending-from-the-start convention every other list endpoint in
    /// this codebase uses, since a conversation is read newest-first).</summary>
    Task<PagedResult<Message>> GetByConversationAsync(Guid conversationId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Unread count for one participant — messages after <paramref name="after"/>
    /// (that participant's LastReadAt) sent by someone other than them.</summary>
    Task<int> GetUnreadCountAsync(Guid conversationId, Guid excludingSenderId, DateTime? after, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
