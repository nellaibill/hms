namespace HMS.Modules.Platform.Application.Abstractions;

internal interface IPlatformPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}
