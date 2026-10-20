namespace HMS.Modules.Branding.Application;

/// <summary>
/// The app's built-in theme: a snapshot of the LHS (Lakshmi Hospitals) tenant's saved
/// branding, taken from production on 2026-10-20. Used to seed the singleton row on first
/// read (see BrandingService.GetOrCreateAsync) so the very first GET a client ever makes
/// returns exactly what the frontend already paints while branding is loading (index.css +
/// lib/apply-branding.ts) — a new hospital starts on that theme with no visible swap.
/// Kept in exact sync with frontend/web/src/features/branding/mockBrandingStore.ts's
/// DEFAULT_TOKENS_LIGHT/DARK and DEFAULT_*_SCALE — if one changes, the other should too.
/// </summary>
internal static class BrandingDefaults
{
    public const string HospitalName = "Lakshmi Hospitals";
    public const string AppTitle = "Hospital Management Information System (HMIS)";
    public const string FontFamily = "Inter";
    public const string FontSizeScale = "md";
    public const string IconSizeScale = "lg";

    public static readonly IReadOnlyDictionary<string, string> TokensLight = new Dictionary<string, string>
    {
        ["--background"] = "0 0% 100%",
        ["--foreground"] = "0 3% 14%",
        ["--card"] = "0 0% 100%",
        ["--card-foreground"] = "222 47% 11%",
        ["--card-header-bg"] = "0 0% 100%",
        ["--card-header-foreground"] = "222 47% 11%",
        ["--page-banner-bg"] = "38 100% 50%",
        ["--page-banner-foreground"] = "0 0% 100%",
        ["--popover"] = "0 0% 100%",
        ["--popover-foreground"] = "222 47% 11%",
        ["--primary"] = "38 93% 57%",
        ["--primary-foreground"] = "0 0% 100%",
        ["--secondary"] = "214 31% 87%",
        ["--secondary-foreground"] = "220 3% 17%",
        ["--muted"] = "210 30% 97%",
        ["--muted-foreground"] = "215 16% 47%",
        ["--accent"] = "0 0% 90%",
        ["--accent-foreground"] = "228 7% 59%",
        ["--link"] = "209 69% 65%",
        ["--icon"] = "215 16% 47%",
        ["--destructive"] = "356 100% 47%",
        ["--destructive-foreground"] = "0 0% 100%",
        ["--success"] = "152 100% 38%",
        ["--success-foreground"] = "0 0% 100%",
        ["--warning"] = "0 76% 77%",
        ["--warning-foreground"] = "222 47% 11%",
        ["--info"] = "199 89% 42%",
        ["--info-foreground"] = "0 0% 100%",
        ["--border"] = "0 0% 83%",
        ["--input"] = "214 28% 91%",
        ["--ring"] = "210 82% 45%",
        ["--sidebar"] = "72 100% 99%",
        ["--sidebar-foreground"] = "222 39% 18%",
        ["--sidebar-border"] = "0 14% 67%",
        ["--sidebar-accent"] = "210 40% 95%",
        ["--sidebar-odd-bg"] = "72 100% 99%",
        ["--sidebar-even-bg"] = "72 100% 99%",
        ["--sidebar-active-bg"] = "41 100% 86%",
        ["--sidebar-active-fg"] = "210 82% 40%",
        ["--header-bg"] = "0 0% 44%",
        ["--header-foreground"] = "0 0% 100%",
    };

    public static readonly IReadOnlyDictionary<string, string> TokensDark = new Dictionary<string, string>
    {
        ["--background"] = "222 47% 7%",
        ["--foreground"] = "210 40% 98%",
        ["--card"] = "222 40% 10%",
        ["--card-foreground"] = "210 40% 98%",
        ["--card-header-bg"] = "222 40% 10%",
        ["--card-header-foreground"] = "210 40% 98%",
        ["--page-banner-bg"] = "196 86% 58%",
        ["--page-banner-foreground"] = "0 0% 100%",
        ["--popover"] = "222 40% 10%",
        ["--popover-foreground"] = "210 40% 98%",
        ["--primary"] = "210 82% 45%",
        ["--primary-foreground"] = "0 0% 100%",
        ["--secondary"] = "217 27% 17%",
        ["--secondary-foreground"] = "210 30% 92%",
        ["--muted"] = "217 27% 17%",
        ["--muted-foreground"] = "215 18% 68%",
        ["--accent"] = "210 60% 20%",
        ["--accent-foreground"] = "210 90% 85%",
        ["--link"] = "210 82% 45%",
        ["--icon"] = "215 18% 68%",
        ["--destructive"] = "356 85% 64%",
        ["--destructive-foreground"] = "222 47% 9%",
        ["--success"] = "152 48% 52%",
        ["--success-foreground"] = "222 47% 9%",
        ["--warning"] = "42 96% 62%",
        ["--warning-foreground"] = "222 47% 9%",
        ["--info"] = "199 89% 62%",
        ["--info-foreground"] = "222 47% 9%",
        ["--border"] = "217 24% 20%",
        ["--input"] = "217 24% 20%",
        ["--ring"] = "210 82% 45%",
        ["--sidebar"] = "222 44% 8%",
        ["--sidebar-foreground"] = "210 30% 92%",
        ["--sidebar-border"] = "217 24% 20%",
        ["--sidebar-accent"] = "217 30% 16%",
        ["--sidebar-odd-bg"] = "222 44% 8%",
        ["--sidebar-even-bg"] = "222 40% 10%",
        ["--sidebar-active-bg"] = "210 60% 20%",
        ["--sidebar-active-fg"] = "210 90% 72%",
        ["--header-bg"] = "210 82% 45%",
        ["--header-foreground"] = "0 0% 100%",
    };
}
