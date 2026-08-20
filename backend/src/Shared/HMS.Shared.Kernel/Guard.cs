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

    /// <summary>Added for HMS.Modules.Pharmacy's quantity invariants (Receive/Dispense must
    /// move a strictly positive amount) — kept here rather than duplicated per-module since
    /// any future module with the same "quantity must be positive" invariant can reuse it.</summary>
    public static void AgainstNonPositive(decimal value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} must be greater than zero.");
        }
    }
}
