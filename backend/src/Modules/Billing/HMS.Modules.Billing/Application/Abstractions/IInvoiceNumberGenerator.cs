namespace HMS.Modules.Billing.Application.Abstractions;

/// <summary>
/// Generates the short, human-readable InvoiceNumber business identifier — distinct from
/// the entity's internal <c>Guid.CreateVersion7()</c> primary key. Implemented in
/// Infrastructure via a Postgres sequence, mirroring IPD's IAdmissionIdentifierGenerator.
/// </summary>
internal interface IInvoiceNumberGenerator
{
    Task<string> NextInvoiceNumberAsync(CancellationToken cancellationToken);
}
