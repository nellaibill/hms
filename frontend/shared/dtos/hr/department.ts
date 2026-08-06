/** Mirrors HMS.Modules.HR.Contracts.DepartmentResponse. */
export interface Department {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.CreateDepartmentRequest. */
export interface CreateDepartmentRequest {
  code: string;
  name: string;
  isActive: boolean;
}

/** Mirrors HMS.Modules.HR.Contracts.UpdateDepartmentRequest — no Code, matching the
 * backend (Code is Department's natural key, set only at creation). */
export interface UpdateDepartmentRequest {
  name: string;
  isActive: boolean;
}

/** Mirrors HMS.Modules.HR.Contracts.DepartmentListQuery. */
export interface DepartmentListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}
