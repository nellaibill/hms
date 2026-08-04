import type { SwapRequestStatus } from '../../enums/hr';

/** Mirrors HMS.Modules.HR.Contracts.SwapRequestResponse. No approval workflow, no
 * notifications, no automatic assignment changes — Status is simply stored (see
 * HMS.Modules.HR.Endpoints.ShiftSwapRequestsController's own doc comment). */
export interface SwapRequest {
  id: string;
  requestedByStaffId: string;
  requestedToStaffId: string;
  currentShiftAssignmentId: string;
  requestedShiftAssignmentId: string;
  status: SwapRequestStatus;
  requestedDate: string;
  approvedDate?: string | null;
  approvedBy?: string | null;
  remarks?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.CreateSwapRequest. */
export interface CreateSwapRequest {
  requestedByStaffId: string;
  requestedToStaffId: string;
  currentShiftAssignmentId: string;
  requestedShiftAssignmentId: string;
  status: SwapRequestStatus;
  requestedDate: string;
  approvedDate?: string | null;
  approvedBy?: string | null;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.UpdateSwapRequest. */
export interface UpdateSwapRequest {
  requestedByStaffId: string;
  requestedToStaffId: string;
  currentShiftAssignmentId: string;
  requestedShiftAssignmentId: string;
  status: SwapRequestStatus;
  requestedDate: string;
  approvedDate?: string | null;
  approvedBy?: string | null;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.SwapRequestListQuery. */
export interface SwapRequestListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
}
