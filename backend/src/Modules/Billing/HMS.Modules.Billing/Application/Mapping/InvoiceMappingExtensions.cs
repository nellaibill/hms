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
        BilledConsultantId = item.BilledConsultantId,
        ServiceId = item.ServiceId,
        PackageId = item.PackageId,
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
        IsReverted = invoice.IsReverted,
        DiscountStatus = invoice.DiscountStatus,
    };

    public static InvoiceDiscountRequestResponse ToResponse(this InvoiceDiscountRequest request) => new()
    {
        Id = request.Id,
        InvoiceId = request.InvoiceId,
        InvoiceNumber = request.InvoiceNumber,
        PatientId = request.PatientId,
        PatientName = request.PatientName,
        PatientUhid = request.PatientUhid,
        GrossAmount = request.GrossAmount,
        DiscountType = request.DiscountType,
        DiscountValue = request.DiscountValue,
        TotalDiscount = request.TotalDiscount,
        Reason = request.Reason,
        Status = request.Status,
        RequestedBy = request.CreatedBy,
        RequestedByName = request.RequestedByName,
        RequestedAt = request.CreatedAt,
        DecidedBy = request.DecidedBy,
        DecidedByName = request.DecidedByName,
        DecidedAt = request.DecidedAt,
        DecisionNotes = request.DecisionNotes,
        Lines = request.Lines.Select(l => new InvoiceDiscountRequestLineResponse
        {
            LineItemId = l.LineItemId,
            BillingType = l.BillingType,
            DepartmentId = l.DepartmentId,
            ConsultantId = l.ConsultantId,
            ServiceId = l.ServiceId,
            PackageId = l.PackageId,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            LineAmount = l.Quantity * l.UnitPrice,
            Discount = l.Discount,
        }).ToList(),
    };
}
