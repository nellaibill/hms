using FluentValidation;
using HMS.Modules.Notifications.Contracts;

namespace HMS.Modules.Notifications.Application.Validators;

/// <summary>
/// Server-side validation for POST /api/v1/notifications (the admin manual-send endpoint) —
/// the same request shape a later phase's cross-module callers use in-process, so this is
/// the one place these rules need to live.
/// </summary>
internal class NotifyRequestValidator : AbstractValidator<NotifyRequest>
{
    public NotifyRequestValidator()
    {
        RuleFor(x => x.TemplateKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        // Body is optional (falls back to InApp template rendering — see NotifyRequest.Body's
        // own doc comment); only its length is bounded when supplied.
        RuleFor(x => x.Body).MaximumLength(4000);
        RuleFor(x => x.SourceModule).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RecipientUserIds).NotEmpty().WithMessage("At least one recipient is required.");
    }
}
