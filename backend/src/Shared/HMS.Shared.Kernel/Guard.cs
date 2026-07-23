namespace HMS.Shared.Kernel;

/// <summary>
/// Small guard-clause helpers used by domain entities/value objects to protect
/// their own invariants at construction/mutation time.
/// </summary>
public static class Guard
{
    public static void AgainstNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} must not be null or empty.", paramName);
        }
    }
}
