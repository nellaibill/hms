import type { PagedQuery } from '../../types';

/** Mirrors HMS.Modules.HR.Contracts.CreateLeaveTypeRequest. */
export interface CreateLeaveTypeRequest {
  code: string;
  name: string;
  /** Null means unlimited. */
  maxDaysPerYear?: number | null;
  isPaid: boolean;
  isActive: boolean;
}

/** Mirrors HMS.Modules.HR.Contracts.UpdateLeaveTypeRequest — no Code (natural key, immutable
 * after create, matching the backend). */
export interface UpdateLeaveTypeRequest {
  name: string;
  maxDaysPerYear?: number | null;
  isPaid: boolean;
  isActive: boolean;
}

/** Mirrors HMS.Modules.HR.Contracts.LeaveTypeResponse. */
export interface LeaveTypeResponse {
  id: string;
  code: string;
  name: string;
  maxDaysPerYear?: number | null;
  isPaid: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.LeaveTypeListQuery. */
export interface LeaveTypeListQuery extends PagedQuery {
  isActive?: boolean;
}
