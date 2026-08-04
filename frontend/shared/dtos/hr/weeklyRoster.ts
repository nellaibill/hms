/** Mirrors HMS.Modules.HR.Contracts.WeeklyRosterResponse. Roster-header only — does not
 * generate or reference ShiftAssignments (see HMS.Modules.HR.Endpoints.WeeklyRostersController's
 * own doc comment). */
export interface WeeklyRoster {
  id: string;
  weekStartDate: string;
  departmentId: string;
  published: boolean;
  publishedDate?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.CreateWeeklyRosterRequest. */
export interface CreateWeeklyRosterRequest {
  weekStartDate: string;
  departmentId: string;
  published: boolean;
  publishedDate?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.UpdateWeeklyRosterRequest. */
export interface UpdateWeeklyRosterRequest {
  weekStartDate: string;
  departmentId: string;
  published: boolean;
  publishedDate?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.WeeklyRosterListQuery. */
export interface WeeklyRosterListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
}

/** Mirrors HMS.Modules.HR.Contracts.CopyWeeklyRosterRequest — POST /weekly-rosters/{id}/copy's
 * body. The caller explicitly chooses the destination week rather than the endpoint
 * silently adding 7 days to the source roster's week. */
export interface CopyWeeklyRosterRequest {
  targetWeekStartDate: string;
}
