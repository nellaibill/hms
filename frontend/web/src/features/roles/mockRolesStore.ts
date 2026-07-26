import type { PaginationMeta } from '@hms/shared';
import { MOCK_ROLES } from './mockRoles';
import type { Role, RoleFormValues, RoleListQuery } from './types';

/**
 * Roles Management has no backend yet — this is the only data source (not an offline
 * fallback like mockPatientsStore.ts). Persisted to localStorage so the demo survives
 * page refreshes.
 */
const STORAGE_KEY = 'hms-mock-roles';

function loadRoles(): Role[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as Role[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed;
      }
    }
  } catch {
    // Corrupt/unavailable storage — fall through to seed data.
  }
  return [...MOCK_ROLES];
}

function persist() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(roles));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
  }
}

let roles: Role[] = loadRoles();
let nextSeq = roles.reduce((max, r) => Math.max(max, Number(r.id.replace('role-', '')) || 0), 0) + 1;

export interface PagedRoles {
  items: Role[];
  meta: PaginationMeta;
}

export function listMockRoles(query: RoleListQuery): PagedRoles {
  const page = query.page ?? 1;
  const pageSize = query.pageSize ?? 20;
  const search = query.search?.trim().toLowerCase();

  let items = roles;
  if (query.status) {
    items = items.filter((r) => r.status === query.status);
  }
  if (search) {
    items = items.filter((r) => [r.name, r.description].some((field) => field.toLowerCase().includes(search)));
  }

  const sort = query.sort ?? 'name';
  const direction = sort.startsWith('-') ? -1 : 1;
  const field = sort.startsWith('-') ? sort.slice(1) : sort;
  items = [...items].sort((a, b) => {
    const left = field === 'updatedAt' ? a.updatedAt : a.name;
    const right = field === 'updatedAt' ? b.updatedAt : b.name;
    return left.localeCompare(right) * direction;
  });

  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = (page - 1) * pageSize;
  const pageItems = items.slice(start, start + pageSize);

  return {
    items: pageItems,
    meta: { page, pageSize, totalCount, totalPages },
  };
}

export function getMockRoleById(id: string): Role | undefined {
  return roles.find((r) => r.id === id);
}

export function createMockRole(values: RoleFormValues): Role {
  const seq = nextSeq++;
  const now = new Date().toISOString();
  const role: Role = {
    id: `role-${String(seq).padStart(3, '0')}`,
    name: values.name,
    description: values.description,
    status: values.status,
    permissions: values.permissions,
    createdAt: now,
    updatedAt: now,
  };
  roles = [role, ...roles];
  persist();
  return role;
}

export function updateMockRole(id: string, values: RoleFormValues): Role {
  const existing = getMockRoleById(id);
  if (!existing) {
    throw new Error(`Mock role ${id} not found.`);
  }
  const updated: Role = {
    ...existing,
    name: values.name,
    description: values.description,
    status: values.status,
    permissions: values.permissions,
    updatedAt: new Date().toISOString(),
  };
  roles = roles.map((r) => (r.id === id ? updated : r));
  persist();
  return updated;
}
