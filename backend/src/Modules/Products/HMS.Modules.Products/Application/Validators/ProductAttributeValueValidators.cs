using FluentValidation;
using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Products.Application.Validators;

internal class CreateProductAttributeValueRequestValidator : AbstractValidator<CreateProductAttributeValueRequest>
{
    public CreateProductAttributeValueRequestValidator()
    {
        RuleFor(x => x.AttributeId).NotEmpty();
        RuleFor(x => x.AttributeValue).NotEmpty();
    }
}

internal class UpdateProductAttributeValueRequestValidator : AbstractValidator<UpdateProductAttributeValueRequest>
{
    public UpdateProductAttributeValueRequestValidator()
    {
        RuleFor(x => x.AttributeValue).NotEmpty();
    }
}
