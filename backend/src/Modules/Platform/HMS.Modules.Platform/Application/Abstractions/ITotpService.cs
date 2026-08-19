namespace HMS.Modules.Platform.Application.Abstractions;

/// <summary>
/// RFC 6238 (TOTP) — the Platform Admin MFA second factor. Defined here (Application) and
/// implemented in Infrastructure (wrapping the Otp.NET library), per the dependency
/// inversion rule — Application never references a third-party crypto library directly.
/// </summary>
internal interface ITotpService
{
    /// <summary>Generates a new random base32-encoded shared secret.</summary>
    string GenerateSecret();

    /// <summary>Builds the otpauth:// URI an authenticator app scans/imports — encodes the
    /// secret, account label, and issuer, but never leaves this process (rendered once on
    /// the setup screen, never persisted).</summary>
    string BuildOtpAuthUri(string secret, string accountEmail);

    /// <summary>True if <paramref name="code"/> is a currently-valid 6-digit TOTP code for
    /// <paramref name="secret"/> (small clock-skew window tolerated internally).</summary>
    bool VerifyCode(string secret, string code);
}
