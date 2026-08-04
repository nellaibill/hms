using FluentValidation;
using HMS.Modules.HR.Contracts;

namespace HMS.Modules.HR.Application.Validators;

/// <summary>
/// Only what the spec requires — Code, Name, StartTime, EndTime — nothing else.
/// </summary>
internal class CreateShiftRequestValidator : AbstractValidator<CreateShiftRequest>
{
    public CreateShiftRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.StartTime).NotNull();
        RuleFor(x => x.EndTime).NotNull();
    }
}

internal class UpdateShiftRequestValidator : AbstractValidator<UpdateShiftRequest>
{
    public UpdateShiftRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.StartTime).NotNull();
        RuleFor(x => x.EndTime).NotNull();
    }
}
