using FluentValidation;
using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Masters.Application.Validators;

internal class CreateBrandRequestValidator : AbstractValidator<CreateBrandRequest>
{
    public CreateBrandRequestValidator()
    {
        RuleFor(x => x.BrandCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.BrandName).NotEmpty().MaximumLength(150);
    }
}

internal class UpdateBrandRequestValidator : AbstractValidator<UpdateBrandRequest>
{
    public UpdateBrandRequestValidator()
    {
        RuleFor(x => x.BrandName).NotEmpty().MaximumLength(150);
    }
}
