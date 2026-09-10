/** Mirrors HMS.Modules.Backups.Contracts.BackupSummaryResponse. */
export interface BackupSummaryResponse {
  key: string;
  label: string;
  /** ISO date string (yyyy-MM-dd), or null when no backup has run for this key yet. */
  lastBackupDate: string | null;
  sizeBytes: number | null;
  isAvailable: boolean;
}
