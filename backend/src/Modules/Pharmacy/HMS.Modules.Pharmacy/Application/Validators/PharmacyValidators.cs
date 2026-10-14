using FluentValidation;
using HMS.Modules.Pharmacy.Contracts;

namespace HMS.Modules.Pharmacy.Application.Validators;

internal class CreateStoreRequestValidator : AbstractValidator<CreateStoreRequest>
{
    public CreateStoreRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StoreType).IsInEnum();
    }
}

internal class UpdateStoreRequestValidator : AbstractValidator<UpdateStoreRequest>
{
    public UpdateStoreRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StoreType).IsInEnum();
    }
}

internal class CreateStockReceiptRequestValidator : AbstractValidator<CreateStockReceiptRequest>
{
    public CreateStockReceiptRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ProductBatchId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

internal class CreateDispenseRequestValidator : AbstractValidator<CreateDispenseRequest>
{
    public CreateDispenseRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ProductBatchId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

internal class DispenseCartLineRequestValidator : AbstractValidator<DispenseCartLineRequest>
{
    public DispenseCartLineRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ProductBatchId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}

internal class CreateDispenseCartRequestValidator : AbstractValidator<CreateDispenseCartRequest>
{
    public CreateDispenseCartRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one item to the cart.");
        RuleForEach(x => x.Lines).SetValidator(new DispenseCartLineRequestValidator());

        // Two rows dispensing the same product/batch are rejected rather than silently merged
        // into one combined quantity — merging would hide what the operator actually typed.
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => (l.ProductId, l.ProductBatchId)).Distinct().Count() == lines.Count)
            .WithMessage("The same product/batch appears more than once in the cart.")
            .When(x => x.Lines.Count > 0);
    }
}

internal class GoodsReceiptLineRequestValidator : AbstractValidator<GoodsReceiptLineRequest>
{
    public GoodsReceiptLineRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.BatchNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ExpiryDate).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FreeQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.Quantity + x.FreeQuantity > 0).WithName("Quantity").WithMessage("Enter a quantity or a free quantity.");
        RuleFor(x => x.PurchaseRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Mrp).GreaterThan(0);
        RuleFor(x => x.SellingRate).GreaterThan(0).LessThanOrEqualTo(x => x.Mrp).When(x => x.SellingRate.HasValue)
            .WithMessage("Selling rate cannot be above MRP.");
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.GstRatePercent).InclusiveBetween(0, 100).When(x => x.GstRatePercent.HasValue);
    }
}

internal class CreateGoodsReceiptRequestValidator : AbstractValidator<CreateGoodsReceiptRequest>
{
    public CreateGoodsReceiptRequestValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.SupplierInvoiceNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.OtherCharges).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one line.");
        RuleFor(x => x.Lines).Must(l => l.Count <= 300).WithMessage("A goods receipt can hold at most 300 lines.");
        RuleForEach(x => x.Lines).SetValidator(new GoodsReceiptLineRequestValidator());
    }
}

internal class ImportProductsRequestValidator : AbstractValidator<ImportProductsRequest>
{
    public ImportProductsRequestValidator()
    {
        RuleFor(x => x.Rows).NotEmpty().WithMessage("There are no rows to import.");
        RuleFor(x => x.Rows).Must(r => r.Count <= 200).WithMessage("Send at most 200 rows at a time.");
    }
}
