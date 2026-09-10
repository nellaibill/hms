namespace HMS.Modules.Patients.Application.Excel;

/// <summary>
/// Neutralizes CSV/Excel formula-injection payloads (OWASP) before writing untrusted text into
/// a ClosedXML cell — a value beginning with =, +, -, or @ can be interpreted as a formula by
/// some spreadsheet applications/configurations when the file is later opened, even though
/// ClosedXML itself writes a plain string <c>.Value</c> assignment as a text-typed cell, not a
/// formula. Prefixing with a leading apostrophe is the standard mitigation regardless of the
/// underlying cell type — it's what Excel's own UI does when a user types a leading-apostrophe
/// value to force literal text, and costs nothing for values that were never at risk.
/// </summary>
internal static class ExcelCellSafety
{
    private static readonly char[] FormulaTriggers = ['=', '+', '-', '@', '\t', '\r'];

    /// <summary>Returns <paramref name="value"/> unchanged unless its first character could be
    /// interpreted as a formula trigger, in which case a leading apostrophe is prepended.</summary>
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        return FormulaTriggers.Contains(value[0]) ? "'" + value : value;
    }
}
