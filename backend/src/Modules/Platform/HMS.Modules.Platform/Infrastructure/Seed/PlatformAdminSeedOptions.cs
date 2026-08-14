namespace HMS.Modules.Platform.Infrastructure.Seed;

/// <summary>
/// Bound from the "PlatformAdminSeed" configuration section (appsettings.json /
/// appsettings.Development.json) — the same configuration-driven pattern
/// HMS.Modules.Identity's SuperAdminSeedOptions already uses. Used only to create the
/// initial Platform Admin account; never read again afterwards.
/// </summary>
internal sealed class PlatformAdminSeedOptions
{
    public const string SectionName = "PlatformAdminSeed";

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Plaintext, read once at startup and immediately discarded — only
    /// <see cref="PlatformDataSeeder"/> touches this property, and only to pass it straight
    /// into <c>IPlatformPasswordHasher.HashPassword</c>. Never persisted as-is.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
