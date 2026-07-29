using HMS.Modules.Branding.Domain;

namespace HMS.Modules.Branding.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule in docs/Architecture.md — Application never references EF Core types.
/// </summary>
internal interface IBrandingRepository
{
    /// <summary>Returns the singleton row, or null if it hasn't been created yet (fresh DB, seed skipped).</summary>
    Task<BrandingSettings?> GetAsync(CancellationToken cancellationToken);

    Task AddAsync(BrandingSettings settings, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
