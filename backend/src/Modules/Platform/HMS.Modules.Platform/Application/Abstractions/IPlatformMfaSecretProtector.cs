namespace HMS.Modules.Platform.Application.Abstractions;

/// <summary>
/// Encrypts/decrypts a Platform Admin's TOTP shared secret for storage in
/// PlatformUser.MfaSecret — the secret is as sensitive as a password (anyone who reads it
/// can generate valid codes forever), so it is never stored in plaintext. Defined here and
/// implemented in Infrastructure (wrapping ASP.NET Core's built-in Data Protection API), per
/// the dependency inversion rule.
/// </summary>
internal interface IPlatformMfaSecretProtector
{
    string Protect(string secret);

    string Unprotect(string protectedSecret);
}
