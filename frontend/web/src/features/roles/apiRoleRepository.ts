import type {
  CreateRoleRequest as CreateRoleRequestDto,
  PaginationMeta,
  RoleListQuery as RoleListQueryDto,
  RoleResponse as RoleResponseDto,
  UpdateRoleRequest as UpdateRoleRequestDto,
} from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { rolesApi } from '@/services/apiClient';
import { createMockRole, getMockRoleById, listMockRoles, updateMockRole } from './mockRolesStore';
import { buildEmptyPermissions, ROLE_MODULES } from './modules';
import { PERMISSION_ACTIONS, type ModulePermissions, type Role, type RoleFormValues, type RoleListQuery } from './types';

export interface PagedRoles {
  items: Role[];
  meta: PaginationMeta;
  /** 'mock' when the real backend was unreachable and this list came from the offline fallback below. */
  source: 'live' | 'mock';
}

/**
 * Real, database-backed repository (HMS.Modules.Identity) — implements the same shape as
 * mockRolesStore.ts so the hooks built against the mock store keep working unchanged. Falls
 * back to that same mock store on NetworkError (backend genuinely unreachable, e.g. the
 * static GitHub Pages deploy) — a real ApiError (backend up, request rejected) still
 * surfaces normally. Mirrors the pattern in features/patients/mockPatientsStore.ts and
 * features/masters/engine/masterStoreFactory.ts.
 *
 * The backend only returns granted permission keys (e.g. "patient-management.view"), not
 * the full module x action grid the UI works with — fromDto() reconstructs the dense
 * matrix by combining those keys with the same ROLE_MODULES/PERMISSION_ACTIONS list the
 * matrix UI already renders from.
 */

function fromDto(dto: RoleResponseDto): Role {
  const granted = new Set(dto.permissionKeys);
  const permissions: Record<string, ModulePermissions> = buildEmptyPermissions();

  for (const module of ROLE_MODULES) {
    for (const action of PERMISSION_ACTIONS) {
      permissions[module.id][action] = granted.has(`${module.id}.${action}`);
    }
  }

  return {
    id: dto.id,
    name: dto.name,
    description: dto.description ?? '',
    status: dto.isActive ? 'Active' : 'Inactive',
    permissions,
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt ?? dto.createdAt,
  };
}

function toPermissionKeys(permissions: Record<string, ModulePermissions>): string[] {
  const keys: string[] = [];
  for (const module of ROLE_MODULES) {
    const modulePerms = permissions[module.id];
    if (!modulePerms) continue;
    for (const action of PERMISSION_ACTIONS) {
      if (modulePerms[action]) {
        keys.push(`${module.id}.${action}`);
      }
    }
  }
  return keys;
}

function toRequestBody(values: RoleFormValues): CreateRoleRequestDto | UpdateRoleRequestDto {
  return {
    name: values.name,
    description: values.description || null,
    permissionKeys: toPermissionKeys(values.permissions),
  };
}

function toListQueryDto(query: RoleListQuery): RoleListQueryDto {
  return {
    page: query.page,
    pageSize: query.pageSize,
    sort: query.sort,
    search: query.search,
    isActive: query.status === undefined ? undefined : query.status === 'Active',
  };
}

export async function listRoles(query: RoleListQuery): Promise<PagedRoles> {
  try {
    const paged = await rolesApi.getRoles(toListQueryDto(query));
    return {
      items: paged.items.map(fromDto),
      meta: paged.meta,
      source: 'live',
    };
  } catch (err) {
    if (err instanceof NetworkError) {
      return { ...listMockRoles(query), source: 'mock' };
    }
    throw err;
  }
}

export async function getRoleById(id: string): Promise<Role> {
  try {
    const dto = await rolesApi.getRoleById(id);
    return fromDto(dto);
  } catch (err) {
    if (err instanceof NetworkError) {
      const mock = getMockRoleById(id);
      if (mock) {
        return mock;
      }
    }
    throw err;
  }
}

export async function createRole(values: RoleFormValues): Promise<Role> {
  try {
    const created = await rolesApi.createRole(toRequestBody(values));

    // CreateRoleRequest has no status field yet — Role.Create() always creates as Active, so
    // a follow-up call is only needed when Inactive was actually requested.
    if (values.status === 'Inactive') {
      const deactivated = await rolesApi.deactivateRole(created.id);
      return fromDto(deactivated);
    }

    return fromDto(created);
  } catch (err) {
    if (err instanceof NetworkError) {
      return createMockRole(values);
    }
    throw err;
  }
}

export async function updateRole(id: string, values: RoleFormValues): Promise<Role> {
  try {
    await rolesApi.updateRole(id, toRequestBody(values));

    // UpdateRoleRequest doesn't carry status either — apply it via the dedicated
    // activate/deactivate endpoints. Both are no-ops server-side if the role is already in
    // the requested state, so calling unconditionally here is safe regardless of prior status.
    const result =
      values.status === 'Active' ? await rolesApi.activateRole(id) : await rolesApi.deactivateRole(id);

    return fromDto(result);
  } catch (err) {
    if (err instanceof NetworkError) {
      return updateMockRole(id, values);
    }
    throw err;
  }
}
