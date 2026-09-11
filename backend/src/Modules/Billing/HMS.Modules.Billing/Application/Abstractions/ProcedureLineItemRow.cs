using HMS.Modules.Billing.Contracts;

namespace HMS.Modules.Billing.Application.Abstractions;

/// <summary>
/// One joined InvoiceLineItem+Invoice row, as returned by
/// IInvoiceRepository.GetProcedureLineItemsPagedAsync — the repository's own plain-scalar
/// projection (not the domain entities themselves, since InvoiceLineItem carries no navigation
/// back to its owning Invoice) that InvoiceService maps 1:1 into the public ProcedureListItem.
/// Mirrors HMS.Modules.Patients' OpdPatientListRow.
/// </summary>
internal sealed record ProcedureLineItemRow(
    Guid InvoiceId,
    Guid InvoiceLineItemId,
    Guid PatientId,
    string PatientName,
    string PatientUhid,
    string? ServiceId,
    string? ConsultantId,
    string? DepartmentId,
    DateTime CreatedAt,
    PaymentStatus PaymentStatus,
    decimal Total);
