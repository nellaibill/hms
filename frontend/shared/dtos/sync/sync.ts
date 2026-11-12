/**
 * Offline-first cloud sync monitoring (ADR-133). Mirrors HMS.Modules.Sync.Contracts.
 * Status values only: the API never returns patient field values, payloads, the cloud
 * address or any key.
 */

/** Mirrors SyncOverallStatuses. */
export type SyncOverallStatus =
  | 'NotEnabled'
  | 'NotConfigured'
  | 'Disabled'
  | 'Paused'
  | 'AuthFailed'
  | 'Offline'
  | 'Attention'
  | 'Syncing'
  | 'Healthy';

/** Mirrors SyncEventStatuses (event and patient-stream statuses). */
export type SyncEventStatus = 'Pending' | 'Processing' | 'Synced' | 'Failed' | 'NeedsAttention' | 'Conflict';

/** Mirrors SyncResolutions. */
export type SyncResolution = 'LocalWins' | 'CloudWins' | 'Skip';

/** Mirrors SyncSummaryResponse. */
export interface SyncSummary {
  role: 'None' | 'Edge' | 'Cloud' | string;
  overallStatus: SyncOverallStatus;
  captureEnabled: boolean;
  sendingEnabled: boolean;
  hasCloudKey: boolean;
  intervalInMinutes: number;
  dashboardAvailable: boolean;
  isPaused: boolean;
  pausedAt?: string | null;
  authFailed: boolean;
  /** Null until the first connectivity probe has run. */
  cloudReachable?: boolean | null;
  lastProbeAt?: string | null;
  lastProbeError?: string | null;
  lastProbeLatencyMs?: number | null;
  lastSuccessfulSyncAt?: string | null;
  lastRunAt?: string | null;
  localDatabaseAvailable: boolean;
  pendingEvents: number;
  processingEvents: number;
  failedEvents: number;
  needsAttentionEvents: number;
  conflictEvents: number;
  syncedLast24Hours: number;
  patientsWithPendingChanges: number;
  blockedPatients: number;
  oldestPendingAt?: string | null;
}

/** Mirrors SyncStreamListQuery. `status` may also be "Unsynced". */
export interface SyncStreamListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: SyncEventStatus | 'Unsynced';
}

/** Mirrors SyncStreamRow: one patient. Name/UHID only for users who can view patients. */
export interface SyncStreamRow {
  patientId: string;
  uhid?: string | null;
  patientName?: string | null;
  patientDeleted?: boolean | null;
  status: SyncEventStatus;
  pendingEvents: number;
  lastSequence: number;
  lastSyncedSequence: number;
  retryCount: number;
  lastAttemptAt?: string | null;
  nextAttemptAt?: string | null;
  lastErrorCode?: string | null;
  lastErrorMessage?: string | null;
  headEventId?: string | null;
  updatedAt: string;
}

/** Mirrors SyncAttemptRow. */
export interface SyncAttemptRow {
  attemptNo: number;
  startedAt: string;
  finishedAt: string;
  outcome: string;
  httpStatus?: number | null;
  errorCode?: string | null;
  errorMessage?: string | null;
  durationMs: number;
}

/** Mirrors SyncEventRow: one outbox event (changed field names, never values). */
export interface SyncEventRow {
  eventId: string;
  patientId: string;
  uhid?: string | null;
  patientName?: string | null;
  sequenceNumber: number;
  operationType: 'Insert' | 'Update' | 'Delete' | 'Snapshot' | string;
  changedFields: string[];
  status: SyncEventStatus;
  retryCount: number;
  createdAt: string;
  lastAttemptAt?: string | null;
  nextRetryAt?: string | null;
  syncedAt?: string | null;
  errorCode?: string | null;
  errorMessage?: string | null;
  resolution?: SyncResolution | null;
  resolvedAt?: string | null;
  hasCloudVersion: boolean;
  attempts?: SyncAttemptRow[] | null;
}

/** Mirrors SyncEventListQuery. `status` is comma-separated; defaults to the failed ones. */
export interface SyncEventListQuery {
  page?: number;
  pageSize?: number;
  status?: string;
}

/** Mirrors SyncResolveRequest. */
export interface SyncResolveRequest {
  resolution: SyncResolution;
  note?: string;
}

/** Mirrors SyncActionResponse. */
export interface SyncActionResponse {
  affected: number;
  message: string;
}

/** Mirrors SyncDashboardSessionResponse. */
export interface SyncDashboardSession {
  /** Server-relative, one-time link (valid 60 s). */
  url: string;
  sessionExpiresAt: string;
}
