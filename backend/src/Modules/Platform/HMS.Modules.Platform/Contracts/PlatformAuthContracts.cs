namespace HMS.Modules.Platform.Contracts;

public record PlatformLoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record PlatformLoginResponse
{
    public string Token { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public PlatformLoginUserResponse User { get; init; } = null!;
}

public record PlatformLoginUserResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
