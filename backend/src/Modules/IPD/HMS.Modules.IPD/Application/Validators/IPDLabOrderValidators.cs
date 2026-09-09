using FluentValidation;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.IPD.Application.Validators;

internal class CreatePlaceLabOrderRequestValidator : AbstractValidator<CreatePlaceLabOrderRequest>
{
    public CreatePlaceLabOrderRequestValidator()
    {
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).Must(l => l.ServiceId.HasValue ^ l.PackageId.HasValue)
            .WithMessage("Each line must specify exactly one of ServiceId or PackageId.");
    }
}
