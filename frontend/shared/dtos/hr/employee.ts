import type { EmployeeGender, EmployeeType, EmploymentStatus } from '../../enums/hr';
import type { PagedQuery } from '../../types';

/** Mirrors HMS.Modules.HR.Contracts.CreateEmployeeRequest. */
export interface CreateEmployeeRequest {
  employeeCode: string;
  firstName: string;
  lastName: string;
  gender: EmployeeGender;
  dateOfBirth: string;
  phone: string;
  email: string;
  address: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  departmentId: string;
  designationId: string;
  employeeType: EmployeeType;
  joiningDate: string;
  employmentStatus: EmploymentStatus;
  reportingManagerId?: string | null;
  profilePhotoUrl?: string | null;
  userId?: string | null;
  isActive: boolean;
}

/** Mirrors HMS.Modules.HR.Contracts.UpdateEmployeeRequest — no EmployeeCode (Employee's
 * natural key, set only at creation, matching the backend). */
export interface UpdateEmployeeRequest {
  firstName: string;
  lastName: string;
  gender: EmployeeGender;
  dateOfBirth: string;
  phone: string;
  email: string;
  address: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  departmentId: string;
  designationId: string;
  employeeType: EmployeeType;
  joiningDate: string;
  employmentStatus: EmploymentStatus;
  reportingManagerId?: string | null;
  profilePhotoUrl?: string | null;
  userId?: string | null;
  isActive: boolean;
}

/**
 * Mirrors HMS.Modules.HR.Contracts.EmployeeResponse. departmentName/designationName/
 * reportingManagerName are populated ONLY on the single-record GET /{id} response — always
 * null on the paged list response (a deliberate backend choice to avoid N+1 cross-module
 * lookups on every list row). The Employees list table resolves these via a client-side
 * lookup map (see useDepartmentDirectory/useDesignationDirectory) instead of relying on
 * these fields.
 */
export interface EmployeeResponse {
  id: string;
  employeeCode: string;
  firstName: string;
  lastName: string;
  gender: EmployeeGender;
  dateOfBirth: string;
  phone: string;
  email: string;
  address: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  departmentId: string;
  departmentName?: string | null;
  designationId: string;
  designationName?: string | null;
  employeeType: EmployeeType;
  joiningDate: string;
  employmentStatus: EmploymentStatus;
  reportingManagerId?: string | null;
  reportingManagerName?: string | null;
  profilePhotoUrl?: string | null;
  userId?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.EmployeeListQuery. Search spans EmployeeCode/FirstName/
 * LastName/Email. */
export interface EmployeeListQuery extends PagedQuery {
  departmentId?: string;
  designationId?: string;
  employeeType?: EmployeeType;
  employmentStatus?: EmploymentStatus;
  isActive?: boolean;
}

/** Mirrors HMS.Modules.HR.Contracts.EmployeeLeaveBalanceResponse. */
export interface EmployeeLeaveBalanceResponse {
  leaveTypeId: string;
  leaveTypeName: string;
  maxDaysPerYear: number | null;
  usedDays: number;
  /** Null when maxDaysPerYear is null (unlimited) — there is no meaningful "remaining"
   * figure for an unlimited leave type. */
  remainingDays: number | null;
}
