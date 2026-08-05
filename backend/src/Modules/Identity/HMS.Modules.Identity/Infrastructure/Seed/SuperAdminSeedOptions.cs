namespace HMS.Modules.Identity.Infrastructure.Seed;

/// <summary>
/// Bound from the "SuperAdminSeed" configuration section (appsettings.json /
/// appsettings.Development.json), the same configuration-driven pattern
/// JwtConfiguration already uses for Jwt:SigningKey — no credential is hard-coded in
/// source. <see cref="IdentityDataSeeder"/> uses this only to create the initial Super
/// Admin user; it is never read again afterwards.
/// </summary>
internal sealed class SuperAdminSeedOptions
{
    public const string SectionName = "SuperAdminSeed";

    public string Username { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Plaintext, read once at startup and immediately discarded — only
    /// <see cref="IdentityDataSeeder"/> touches this property, and only to pass it
    /// straight into <c>IPasswordHasher.HashPassword</c>. Never persisted as-is.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
