import type { AssignmentStatus } from '../../enums/hr';

/** Mirrors HMS.Modules.HR.Contracts.ShiftAssignmentResponse. StaffId/DepartmentId are plain
 * Guid references with no backing entity yet — no Staff/Department module exists (see
 * HMS.Modules.HR.Domain.ShiftAssignment's own doc comment). */
export interface ShiftAssignment {
  id: string;
  staffId: string;
  departmentId: string;
  shiftId: string;
  rosterDate: string;
  status: AssignmentStatus;
  remarks?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.CreateShiftAssignmentRequest. */
export interface CreateShiftAssignmentRequest {
  staffId: string;
  departmentId: string;
  shiftId: string;
  rosterDate: string;
  status: AssignmentStatus;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.UpdateShiftAssignmentRequest. */
export interface UpdateShiftAssignmentRequest {
  staffId: string;
  departmentId: string;
  shiftId: string;
  rosterDate: string;
  status: AssignmentStatus;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.ShiftAssignmentListQuery. */
export interface ShiftAssignmentListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
}
