namespace HMS.Modules.Identity.Contracts;

public record UpdateUserRequest
{
    public string Username { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public Guid RoleId { get; init; }

    /// <summary>Optional link to a Masters.Consultant record, independent of RoleId. Null
    /// clears an existing link.</summary>
    public Guid? ConsultantId { get; init; }
}
