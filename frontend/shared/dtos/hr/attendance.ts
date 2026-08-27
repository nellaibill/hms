import type { AttendanceStatus } from '../../enums/hr';
import type { PagedQuery } from '../../types';

/** Mirrors HMS.Modules.HR.Contracts.CreateAttendanceRequest. */
export interface CreateAttendanceRequest {
  employeeId: string;
  attendanceDate: string;
  checkInTime?: string | null;
  checkOutTime?: string | null;
  status: AttendanceStatus;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.UpdateAttendanceRequest — no EmployeeId/AttendanceDate
 * (the natural key, immutable after create, matching the backend). */
export interface UpdateAttendanceRequest {
  checkInTime?: string | null;
  checkOutTime?: string | null;
  status: AttendanceStatus;
  remarks?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.CheckInRequest. */
export interface CheckInRequest {
  employeeId: string;
  /** Defaults to the server's current UTC time when omitted. */
  checkInTime?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.CheckOutRequest. */
export interface CheckOutRequest {
  employeeId: string;
  /** Defaults to the server's current UTC time when omitted. */
  checkOutTime?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.AttendanceResponse. */
export interface AttendanceResponse {
  id: string;
  employeeId: string;
  employeeCode: string;
  employeeName: string;
  attendanceDate: string;
  checkInTime?: string | null;
  checkOutTime?: string | null;
  status: AttendanceStatus;
  remarks?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.AttendanceListQuery. */
export interface AttendanceListQuery extends PagedQuery {
  employeeId?: string;
  departmentId?: string;
  status?: AttendanceStatus;
  dateFrom?: string;
  dateTo?: string;
}
