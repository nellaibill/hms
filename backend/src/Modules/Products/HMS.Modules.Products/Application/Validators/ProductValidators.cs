using FluentValidation;
using HMS.Modules.Products.Contracts;

namespace HMS.Modules.Products.Application.Validators;

internal class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GenericName).MaximumLength(200);
        RuleFor(x => x.HsnCode).MaximumLength(50);
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.ManufacturerId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.SubCategoryId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.UomId).NotEmpty();
        RuleFor(x => x.BaseUomId).NotEmpty();
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStockLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxStockLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Mrp).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
    }
}

internal class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GenericName).MaximumLength(200);
        RuleFor(x => x.HsnCode).MaximumLength(50);
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.ManufacturerId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.SubCategoryId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.UomId).NotEmpty();
        RuleFor(x => x.BaseUomId).NotEmpty();
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStockLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxStockLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Mrp).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
    }
}
