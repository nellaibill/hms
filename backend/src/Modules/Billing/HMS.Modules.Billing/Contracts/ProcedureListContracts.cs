namespace HMS.Modules.Billing.Contracts;

/// <summary>
/// One BillingType.Procedure invoice line item — the Procedures List reads straight off
/// Billing's existing invoice line items rather than a dedicated procedure-order entity: there
/// is nothing else in the codebase to base a "procedure order" concept on (unlike Laboratory's
/// LabOrder), so PaymentStatus doubles as this line's status column. ConsultantId/DepartmentId
/// are typed as free-text (not Guid FKs) — see Domain/InvoiceLineItem.cs's own doc comment —
/// and ConsultantId here is deliberately sourced from InvoiceLineItem.BilledConsultantId, not
/// ConsultantId itself, since the latter is cleared once a line is paid (ADR-048) while
/// BilledConsultantId survives for reporting. In practice ServiceId/ConsultantId/DepartmentId
/// hold Masters Guids (ServiceBillingCard.tsx's Procedure Billing form picks a real
/// DiagnosticService/Consultant), so InvoiceService.GetProcedureLineItemsAsync additionally
/// resolves each into its own *Name field below — mirrors every other list item in this
/// codebase that denormalizes a display name alongside its id (e.g. OpdPatientListItem's
/// departmentId/departmentName) rather than replacing the id with the name.
/// </summary>
public record ProcedureListItem
{
    public Guid InvoiceId { get; init; }
    public Guid InvoiceLineItemId { get; init; }
    public Guid PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string PatientUhid { get; init; } = string.Empty;
    public string? ServiceId { get; init; }
    public string? ServiceName { get; init; }
    public string? ConsultantId { get; init; }
    public string? ConsultantName { get; init; }
    public string? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public DateTime CreatedAt { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public decimal Total { get; init; }
}

/// <summary>DepartmentId/ConsultantId stay string (not Guid) to match InvoiceLineItem's own
/// free-text fields — see ProcedureListItem's doc comment.</summary>
public record ProcedureListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? DepartmentId { get; init; }
    public string? ConsultantId { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
}
