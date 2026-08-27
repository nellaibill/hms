using HMS.Modules.Messaging.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Messaging.Application;

/// <summary>Public for the same CS0051 reason as HMS.Modules.Identity's IUserService —
/// ConversationsController's public constructor takes this as a dependency.</summary>
public interface IConversationService
{
    Task<Result<ConversationResponse>> CreateAsync(CreateConversationRequest request, Guid actorId, CancellationToken cancellationToken);

    /// <summary>Always scoped to <paramref name="userId"/> — there is no way to fetch
    /// another user's conversations through this method, by design.</summary>
    Task<IReadOnlyList<ConversationResponse>> GetMyConversationsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Fails with <see cref="ConversationErrorCodes.NotParticipant"/> if
    /// <paramref name="callerId"/> isn't a participant of <paramref name="conversationId"/>
    /// (or that conversation doesn't exist at all) — see that error code's own doc comment.</summary>
    Task<Result<PagedResult<MessageResponse>>> GetMessagesAsync(Guid conversationId, Guid callerId, int page, int pageSize, CancellationToken cancellationToken);

    Task<Result<MessageResponse>> SendMessageAsync(Guid conversationId, Guid senderId, SendMessageRequest request, CancellationToken cancellationToken);

    Task<Result> MarkReadAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken);
}
