using FluentValidation;
using HMS.Modules.Notifications.Contracts;

namespace HMS.Modules.Notifications.Application.Validators;

internal class CreateNotificationTemplateRequestValidator : AbstractValidator<CreateNotificationTemplateRequest>
{
    public CreateNotificationTemplateRequestValidator()
    {
        RuleFor(x => x.TemplateKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.BodyTemplate).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Subject).MaximumLength(500);
        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Subject is required for an Email template.")
            .When(x => x.Channel == NotificationChannel.Email);
    }
}

internal class UpdateNotificationTemplateRequestValidator : AbstractValidator<UpdateNotificationTemplateRequest>
{
    public UpdateNotificationTemplateRequestValidator()
    {
        RuleFor(x => x.BodyTemplate).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Subject).MaximumLength(500);
    }
}
