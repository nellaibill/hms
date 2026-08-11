/** Mirrors HMS.Modules.Masters.Contracts.DepartmentResponse (Department lives in Masters, not HR). */
export interface Department {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Masters.Contracts.CreateDepartmentRequest. */
export interface CreateDepartmentRequest {
  code: string;
  name: string;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.UpdateDepartmentRequest — no Code, matching the
 * backend (Code is Department's natural key, set only at creation). */
export interface UpdateDepartmentRequest {
  name: string;
  isActive: boolean;
}

/** Mirrors HMS.Modules.Masters.Contracts.DepartmentListQuery. */
export interface DepartmentListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}
