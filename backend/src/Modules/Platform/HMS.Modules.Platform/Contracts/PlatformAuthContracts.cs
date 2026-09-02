namespace HMS.Modules.Platform.Contracts;

public record PlatformLoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Either a completed login (MfaRequired false, Token/User populated) or the first half of
/// a two-step MFA login (MfaRequired true, MfaChallengeToken populated, Token/User null) —
/// see PlatformMfaChallenge's own doc comment for the full flow. Returned by both
/// LoginAsync (which decides which shape applies) and VerifyMfaAsync (always the completed
/// shape, since a second MFA challenge is never issued for an already-passed MFA step).
/// </summary>
public record PlatformLoginResponse
{
    public bool MfaRequired { get; init; }
    public string? MfaChallengeToken { get; init; }
    public string? Token { get; init; }
    public int ExpiresIn { get; init; }
    public PlatformLoginUserResponse? User { get; init; }
}

public record PlatformLoginUserResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

/// <summary>Second step of a two-step MFA login — exchanges the challenge token LoginAsync
/// issued, plus a current TOTP code, for the real bearer token.</summary>
public record PlatformMfaVerifyRequest
{
    public string ChallengeToken { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

/// <summary>Starts (or restarts) MFA setup for the caller's own account — generates a new
/// secret and returns it once for the admin to add to their authenticator app.
/// MfaEnabled stays false until <see cref="PlatformMfaEnableRequest"/> confirms a code.</summary>
public record PlatformMfaSetupResponse
{
    public string Secret { get; init; } = string.Empty;
    public string OtpAuthUri { get; init; } = string.Empty;
}

/// <summary>Confirms a pending setup by proving the admin's authenticator app produces a
/// valid code — the only way MfaEnabled turns true.</summary>
public record PlatformMfaEnableRequest
{
    public string Code { get; init; } = string.Empty;
}

/// <summary>Turns MFA back off. Requires a valid current code (not just an authenticated
/// session) — same reasoning as requiring the current password to change a password.</summary>
public record PlatformMfaDisableRequest
{
    public string Code { get; init; } = string.Empty;
}

/// <summary>Whether the caller's own account currently has MFA enabled — lets the frontend
/// render "Set up MFA" vs "Disable MFA" without guessing from stale JWT claims.</summary>
public record PlatformMfaStatusResponse
{
    public bool Enabled { get; init; }
}

/// <summary>Self-service password change for a Platform Admin's own account — mirrors
/// HMS.Modules.Identity.Contracts.ChangePasswordRequest. There is no admin-resets-another-
/// admin equivalent yet (see PlatformUser.ChangePassword's own doc comment).</summary>
public record PlatformChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
