using HMS.Shared.Kernel;

namespace HMS.Modules.Identity.Domain;

/// <summary>
/// A person who can be represented in HMS (front-desk staff, clinician, administrator).
/// Deliberately carries no credential/authentication fields in this iteration —
/// see docs/modules/Identity/Users.md and docs/DecisionLog.md.
/// </summary>
internal class User : Entity
{
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; }

    // Required by EF Core materialization.
    private User()
    {
    }

    private User(Guid id, string firstName, string lastName, string email, string? phoneNumber, Guid? createdBy)
        : base(id, createdBy)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        IsActive = true;
    }

    public static User Create(string firstName, string lastName, string email, string? phoneNumber, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));
        Guard.AgainstNullOrWhiteSpace(email, nameof(email));

        // Time-ordered UUID per docs/DatabaseArchitecture.md §4 (better index locality than
        // random v4, still coordination-free and non-enumerable).
        return new User(
            Guid.CreateVersion7(),
            firstName.Trim(),
            lastName.Trim(),
            NormalizeEmail(email),
            phoneNumber,
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

    public void ChangeEmail(string email, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(email, nameof(email));
        Email = NormalizeEmail(email);
        MarkUpdated(updatedBy);
    }

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
}
