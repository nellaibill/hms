namespace HMS.Modules.Identity.Contracts;

public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public LoginUserResponse User { get; init; } = null!;
}
