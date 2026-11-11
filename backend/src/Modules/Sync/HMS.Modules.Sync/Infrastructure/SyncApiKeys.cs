using System.Security.Cryptography;
using System.Text;

namespace HMS.Modules.Sync.Infrastructure;

/// <summary>Sync API keys: <c>hmssync_</c> + 32 random bytes (base64url). Only the SHA-256 hash
/// is stored; a 256-bit random key needs no salt or slow hash, and lookup by hash is a plain
/// indexed equality, so verification takes the same time whatever the key.</summary>
internal static class SyncApiKeys
{
    public const string Prefix = "hmssync_";
    private const int RandomBytes = 32;
    private const int DisplayPrefixLength = 14;

    // 32 bytes → 43 base64url characters (no padding).
    private static readonly int KeyLength = Prefix.Length + 43;

    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(RandomBytes);
        return Prefix + Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string Hash(string key) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    public static string DisplayPrefix(string key) => key[..Math.Min(DisplayPrefixLength, key.Length)];

    /// <summary>Cheap shape check before any database lookup.</summary>
    public static bool IsWellFormed(string? key) =>
        key is not null
        && key.Length == KeyLength
        && key.StartsWith(Prefix, StringComparison.Ordinal)
        && key.AsSpan(Prefix.Length).IndexOfAnyExcept("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_") < 0;
}
