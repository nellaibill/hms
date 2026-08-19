using HMS.Modules.Platform.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace HMS.Modules.Platform.Infrastructure;

/// <summary>
/// Wraps ASP.NET Core's built-in Data Protection API (already part of the
/// Microsoft.AspNetCore.App shared framework — no new infrastructure or dependency) with a
/// purpose string scoped to this one use, so a key compromise elsewhere in the app can't be
/// reused to decrypt MFA secrets and vice versa.
/// </summary>
internal sealed class PlatformMfaSecretProtector : IPlatformMfaSecretProtector
{
    private const string Purpose = "HMS.Platform.MfaSecret.v1";

    private readonly IDataProtector _protector;

    public PlatformMfaSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string secret) => _protector.Protect(secret);

    public string Unprotect(string protectedSecret) => _protector.Unprotect(protectedSecret);
}
