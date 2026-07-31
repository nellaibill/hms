namespace HMS.Modules.Identity.Contracts;

public record LoginUserResponse
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public string LoginType { get; init; } = string.Empty;
    public string? ProfilePhotoUrl { get; init; }
}
