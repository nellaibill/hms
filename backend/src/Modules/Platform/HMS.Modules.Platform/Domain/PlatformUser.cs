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
}
