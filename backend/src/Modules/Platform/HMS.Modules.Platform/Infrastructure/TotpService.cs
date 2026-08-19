using HMS.Modules.Platform.Application.Abstractions;
using OtpNet;

namespace HMS.Modules.Platform.Infrastructure;

/// <summary>
/// Thin wrapper around Otp.NET — RFC 6238 TOTP generation/verification for Platform Admin
/// MFA, rather than hand-rolling HMAC-based one-time-code math for a security primitive.
/// </summary>
internal sealed class TotpService : ITotpService
{
    private const string Issuer = "HMS Platform Portal";

    // Tolerates the code from one step before/after "now" (±30s) — accounts for ordinary
    // clock drift between the server and the admin's phone without meaningfully widening
    // the brute-force window (still only 3 valid 6-digit codes at any instant).
    private static readonly VerificationWindow ClockSkewWindow = new(previous: 1, future: 1);

    public string GenerateSecret()
    {
        // 160 bits — the length RFC 4226 §4 recommends for HMAC-SHA1-based one-time codes.
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string BuildOtpAuthUri(string secret, string accountEmail)
    {
        var label = Uri.EscapeDataString($"{Issuer}:{accountEmail}");
        var issuer = Uri.EscapeDataString(Issuer);
        return $"otpauth://totp/{label}?secret={secret}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";
    }

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code, out _, ClockSkewWindow);
    }
}
