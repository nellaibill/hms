using HMS.Modules.Platform.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Platform.Domain;

/// <summary>
/// A Support User (Platform Admin) who can sign into the Platform Portal to register new
/// hospitals. Entirely separate from HMS.Modules.Identity.Domain.User — platform admins and
/// hospital users are never the same session, the same table, or the same database.
/// </summary>
internal class PlatformUser : Entity
{
    public string FullName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public PlatformRole Role { get; private set; }

    /// <summary>Consecutive wrong-password attempts since the last successful login or the
    /// last time a lockout expired — see HMS.Modules.Identity.Domain.User's identical
    /// fields, the same brute-force throttling shape on the hospital-user side.</summary>
    public int FailedLoginAttempts { get; private set; }

    public DateTime? LockedOutUntil { get; private set; }

    /// <summary>
    /// The TOTP shared secret (RFC 6238), encrypted at rest via
    /// IPlatformMfaSecretProtector — never stored or returned in plaintext once set. Set by
    /// <see cref="SetPendingMfaSecret"/> during setup, before <see cref="MfaEnabled"/> turns
    /// true; cleared by <see cref="DisableMfa"/>.
    /// </summary>
    public string? MfaSecret { get; private set; }

    /// <summary>
    /// True once the Platform Admin has confirmed a setup code against
    /// <see cref="MfaSecret"/> — see <see cref="EnableMfa"/>. A non-null
    /// <see cref="MfaSecret"/> with this still false means setup was started but never
    /// confirmed; PlatformAuthenticationService.LoginAsync only requires the MFA step when
    /// this is true.
    /// </summary>
    public bool MfaEnabled { get; private set; }

    // Required by EF Core materialization.
    private PlatformUser()
    {
    }

    private PlatformUser(Guid id, string fullName, string email, string passwordHash, PlatformRole role, Guid? createdBy)
        : base(id, createdBy)
    {
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public static PlatformUser Create(string fullName, string email, string passwordHash, PlatformRole role, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(fullName, nameof(fullName));
        Guard.AgainstNullOrWhiteSpace(email, nameof(email));
        Guard.AgainstNullOrWhiteSpace(passwordHash, nameof(passwordHash));

        return new PlatformUser(
            Guid.CreateVersion7(),
            fullName.Trim(),
            email.Trim().ToLowerInvariant(),
            passwordHash,
            role,
            createdBy);
    }

    public void RecordSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockedOutUntil = null;
    }

    public void RecordFailedLogin(DateTime attemptedAt, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
        {
            LockedOutUntil = attemptedAt.Add(lockoutDuration);
        }
    }

    public bool IsLockedOut(DateTime asOf) => LockedOutUntil.HasValue && LockedOutUntil.Value > asOf;

    /// <summary>
    /// Stores a freshly-generated, not-yet-confirmed MFA secret (already encrypted by the
    /// caller). Overwrites any prior pending or active secret — starting setup again always
    /// wins, matching SetPasswordHash's "the most recent write is authoritative" convention.
    /// Does not itself enable MFA; <see cref="EnableMfa"/> does that once the setup code is
    /// verified.
    /// </summary>
    public void SetPendingMfaSecret(string encryptedSecret)
    {
        Guard.AgainstNullOrWhiteSpace(encryptedSecret, nameof(encryptedSecret));
        MfaSecret = encryptedSecret;
        MfaEnabled = false;
    }

    public void EnableMfa()
    {
        if (MfaSecret is null)
        {
            throw new InvalidOperationException("Cannot enable MFA before SetPendingMfaSecret has stored a secret.");
        }

        MfaEnabled = true;
    }

    public void DisableMfa()
    {
        MfaSecret = null;
        MfaEnabled = false;
    }
}
