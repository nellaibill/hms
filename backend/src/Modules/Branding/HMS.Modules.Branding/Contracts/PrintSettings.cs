namespace HMS.Modules.Branding.Contracts;

/// <summary>The page every printout gets unless its document type overrides it: paper size
/// (one of <see cref="PrintPaperSizes"/>), orientation (<see cref="PrintOrientations"/>) and the
/// page margin in millimetres. Applied by the frontend as a CSS <c>@page</c> rule, which is what
/// the browser's print dialog then picks up as its paper size and layout.</summary>
public record PrintPageSetup
{
    public string PaperSize { get; init; } = PrintPaperSizes.A4;
    public string Orientation { get; init; } = PrintOrientations.Portrait;
    public int MarginMm { get; init; } = 12;
}

/// <summary>A per-document override of <see cref="PrintSettings.Default"/>. A null field means
/// "same as the default", so changing the default still moves every document that never set it.</summary>
public record PrintDocumentSetup
{
    public string? PaperSize { get; init; }
    public string? Orientation { get; init; }
}

/// <summary>
/// Hospital-wide print configuration: one default page setup plus optional overrides keyed by
/// <see cref="PrintDocumentTypes"/>. Stored as one jsonb column, same reasoning as
/// <see cref="LogoDisplaySettings"/>. The pharmacy's own bill format (Pharmacy settings) is not
/// part of this — a counter bill keeps printing on the pharmacy's A5/A4/thermal choice.
/// </summary>
public record PrintSettings
{
    public PrintPageSetup Default { get; init; } = new();
    public Dictionary<string, PrintDocumentSetup> Documents { get; init; } = new();
}

/// <summary>Paper sizes offered, as CSS <c>@page size</c> keywords. Kept in sync with
/// frontend/web/src/features/branding/types.ts's PAPER_SIZES.</summary>
public static class PrintPaperSizes
{
    public const string A3 = "A3";
    public const string A4 = "A4";
    public const string A5 = "A5";
    public const string Letter = "Letter";
    public const string Legal = "Legal";

    public static readonly IReadOnlyList<string> All = [A4, A3, A5, Letter, Legal];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}

public static class PrintOrientations
{
    public const string Portrait = "portrait";
    public const string Landscape = "landscape";

    public static readonly IReadOnlyList<string> All = [Portrait, Landscape];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}

/// <summary>The printouts that can override the default page. Kept in sync with
/// frontend/web/src/features/branding/types.ts's PRINT_DOCUMENTS.</summary>
public static class PrintDocumentTypes
{
    public const string Patient = "patient";
    public const string Invoice = "invoice";
    public const string LabReport = "labReport";
    public const string OpdConsultation = "opdConsultation";
    public const string Report = "report";

    public static readonly IReadOnlyList<string> All = [Patient, Invoice, LabReport, OpdConsultation, Report];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
