using FluentValidation;
using HMS.Modules.Messaging.Contracts;

namespace HMS.Modules.Messaging.Application.Validators;

/// <summary>
/// Shape-only validation (non-empty participant list, a title for Group). The exact
/// participant-count rules (exactly one other for OneToOne, at least two others for Group)
/// depend on combining this request with the caller's own id, so those live in
/// ConversationService.CreateAsync instead, returning a Result failure rather than a
/// validation error.
/// </summary>
internal class CreateConversationRequestValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationRequestValidator()
    {
        RuleFor(x => x.ParticipantUserIds).NotEmpty().WithMessage("At least one other participant is required.");
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Title is required for a group conversation.")
            .When(x => x.Type == ConversationType.Group);
    }
}
