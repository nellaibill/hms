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

    // Required by EF Core materialization.
    private PlatformUser()
    {
    }

    private PlatformUser(Guid id, string fullName, string email, string passwordHash, Guid? createdBy)
        : base(id, createdBy)
    {
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
    }

    public static PlatformUser Create(string fullName, string email, string passwordHash, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(fullName, nameof(fullName));
        Guard.AgainstNullOrWhiteSpace(email, nameof(email));
        Guard.AgainstNullOrWhiteSpace(passwordHash, nameof(passwordHash));

        return new PlatformUser(
            Guid.CreateVersion7(),
            fullName.Trim(),
            email.Trim().ToLowerInvariant(),
            passwordHash,
            createdBy);
    }
}
