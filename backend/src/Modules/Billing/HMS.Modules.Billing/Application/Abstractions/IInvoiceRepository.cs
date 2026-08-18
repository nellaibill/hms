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

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
