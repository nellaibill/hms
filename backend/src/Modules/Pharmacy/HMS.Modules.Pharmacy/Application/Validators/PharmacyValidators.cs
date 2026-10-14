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

internal class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
    {
        RuleFor(x => x.CustomerName).MaximumLength(150);
        RuleFor(x => x.CustomerPhone).MaximumLength(20);
        RuleFor(x => x.DoctorName).MaximumLength(150);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one item.");
        RuleFor(x => x.Lines).Must(l => l.Count <= 200).WithMessage("A bill can hold at most 200 lines.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.DiscountPercent).InclusiveBetween(0, 100).When(l => l.DiscountPercent.HasValue);
        });
        RuleForEach(x => x.Payments).ChildRules(payment =>
        {
            payment.RuleFor(p => p.Method).IsInEnum();
            payment.RuleFor(p => p.Amount).GreaterThanOrEqualTo(0);
            payment.RuleFor(p => p.ReferenceNo).MaximumLength(100);
        });
        RuleFor(x => x.AdmissionId).NotEmpty().When(x => x.ChargeToAdmission).WithMessage("Choose the admission to charge.");
    }
}

internal class CollectSalePaymentRequestValidator : AbstractValidator<CollectSalePaymentRequest>
{
    public CollectSalePaymentRequestValidator()
    {
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ReferenceNo).MaximumLength(100);
    }
}

internal class CreateSaleReturnRequestValidator : AbstractValidator<CreateSaleReturnRequest>
{
    public CreateSaleReturnRequestValidator()
    {
        RuleFor(x => x.RefundMethod).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Choose at least one item to return.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.SaleLineId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
        RuleFor(x => x.Lines).Must(lines => lines.Select(l => l.SaleLineId).Distinct().Count() == lines.Count)
            .WithMessage("Each bill line can appear once on a return.");
    }
}

internal class HoldBillRequestValidator : AbstractValidator<HoldBillRequest>
{
    public HoldBillRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Payload).NotEmpty().MaximumLength(200_000);
        RuleFor(x => x.ItemCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
    }
}

internal class UpdatePharmacySettingsRequestValidator : AbstractValidator<UpdatePharmacySettingsRequest>
{
    public UpdatePharmacySettingsRequestValidator()
    {
        RuleFor(x => x.PharmacyName).MaximumLength(150);
        RuleFor(x => x.Gstin).Matches("^[0-9]{2}[A-Za-z]{5}[0-9]{4}[A-Za-z][0-9A-Za-z]Z[0-9A-Za-z]$").When(x => !string.IsNullOrWhiteSpace(x.Gstin))
            .WithMessage("GSTIN must be 15 characters, like 33ABCDE1234F1Z5.");
        RuleFor(x => x.DrugLicenceNo).MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.BillFooter).MaximumLength(500);
        RuleFor(x => x.PrintFormat).IsInEnum();
        RuleFor(x => x.MaxDiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.ExpiryAlertDays).InclusiveBetween(1, 730);
    }
}

internal class SavePurchaseOrderRequestValidator : AbstractValidator<SavePurchaseOrderRequest>
{
    public SavePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one product to the order.");
        RuleFor(x => x.Lines).Must(l => l.Count <= 300).WithMessage("An order can hold at most 300 lines.");
        RuleFor(x => x.Lines).Must(lines => lines.Select(l => l.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("Each product can appear once on an order.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.ExpectedRate).GreaterThanOrEqualTo(0).When(l => l.ExpectedRate.HasValue);
        });
    }
}

internal class CreatePurchaseReturnRequestValidator : AbstractValidator<CreatePurchaseReturnRequest>
{
    public CreatePurchaseReturnRequestValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.CreditNoteNo).MaximumLength(50);
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one batch to return.");
        RuleFor(x => x.Lines).Must(lines => lines.Select(l => l.ProductBatchId).Distinct().Count() == lines.Count)
            .WithMessage("Each batch can appear once on a return.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.ProductBatchId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitRate).GreaterThanOrEqualTo(0).When(l => l.UnitRate.HasValue);
        });
    }
}

internal class CreateSupplierPaymentRequestValidator : AbstractValidator<CreateSupplierPaymentRequest>
{
    public CreateSupplierPaymentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.ReferenceNo).MaximumLength(100);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}
