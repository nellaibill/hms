namespace HMS.Modules.Backups.Contracts;

/// <summary>One row in a backup list — either "masters" or one tenant. <see cref="IsAvailable"/>
/// distinguishes "never backed up yet" from a zero-length/missing file, since the latter never
/// occurs (GetLatestAsync only returns a result for a file that actually exists).</summary>
public record BackupSummaryResponse
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public DateOnly? LastBackupDate { get; init; }
    public long? SizeBytes { get; init; }
    public bool IsAvailable { get; init; }
}
