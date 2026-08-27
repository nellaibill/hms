using FluentValidation;
using HMS.Modules.Messaging.Contracts;

namespace HMS.Modules.Messaging.Application.Validators;

internal class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}
