using HMS.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace HMS.Modules.Identity.Infrastructure;

/// <summary>
/// Wraps ASP.NET Core Identity's own <see cref="PasswordHasher{TUser}"/> (PBKDF2, already
/// available via the Microsoft.AspNetCore.App shared framework — no extra package needed)
/// rather than a hand-rolled algorithm, per "reuse existing infrastructure wherever
/// possible."
/// </summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    // TUser is only part of PasswordHasher<TUser>'s generic signature — the hash/verify
    // algorithm itself never inspects the instance — so a plain object avoids coupling
    // this purely cryptographic concern to the domain User type.
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _inner = new();

    public string HashPassword(string password) => _inner.HashPassword(default!, password);

    public bool VerifyPassword(string password, string passwordHash) =>
        _inner.VerifyHashedPassword(default!, passwordHash, password) != PasswordVerificationResult.Failed;
}
