using HMS.Shared.Kernel;

namespace HMS.Modules.Identity.Domain;

/// <summary>
/// A person who can be represented in HMS (front-desk staff, clinician, administrator).
/// Per ADR-001 (docs/DecisionLog.md), this originally carried no credential fields; the
/// four credential-related properties below (Username, PasswordHash, EmailVerified,
/// LastLoginAt) are exactly the additive columns ADR-001's "Consequences" section
/// anticipated — no redesign of the existing entity/schema/contracts. Login/JWT/hashing
/// logic itself remains out of scope (docs/Authentication.md is still a stub) — this is
/// storage only.
/// </summary>
internal class User : Entity
{
    public string Username { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    /// <summary>
    /// Never a plaintext password — the hash of one, written only via
    /// <see cref="SetPasswordHash"/>. Null until Authentication ships and a credential is
    /// actually set; never serialized into <c>UserResponse</c>.
    /// </summary>
    public string? PasswordHash { get; private set; }

    public bool EmailVerified { get; private set; }

    public DateTime? LastLoginAt { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Consecutive wrong-password attempts since the last successful login or the last time
    /// a lockout expired — reset to 0 by <see cref="RecordLogin"/>. Never incremented for a
    /// login rejected for any other reason (user not found, inactive account, wrong login
    /// type) — those aren't password-guessing attempts against this specific account.
    /// </summary>
    public int FailedLoginAttempts { get; private set; }

    /// <summary>
    /// Set by <see cref="RecordFailedLogin"/> once <see cref="FailedLoginAttempts"/> reaches
    /// <c>AuthenticationService.MaxFailedLoginAttempts</c>. While in the future, login is
    /// rejected outright (same generic message as every other rejection reason — see
    /// AuthenticationService's own doc comment) without even checking the password.
    /// </summary>
    public DateTime? LockedOutUntil { get; private set; }

    /// <summary>
    /// Relative path under wwwroot (e.g. "uploads/users/{id}.jpg"), written only via
    /// <see cref="SetProfilePhoto"/>. Null until an admin uploads one from the User Details
    /// page — never set at creation time (see UsersController.UploadProfilePhoto).
    /// </summary>
    public string? ProfilePhotoUrl { get; private set; }

    // Required by EF Core materialization.
    private User()
    {
    }

    private User(
        Guid id,
        string username,
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        Guid roleId,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Username = username;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        RoleId = roleId;
        EmailVerified = false;
        IsActive = true;
    }

    public static User Create(
        string username,
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        Guid roleId,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(username, nameof(username));
        Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));
        Guard.AgainstNullOrWhiteSpace(email, nameof(email));

        // Time-ordered UUID per docs/DatabaseArchitecture.md §4 (better index locality than
        // random v4, still coordination-free and non-enumerable).
        return new User(
            Guid.CreateVersion7(),
            NormalizeUsername(username),
            firstName.Trim(),
            lastName.Trim(),
            NormalizeEmail(email),
            phoneNumber,
            roleId,
            createdBy);
    }

    public void UpdateProfile(string firstName, string lastName, string? phoneNumber, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber;
        MarkUpdated(updatedBy);
    }

    public void ChangeUsername(string username, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(username, nameof(username));
        Username = NormalizeUsername(username);
        MarkUpdated(updatedBy);
    }

    public void ChangeEmail(string email, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(email, nameof(email));
        Email = NormalizeEmail(email);

        // A changed email address is unverified until proven again — same reasoning a
        // changed password would invalidate an existing session, once sessions exist.
        EmailVerified = false;
        MarkUpdated(updatedBy);
    }

    public void ChangeRole(Guid roleId, Guid? updatedBy)
    {
        RoleId = roleId;
        MarkUpdated(updatedBy);
    }

    public void SetProfilePhoto(string profilePhotoUrl, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(profilePhotoUrl, nameof(profilePhotoUrl));
        ProfilePhotoUrl = profilePhotoUrl;
        MarkUpdated(updatedBy);
    }

    /// <summary>
    /// Stores a pre-computed hash (never a plaintext password — hashing itself is an
    /// Authentication-iteration concern, docs/Authentication.md). Not called from anywhere
    /// yet; the seam Authentication will use once it ships.
    /// </summary>
    public void SetPasswordHash(string passwordHash, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(passwordHash, nameof(passwordHash));
        PasswordHash = passwordHash;
        MarkUpdated(updatedBy);
    }

    public void VerifyEmail(Guid? updatedBy)
    {
        if (EmailVerified)
        {
            return;
        }

        EmailVerified = true;
        MarkUpdated(updatedBy);
    }

    /// <summary>
    /// Records a successful login. Deliberately does not call <see cref="Entity.MarkUpdated"/>
    /// — UpdatedAt/UpdatedBy represent an admin-style edit to the record, not the subject's
    /// own act of logging in. Clears any lockout — a successful login is proof of a correct
    /// password, so there's no reason to keep the account locked.
    /// </summary>
    public void RecordLogin(DateTime loginAt)
    {
        LastLoginAt = loginAt;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;
    }

    /// <summary>
    /// Records a wrong-password attempt, locking the account out once
    /// <paramref name="maxAttempts"/> is reached. Deliberately does not call
    /// <see cref="Entity.MarkUpdated"/> — same reasoning as <see cref="RecordLogin"/>.
    /// </summary>
    public void RecordFailedLogin(DateTime attemptedAt, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
        {
            LockedOutUntil = attemptedAt.Add(lockoutDuration);
        }
    }

    public bool IsLockedOut(DateTime asOf) => LockedOutUntil.HasValue && LockedOutUntil.Value > asOf;

    public void Activate(Guid? updatedBy)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        MarkUpdated(updatedBy);
    }

    public void Deactivate(Guid? updatedBy)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        MarkUpdated(updatedBy);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();
}
