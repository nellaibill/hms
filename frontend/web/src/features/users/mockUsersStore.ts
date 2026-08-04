import type { CreateUserRequest, PaginationMeta, UpdateUserRequest, User, UserListQuery } from '@hms/shared';
import { getMockRoleById } from '../roles/mockRolesStore';
import { MOCK_USERS } from './mockUsers';

/**
 * Offline fallback store used only when the real API is unreachable (see the NetworkError
 * catches in the Users hooks) — mirrors features/roles/mockRolesStore.ts and
 * features/patients/mockPatientsStore.ts. Persisted to localStorage so the demo survives
 * page refreshes. Seeded from mockUsers.ts — the same 10 records features/auth's mock login
 * signs in against.
 */
const STORAGE_KEY = 'hms-mock-users';

function seedUsers(): User[] {
  const now = new Date().toISOString();
  return MOCK_USERS.map((seed) => ({
    id: seed.id,
    username: seed.username,
    firstName: seed.firstName,
    lastName: seed.lastName,
    email: seed.email,
    phoneNumber: seed.phoneNumber,
    profilePhotoUrl: null,
    roleId: seed.roleId,
    roleName: seed.roleName,
    emailVerified: true,
    lastLoginAt: null,
    isActive: true,
    createdAt: now,
    updatedAt: null,
  }));
}

function loadUsers(): User[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as User[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed;
      }
    }
  } catch {
    // Corrupt/unavailable storage — fall through to seed data.
  }
  return seedUsers();
}

function persist() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(users));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
  }
}

let users: User[] = loadUsers();
let nextSeq = users.reduce((max, u) => Math.max(max, Number(u.id.replace('user-', '')) || 0), 0) + 1;

export interface PagedUsers {
  items: User[];
  meta: PaginationMeta;
}

function findUser(id: string): User {
  const user = users.find((u) => u.id === id);
  if (!user) {
    throw new Error(`Mock user ${id} not found.`);
  }
  return user;
}

/** Resolves a roleId to its display name via Roles' own mock store (same store
 * useRolesForSelect.ts already falls back to), so a newly-picked role shows correctly. */
function resolveRoleName(roleId: string, fallback?: string): string {
  return getMockRoleById(roleId)?.name ?? fallback ?? 'Unknown Role';
}

export function listMockUsers(query: UserListQuery): PagedUsers {
  const page = query.page ?? 1;
  const pageSize = query.pageSize ?? 20;
  const search = query.search?.trim().toLowerCase();

  let items = users;
  if (query.isActive !== undefined) {
    items = items.filter((u) => u.isActive === query.isActive);
  }
  if (search) {
    items = items.filter((u) =>
      [u.username, u.firstName, u.lastName, u.email].some((field) => field.toLowerCase().includes(search)),
    );
  }

  const sort = query.sort ?? '-createdAt';
  const direction = sort.startsWith('-') ? -1 : 1;
  const field = (sort.startsWith('-') ? sort.slice(1) : sort) as keyof User;
  items = [...items].sort((a, b) => {
    const left = String(a[field] ?? '');
    const right = String(b[field] ?? '');
    return left.localeCompare(right) * direction;
  });

  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = (page - 1) * pageSize;
  const pageItems = items.slice(start, start + pageSize);

  return { items: pageItems, meta: { page, pageSize, totalCount, totalPages } };
}

export function getMockUserById(id: string): User | undefined {
  return users.find((u) => u.id === id);
}

export function createMockUser(request: CreateUserRequest): User {
  const seq = nextSeq++;
  const now = new Date().toISOString();
  const user: User = {
    id: `user-${String(seq).padStart(3, '0')}`,
    username: request.username,
    firstName: request.firstName,
    lastName: request.lastName,
    email: request.email,
    phoneNumber: request.phoneNumber ?? null,
    profilePhotoUrl: null,
    roleId: request.roleId,
    roleName: resolveRoleName(request.roleId),
    emailVerified: false,
    lastLoginAt: null,
    isActive: true,
    createdAt: now,
    updatedAt: null,
  };
  users = [user, ...users];
  persist();
  return user;
}

export function updateMockUser(id: string, request: UpdateUserRequest): User {
  const existing = findUser(id);
  const updated: User = {
    ...existing,
    username: request.username,
    firstName: request.firstName,
    lastName: request.lastName,
    email: request.email,
    phoneNumber: request.phoneNumber ?? null,
    roleId: request.roleId,
    roleName: resolveRoleName(request.roleId, existing.roleName),
    updatedAt: new Date().toISOString(),
  };
  users = users.map((u) => (u.id === id ? updated : u));
  persist();
  return updated;
}

export function deleteMockUser(id: string): void {
  findUser(id);
  users = users.filter((u) => u.id !== id);
  persist();
}

export function activateMockUser(id: string): User {
  return setMockUserActive(id, true);
}

export function deactivateMockUser(id: string): User {
  return setMockUserActive(id, false);
}

function setMockUserActive(id: string, isActive: boolean): User {
  const existing = findUser(id);
  const updated: User = { ...existing, isActive, updatedAt: new Date().toISOString() };
  users = users.map((u) => (u.id === id ? updated : u));
  persist();
  return updated;
}

/** Demo mode never checks passwords (see mockAuthStore.ts) — this just needs to succeed. */
export function setMockUserPassword(id: string): User {
  const existing = findUser(id);
  const updated: User = { ...existing, updatedAt: new Date().toISOString() };
  users = users.map((u) => (u.id === id ? updated : u));
  persist();
  return updated;
}

export function uploadMockUserProfilePhoto(id: string, file: File): Promise<User> {
  const existing = findUser(id);
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const updated: User = { ...existing, profilePhotoUrl: reader.result as string, updatedAt: new Date().toISOString() };
      users = users.map((u) => (u.id === id ? updated : u));
      persist();
      resolve(updated);
    };
    reader.onerror = () => reject(reader.error ?? new Error('Failed to read profile photo file.'));
    reader.readAsDataURL(file);
  });
}
