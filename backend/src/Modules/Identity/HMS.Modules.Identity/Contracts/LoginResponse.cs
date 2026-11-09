namespace HMS.Modules.Identity.Contracts;

public record LoginResponse
{
    /// <summary>Has no expiry — valid until the user signs out (ADR-131).</summary>
    public string Token { get; init; } = string.Empty;
    public LoginUserResponse User { get; init; } = null!;
}
