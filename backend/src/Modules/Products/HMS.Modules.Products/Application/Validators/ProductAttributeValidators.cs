using FluentValidation;
using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Products.Application.Validators;

internal class CreateProductAttributeRequestValidator : AbstractValidator<CreateProductAttributeRequest>
{
    public CreateProductAttributeRequestValidator()
    {
        RuleFor(x => x.AttributeCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AttributeName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DataType).NotEmpty().MaximumLength(20);
    }
}

internal class UpdateProductAttributeRequestValidator : AbstractValidator<UpdateProductAttributeRequest>
{
    public UpdateProductAttributeRequestValidator()
    {
        RuleFor(x => x.AttributeName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DataType).NotEmpty().MaximumLength(20);
    }
}
