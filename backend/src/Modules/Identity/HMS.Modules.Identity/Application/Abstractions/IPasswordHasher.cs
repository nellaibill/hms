namespace HMS.Modules.Identity.Application.Abstractions;

/// <summary>
/// Defined here (Application) and implemented in Infrastructure, per the dependency
/// inversion rule — same split as <see cref="IUserFileStorage"/>. Synchronous, unlike the
/// repository abstractions: hashing/verifying is pure CPU-bound work with no I/O, so
/// wrapping it in a Task would add nothing.
/// </summary>
internal interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}
