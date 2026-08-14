using HMS.Modules.Platform.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace HMS.Modules.Platform.Infrastructure;

/// <summary>
/// Wraps ASP.NET Core Identity's own <see cref="PasswordHasher{TUser}"/> (PBKDF2, already
/// available via the Microsoft.AspNetCore.App shared framework) — the same approach
/// HMS.Modules.Identity.Infrastructure.PasswordHasher uses. A separate copy, not a shared
/// reference: Identity's implementation is internal to its own assembly by design (every
/// module in this codebase is a fully isolated vertical slice), so Platform gets its own
/// ~10-line wrapper around the same underlying library rather than reaching into Identity's
/// internals or weakening that boundary.
/// </summary>
internal sealed class PlatformPasswordHasher : IPlatformPasswordHasher
{
    private readonly PasswordHasher<object> _inner = new();

    public string HashPassword(string password) => _inner.HashPassword(default!, password);

    public bool VerifyPassword(string password, string passwordHash) =>
        _inner.VerifyHashedPassword(default!, passwordHash, password) != PasswordVerificationResult.Failed;
}
