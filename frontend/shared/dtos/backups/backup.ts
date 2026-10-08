/** Mirrors HMS.Modules.Backups.Contracts.BackupSummaryResponse. */
export interface BackupSummaryResponse {
  key: string;
  label: string;
  /** ISO date string (yyyy-MM-dd), or null when no backup has run for this key yet. */
  lastBackupDate: string | null;
  sizeBytes: number | null;
  isAvailable: boolean;
}

/** Mirrors HMS.Modules.Backups.Contracts.TenantFileCategoryResponse. `key` is also this kind's
 * top-level folder name inside the zip. */
export interface TenantFileCategoryResponse {
  key: string;
  label: string;
  fileCount: number;
  sizeBytes: number;
}

/** Mirrors HMS.Modules.Backups.Contracts.TenantFilesSummaryResponse — what the documents &
 * images zip would contain right now. */
export interface TenantFilesSummaryResponse {
  fileCount: number;
  totalSizeBytes: number;
  categories: TenantFileCategoryResponse[];
}
