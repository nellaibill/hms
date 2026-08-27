using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Messaging.Application;
using HMS.Modules.Messaging.Contracts;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Modules.Messaging.Endpoints;

/// <summary>
/// The Messaging module's HTTP surface — start a conversation, list "mine," read/send
/// messages, mark read. Every per-conversation action's only gate is participant membership
/// (checked in ConversationService, not here) — no RequirePermission beyond [Authorize],
/// since Doctor→Nurse/Nurse→Doctor/Staff→Staff messaging isn't a clinically gated action
/// (see the design doc's Security section). Creating a conversation requires
/// "engagement.view" — any authenticated staff member with access to the engagement
/// surface at all, not an elevated action. Every action requires the tenant to have the
/// "messages-and-notifications" feature enabled.
/// </summary>
[ApiController]
[RequireFeature("messages-and-notifications")]
[Route("api/v1/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly IValidator<CreateConversationRequest> _createValidator;
    private readonly IValidator<SendMessageRequest> _sendMessageValidator;

    public ConversationsController(
        IConversationService conversationService,
        IValidator<CreateConversationRequest> createValidator,
        IValidator<SendMessageRequest> sendMessageValidator)
    {
        _conversationService = conversationService;
        _createValidator = createValidator;
        _sendMessageValidator = sendMessageValidator;
    }

    /// <summary>Starts a one-to-one or group conversation. The caller is added as a
    /// participant automatically. A one-to-one request that already has a conversation
    /// between the same two users returns that existing conversation instead of creating a
    /// duplicate.</summary>
    /// <response code="201">The conversation was created (or the existing one was returned).</response>
    /// <response code="400">The request failed validation, or the participant count doesn't match the conversation type.</response>
    [Authorize]
    [RequirePermission("engagement.view")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConversationRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _conversationService.CreateAsync(request, actorId: User.GetUserId()!.Value, cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : StatusCode(StatusCodes.Status201Created, new ApiResponse<ConversationResponse> { Data = result.Value });
    }

    /// <summary>The caller's conversations, most recently active first, with unread counts.</summary>
    /// <response code="200">The caller's conversations.</response>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var conversations = await _conversationService.GetMyConversationsAsync(User.GetUserId()!.Value, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<ConversationResponse>> { Data = conversations });
    }

    /// <summary>Paged message history, oldest-to-newest within the page.</summary>
    /// <response code="200">A page of messages.</response>
    /// <response code="403">The caller isn't a participant of this conversation.</response>
    [Authorize]
    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid id, [FromQuery] PagedRequest query, CancellationToken cancellationToken)
    {
        var result = await _conversationService.GetMessagesAsync(id, User.GetUserId()!.Value, query.Page, query.PageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return MapFailure(result.ErrorCode!, result.Error!);
        }

        var meta = new PaginationMeta
        {
            Page = result.Value!.Page,
            PageSize = result.Value.PageSize,
            TotalCount = result.Value.TotalCount,
            TotalPages = result.Value.TotalPages,
        };

        return Ok(new ApiResponse<IReadOnlyList<MessageResponse>> { Data = result.Value.Items, Meta = meta });
    }

    /// <summary>Sends a message.</summary>
    /// <response code="201">The message was sent.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="403">The caller isn't a participant of this conversation.</response>
    [Authorize]
    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var validation = await _sendMessageValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationError(validation));
        }

        var result = await _conversationService.SendMessageAsync(id, senderId: User.GetUserId()!.Value, request, cancellationToken);
        return !result.IsSuccess
            ? MapFailure(result.ErrorCode!, result.Error!)
            : StatusCode(StatusCodes.Status201Created, new ApiResponse<MessageResponse> { Data = result.Value });
    }

    /// <summary>Bumps the caller's read position to now for this conversation.</summary>
    /// <response code="204">The conversation was marked read.</response>
    /// <response code="403">The caller isn't a participant of this conversation.</response>
    [Authorize]
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var result = await _conversationService.MarkReadAsync(id, User.GetUserId()!.Value, cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result.ErrorCode!, result.Error!);
    }

    private IActionResult MapFailure(string errorCode, string message)
    {
        var status = errorCode switch
        {
            ConversationErrorCodes.NotParticipant => StatusCodes.Status403Forbidden,
            ConversationErrorCodes.InvalidParticipants => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest,
        };

        var error = new ApiErrorResponse
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = HttpContext.GetCorrelationId(),
            Timestamp = DateTime.UtcNow,
        };

        return StatusCode(status, error);
    }

    private ApiErrorResponse BuildValidationError(ValidationResult validation) => new()
    {
        ErrorCode = "VALIDATION.FAILED",
        Message = "One or more validation errors occurred.",
        ValidationErrors = validation.Errors
            .Select(e => new ValidationErrorItem { Field = e.PropertyName, Message = e.ErrorMessage })
            .ToList(),
        CorrelationId = HttpContext.GetCorrelationId(),
        Timestamp = DateTime.UtcNow,
    };
}
