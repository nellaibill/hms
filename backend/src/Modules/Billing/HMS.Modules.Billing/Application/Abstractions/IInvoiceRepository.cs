using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Domain;

namespace HMS.Modules.Billing.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/DeveloperHandbook.md — Application never references EF Core types.
/// </summary>
internal interface IInvoiceRepository
{
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken);

    /// <summary>Always includes Items — an Invoice is never meaningfully read without
    /// its line items (mirrors Patients' IPatientRepository.GetByIdAsync + Registrations).</summary>
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(InvoiceListQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyList<Invoice>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>(invoice CreatedAt, line BillingType, line Total) for every line of every
    /// non-voided invoice since a UTC instant — the Executive Dashboard's revenue charts (DASH-01).</summary>
    Task<IReadOnlyList<(DateTime CreatedAt, BillingType BillingType, decimal Total)>> GetLineTotalsSinceAsync(DateTime fromUtc, CancellationToken cancellationToken);

    /// <summary>The Procedures List's backing query — one row per BillingType.Procedure
    /// invoice line item, joined with its owning Invoice for patient display fields, paged/
    /// filtered per ProcedureListQuery. See ProcedureLineItemRow's own doc comment for why
    /// this returns a plain projection rather than the InvoiceLineItem/Invoice entities.</summary>
    Task<(IReadOnlyList<ProcedureLineItemRow> Items, int TotalCount)> GetProcedureLineItemsPagedAsync(ProcedureListQuery query, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
