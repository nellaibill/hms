namespace HMS.Modules.Laboratory.Application.Abstractions;

/// <summary>
/// Generates the short, human-readable LabOrderNumber business identifier (e.g.
/// "LAB-2026-000001") — distinct from the entity's internal Guid.CreateVersion7() primary
/// key. Implemented in Infrastructure via its own Postgres sequence, mirroring Billing's
/// IInvoiceNumberGenerator.
/// </summary>
internal interface ILabOrderNumberGenerator
{
    Task<string> NextLabOrderNumberAsync(CancellationToken cancellationToken);
}
