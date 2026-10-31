namespace HMS.Modules.Identity.Contracts;

public record CreateUserRequest
{
    public string Username { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public Guid RoleId { get; init; }

    /// <summary>Optional link to a Masters.Consultant record, independent of RoleId.</summary>
    public Guid? ConsultantId { get; init; }

    /// <summary>
    /// Optional first password, so an account can be created and made usable in one step
    /// (the Employee/Consultant forms' Login access card — ADR-108). Stored exactly like an
    /// admin SetPassword: hashed, with MustChangePassword set so the person picks their own
    /// at first login. Omitted, the account has no password until SetPassword is called.
    /// </summary>
    public string? Password { get; init; }
}
