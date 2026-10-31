namespace HMS.Modules.HR.Application;

/// <summary>
/// Turns an employee's free-text Phone into the bare 10-digit mobile number Identity requires
/// for a login (CreateUserRequestValidator) — spaces, dashes and a +91/0 prefix are dropped.
/// Null when what's left isn't a 10-digit number.
/// </summary>
internal static class MobileNumber
{
    public static string? ToTenDigits(string? phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

        if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }
        else if (digits.Length == 11 && digits.StartsWith('0'))
        {
            digits = digits[1..];
        }

        return digits.Length == 10 ? digits : null;
    }
}
