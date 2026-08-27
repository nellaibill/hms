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

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
