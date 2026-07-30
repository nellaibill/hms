/** Mirrors HMS.Modules.Identity.Contracts.RoleResponse. */
export interface RoleResponse {
  id: string;
  name: string;
  description?: string | null;
  isSystemRole: boolean;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt?: string | null;
  /** Granted permission keys only, e.g. "patient-management.view" — absent keys mean not granted. */
  permissionKeys: string[];
}

/** Mirrors HMS.Modules.Identity.Contracts.CreateRoleRequest. */
export interface CreateRoleRequest {
  name: string;
  description?: string | null;
  displayOrder?: number;
  permissionKeys: string[];
}

/** Mirrors HMS.Modules.Identity.Contracts.UpdateRoleRequest. */
export interface UpdateRoleRequest {
  name: string;
  description?: string | null;
  displayOrder?: number;
  permissionKeys: string[];
}

/** Mirrors HMS.Modules.Identity.Contracts.RoleListQuery. */
export interface RoleListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}
