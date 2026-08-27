using FluentValidation;
using HMS.Modules.Notifications.Contracts;

namespace HMS.Modules.Notifications.Application.Validators;

internal class UpdateNotificationPreferenceRequestValidator : AbstractValidator<UpdateNotificationPreferenceRequest>
{
    public UpdateNotificationPreferenceRequestValidator()
    {
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
    }
}
