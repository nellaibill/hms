/** Mirrors HMS.Modules.ActivityLog.Contracts.ActivityLogResponse. */
export interface ActivityLogEntry {
  id: string;
  userId?: string | null;
  action: string;
  module: string;
  entityType?: string | null;
  entityId?: string | null;
  description?: string | null;
  isSuccess: boolean;
  createdAt: string;
}

/** Mirrors HMS.Modules.ActivityLog.Contracts.ActivityLogDetailResponse. */
export interface ActivityLogDetail extends ActivityLogEntry {
  tenantId?: string | null;
  /** Sanitized JSON snapshot (any shape) — null when the event carried no before/after state. */
  oldValues?: unknown;
  newValues?: unknown;
  ipAddress?: string | null;
  userAgent?: string | null;
  correlationId?: string | null;
}

/** Mirrors HMS.Modules.ActivityLog.Contracts.ActivityLogListQuery. */
export interface ActivityLogListQuery {
  page?: number;
  pageSize?: number;
  /** Inclusive lower bound (ISO date/time, UTC). */
  from?: string;
  /** Inclusive upper bound; a date-only value means through the end of that day. */
  to?: string;
  userId?: string;
  module?: string;
  action?: string;
  entityType?: string;
  entityId?: string;
  search?: string;
}
