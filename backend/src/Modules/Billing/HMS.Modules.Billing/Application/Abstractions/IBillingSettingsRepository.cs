using HMS.Modules.Billing.Domain;

namespace HMS.Modules.Billing.Application.Abstractions;

/// <summary>The hospital's single billing-settings row (ADR-136). Null until it is first saved.</summary>
internal interface IBillingSettingsRepository
{
    Task<BillingSettings?> GetAsync(CancellationToken cancellationToken);

    Task AddAsync(BillingSettings settings, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
