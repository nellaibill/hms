using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;

namespace HMS.Modules.Billing.Application.Mapping;

internal static class InvoiceMappingExtensions
{
    public static InvoiceLineItemResponse ToResponse(this InvoiceLineItem item) => new()
    {
        Id = item.Id,
        BillingType = item.BillingType,
        DepartmentId = item.DepartmentId,
        ConsultantId = item.ConsultantId,
        ServiceId = item.ServiceId,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice,
        Discount = item.Discount,
        DiscountApproved = item.DiscountApproved,
        DiscountApprovedBy = item.DiscountApprovedBy,
        PaymentStatus = item.PaymentStatus,
        Total = item.Total,
    };

    public static InvoiceResponse ToResponse(this Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        PatientId = invoice.PatientId,
        VisitId = invoice.VisitId,
        PatientName = invoice.PatientName,
        PatientUhid = invoice.PatientUhid,
        CreatedAt = invoice.CreatedAt,
        Items = invoice.Items.Select(i => i.ToResponse()).ToList(),
        GrossAmount = invoice.GrossAmount,
        TotalDiscount = invoice.TotalDiscount,
        NetAmount = invoice.NetAmount,
        PaymentStatus = invoice.PaymentStatus,
        IsVoided = invoice.IsVoided,
        VoidedAt = invoice.VoidedAt,
        VoidReason = invoice.VoidReason,
    };
}
