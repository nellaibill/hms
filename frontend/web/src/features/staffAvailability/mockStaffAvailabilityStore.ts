import type { CreateStaffAvailabilityRequest, PaginationMeta, StaffAvailability, StaffAvailabilityListQuery, UpdateStaffAvailabilityRequest } from '@hms/shared';

/**
 * Offline fallback store used only when the real API is unreachable (see the NetworkError
 * catches in the Staff Availability hooks) — mirrors features/roles/mockRolesStore.ts.
 * Persisted to localStorage so the demo survives page refreshes. staffId values reference
 * features/users/mockUsers.ts's seeded accounts so StaffName resolves real-looking names.
 */
const STORAGE_KEY = 'hms-mock-staff-availability';

function seedRecords(): StaffAvailability[] {
  const now = new Date().toISOString();
  const seeds: Array<Omit<StaffAvailability, 'createdAt' | 'updatedAt'>> = [
    { id: 'availability-001', staffId: 'user-005', startDate: '2026-08-10', endDate: '2026-08-15', availabilityStatus: 'Unavailable', reason: 'Annual leave' },
    { id: 'availability-002', staffId: 'user-004', startDate: '2026-08-12', endDate: '2026-08-12', availabilityStatus: 'Unavailable', reason: 'Conference' },
    { id: 'availability-003', staffId: 'user-006', startDate: '2026-08-01', endDate: '2026-08-31', availabilityStatus: 'Available', reason: null },
    { id: 'availability-004', staffId: 'user-008', startDate: '2026-08-20', endDate: '2026-08-22', availabilityStatus: 'Unavailable', reason: 'Training' },
  ];
  return seeds.map((s) => ({ ...s, createdAt: now, updatedAt: null }));
}

function loadRecords(): StaffAvailability[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as StaffAvailability[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed;
      }
    }
  } catch {
    // Corrupt/unavailable storage — fall through to seed data.
  }
  return seedRecords();
}

function persist() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(records));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
  }
}

let records: StaffAvailability[] = loadRecords();
let nextSeq = records.reduce((max, r) => Math.max(max, Number(r.id.replace('availability-', '')) || 0), 0) + 1;

export interface PagedStaffAvailability {
  items: StaffAvailability[];
  meta: PaginationMeta;
}

function findRecord(id: string): StaffAvailability {
  const record = records.find((r) => r.id === id);
  if (!record) {
    throw new Error(`Mock staff availability record ${id} not found.`);
  }
  return record;
}

export function listMockStaffAvailability(query: StaffAvailabilityListQuery): PagedStaffAvailability {
  const page = query.page ?? 1;
  const pageSize = query.pageSize ?? 20;
  const search = query.search?.trim().toLowerCase();

  let items = records;
  if (search) {
    items = items.filter((r) => (r.reason ?? '').toLowerCase().includes(search));
  }

  const sort = query.sort ?? '-startDate';
  const direction = sort.startsWith('-') ? -1 : 1;
  const field = (sort.startsWith('-') ? sort.slice(1) : sort) as keyof StaffAvailability;
  items = [...items].sort((a, b) => String(a[field] ?? '').localeCompare(String(b[field] ?? '')) * direction);

  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = (page - 1) * pageSize;
  const pageItems = items.slice(start, start + pageSize);

  return { items: pageItems, meta: { page, pageSize, totalCount, totalPages } };
}

export function getMockStaffAvailabilityById(id: string): StaffAvailability | undefined {
  return records.find((r) => r.id === id);
}

export function createMockStaffAvailability(request: CreateStaffAvailabilityRequest): StaffAvailability {
  const seq = nextSeq++;
  const now = new Date().toISOString();
  const record: StaffAvailability = {
    id: `availability-${String(seq).padStart(3, '0')}`,
    staffId: request.staffId,
    startDate: request.startDate,
    endDate: request.endDate,
    availabilityStatus: request.availabilityStatus ?? 'Available',
    reason: request.reason ?? null,
    createdAt: now,
    updatedAt: null,
  };
  records = [record, ...records];
  persist();
  return record;
}

export function updateMockStaffAvailability(id: string, request: UpdateStaffAvailabilityRequest): StaffAvailability {
  const existing = findRecord(id);
  const updated: StaffAvailability = {
    ...existing,
    staffId: request.staffId,
    startDate: request.startDate,
    endDate: request.endDate,
    availabilityStatus: request.availabilityStatus ?? existing.availabilityStatus,
    reason: request.reason ?? null,
    updatedAt: new Date().toISOString(),
  };
  records = records.map((r) => (r.id === id ? updated : r));
  persist();
  return updated;
}

export function deleteMockStaffAvailability(id: string): void {
  findRecord(id);
  records = records.filter((r) => r.id !== id);
  persist();
}
