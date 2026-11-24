namespace HMS.Modules.Identity.Contracts;

/// <summary>
/// The features the caller's hospital can use right now (ADR-145) — the same set the login
/// response's FeatureKeys carries, but read live, so a Platform Admin switching a feature off
/// reaches signed-in users without them having to sign in again.
/// </summary>
public record CurrentFeaturesResponse
{
    public IReadOnlyList<string> FeatureKeys { get; init; } = [];
}
