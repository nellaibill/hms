export type RoleStatus = 'Active' | 'Inactive';

export const PERMISSION_ACTIONS = ['view', 'create', 'edit', 'delete'] as const;
export type PermissionAction = (typeof PERMISSION_ACTIONS)[number];

export const PERMISSION_ACTION_LABELS: Record<PermissionAction, string> = {
  view: 'View',
  create: 'Create',
  edit: 'Edit',
  delete: 'Delete',
};

export type ModulePermissions = Record<PermissionAction, boolean>;

export interface RoleModule {
  id: string;
  label: string;
}

export interface Role {
  id: string;
  name: string;
  description: string;
  status: RoleStatus;
  /** Keyed by RoleModule.id. */
  permissions: Record<string, ModulePermissions>;
  createdAt: string;
  updatedAt: string;
}

export interface RoleFormValues {
  name: string;
  description: string;
  status: RoleStatus;
  permissions: Record<string, ModulePermissions>;
}

export interface RoleListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  status?: RoleStatus;
}

export function countGrantedPermissions(permissions: Record<string, ModulePermissions>): { granted: number; total: number } {
  let granted = 0;
  let total = 0;
  for (const modulePerms of Object.values(permissions)) {
    for (const action of PERMISSION_ACTIONS) {
      total += 1;
      if (modulePerms[action]) granted += 1;
    }
  }
  return { granted, total };
}
