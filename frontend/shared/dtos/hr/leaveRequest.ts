import type { LeaveRequestStatus } from '../../enums/hr';
import type { PagedQuery } from '../../types';

/** Mirrors HMS.Modules.HR.Contracts.CreateLeaveRequestRequest. TotalDays is intentionally
 * absent — always computed server-side from startDate/endDate. */
export interface CreateLeaveRequestRequest {
  employeeId: string;
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  reason: string;
}

/** Mirrors HMS.Modules.HR.Contracts.ApproveLeaveRequestRequest. */
export interface ApproveLeaveRequestRequest {
  notes?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.RejectLeaveRequestRequest — a reason is required. */
export interface RejectLeaveRequestRequest {
  reason: string;
}

/** Mirrors HMS.Modules.HR.Contracts.LeaveRequestResponse. */
export interface LeaveRequestResponse {
  id: string;
  employeeId: string;
  employeeCode: string;
  employeeName: string;
  leaveTypeId: string;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  status: LeaveRequestStatus;
  approvedByUserId?: string | null;
  approvedAt?: string | null;
  decisionNotes?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.LeaveRequestListQuery. Filters on startDate falling
 * within [dateFrom, dateTo]. */
export interface LeaveRequestListQuery extends PagedQuery {
  employeeId?: string;
  leaveTypeId?: string;
  status?: LeaveRequestStatus;
  dateFrom?: string;
  dateTo?: string;
}
