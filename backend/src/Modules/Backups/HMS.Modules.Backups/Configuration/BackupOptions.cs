namespace HMS.Modules.Backups.Configuration;

/// <summary>
/// Bound from the "Backups" configuration section. <see cref="PgDumpExecutablePath"/> is a
/// real deployment prerequisite this module cannot verify on its own — it must point at a
/// pg_dump matching the target Postgres server's major version, installed on whatever host
/// actually runs the API process; there is no repo-wide evidence pg_dump is present there
/// today, so this needs to be confirmed (or installed) as part of standing this feature up.
/// </summary>
public sealed class BackupOptions
{
    public const string SectionName = "Backups";

    /// <summary>Full path to pg_dump(.exe). Defaults to "pg_dump", i.e. "resolve it from PATH" —
    /// override with an absolute path if it isn't on PATH for the service account running the API.</summary>
    public string PgDumpExecutablePath { get; set; } = "pg_dump";

    /// <summary>Root directory backups are written under — one "yyyy-MM-dd" subfolder per day,
    /// one "&lt;key&gt;.dump" file per masters/tenant inside it. Must be writable by the API's
    /// service account and, ideally, on a different physical disk/volume than the database
    /// itself (a backup that lives only on the same disk as what it backs up protects against
    /// far fewer failure modes).</summary>
    public string BackupRootDirectory { get; set; } = string.Empty;

    /// <summary>How many days of dated folders the daily job keeps before deleting older ones.</summary>
    public int RetentionDays { get; set; } = 14;
}
