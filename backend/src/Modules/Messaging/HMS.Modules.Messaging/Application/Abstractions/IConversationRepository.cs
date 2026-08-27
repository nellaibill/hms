using HMS.Modules.Messaging.Domain;

namespace HMS.Modules.Messaging.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/Architecture.md — Application never references EF Core types.
/// </summary>
internal interface IConversationRepository
{
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken);

    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Batched lookup for GetMyConversationsAsync's conversation list — one query
    /// instead of one per row (mirrors HMS.Modules.Notifications' identical
    /// INotificationRepository.GetManyByIdsAsync).</summary>
    Task<IReadOnlyList<Conversation>> GetManyByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
