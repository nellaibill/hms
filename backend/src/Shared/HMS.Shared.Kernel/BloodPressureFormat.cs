using System.Globalization;
using System.Text.RegularExpressions;

namespace HMS.Shared.Kernel;

/// <summary>
/// The one rule for a blood pressure typed as free text ("120/80") — OPD consultation vitals and
/// the discharge summary's examination vitals both store BP as a string, and until this rule both
/// accepted anything up to 20 characters ("abc" included). Ranges match IPD's numeric vitals
/// (VitalsReadingValidators: systolic 40–300, diastolic 20–200), and systolic must be higher than
/// diastolic so a transposed reading ("80/120") is caught. A trailing "mmHg" is tolerated, since
/// that's how many clinicians write it. Kernel has no FluentValidation dependency, so this is a
/// plain check the module validators call via Must(...).
/// </summary>
public static partial class BloodPressureFormat
{
    public const int MinSystolic = 40;
    public const int MaxSystolic = 300;
    public const int MinDiastolic = 20;
    public const int MaxDiastolic = 200;

    public const string Message =
        "Blood pressure must be systolic/diastolic, e.g. 120/80 (systolic 40–300, diastolic 20–200, systolic higher than diastolic).";

    [GeneratedRegex(@"^\s*(\d{2,3})\s*/\s*(\d{2,3})\s*(mm\s*hg)?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex ReadingRegex();

    /// <summary>True for a blank value (BP is optional everywhere it's typed) or a valid reading.</summary>
    public static bool IsValidOrEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var match = ReadingRegex().Match(value);
        if (!match.Success)
        {
            return false;
        }

        var systolic = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var diastolic = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        return systolic is >= MinSystolic and <= MaxSystolic
            && diastolic is >= MinDiastolic and <= MaxDiastolic
            && systolic > diastolic;
    }
}
