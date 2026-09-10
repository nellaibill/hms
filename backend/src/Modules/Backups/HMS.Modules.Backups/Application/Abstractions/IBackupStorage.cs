namespace HMS.Modules.Backups.Application.Abstractions;

/// <summary>
/// Where backup files live and how they're found — a plain "&lt;root&gt;/yyyy-MM-dd/&lt;key&gt;.dump"
/// directory layout, deliberately with no database of its own recording backup metadata (see
/// BackupsModule's own doc comment for why). <c>key</c> is <c>"masters"</c> for the Platform
/// database or a tenant's <c>Guid</c> (as a string) for a hospital database.
/// </summary>
public interface IBackupStorage
{
    /// <summary>The path a backup for <paramref name="key"/> on <paramref name="date"/> should
    /// be written to. Does not create the file or its parent directory — the caller (the
    /// pg_dump runner) does that as part of writing to it.</summary>
    string ResolveOutputPath(string key, DateOnly date);

    /// <summary>The most recent successfully-written backup for <paramref name="key"/>, if any —
    /// searches date folders newest-first and returns the first one that actually contains this
    /// key's file (a day where only some databases dumped successfully still lets every other
    /// key fall back to its own most recent success, rather than "the newest folder or nothing").</summary>
    Task<BackupFileInfo?> GetLatestAsync(string key, CancellationToken cancellationToken);

    /// <summary>Deletes entire date folders older than the configured retention window.</summary>
    Task CleanupOldBackupsAsync(CancellationToken cancellationToken);
}

public sealed record BackupFileInfo(string Key, DateOnly Date, string FilePath, long SizeBytes);
