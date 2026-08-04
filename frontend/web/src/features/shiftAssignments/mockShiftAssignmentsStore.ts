import type { CreateShiftAssignmentRequest, PaginationMeta, ShiftAssignment, ShiftAssignmentListQuery, UpdateShiftAssignmentRequest } from '@hms/shared';

/**
 * Offline fallback store used only when the real API is unreachable (see the NetworkError
 * catches in the Shift Assignment hooks) — mirrors features/roles/mockRolesStore.ts.
 * Persisted to localStorage so the demo survives page refreshes. staffId/shiftId values
 * reference mockUsers.ts/mockShiftsStore.ts's seeded records so StaffName/ShiftName resolve
 * real-looking labels; departmentId reuses the same demo department GUIDs as
 * mockWeeklyRostersStore.ts.
 */
const STORAGE_KEY = 'hms-mock-shift-assignments';

const DEMO_DEPARTMENT_IDS = [
  '10000000-0000-0000-0000-000000000001', // General Medicine
  '10000000-0000-0000-0000-000000000002', // Emergency
  '10000000-0000-0000-0000-000000000003', // ICU
];

function seedAssignments(): ShiftAssignment[] {
  const now = new Date().toISOString();
  const seeds: Array<Omit<ShiftAssignment, 'createdAt' | 'updatedAt'>> = [
    { id: 'assignment-001', staffId: 'user-004', departmentId: DEMO_DEPARTMENT_IDS[0], shiftId: 'shift-001', rosterDate: '2026-08-10', status: 'Scheduled', remarks: null },
    { id: 'assignment-002', staffId: 'user-005', departmentId: DEMO_DEPARTMENT_IDS[0], shiftId: 'shift-002', rosterDate: '2026-08-10', status: 'Scheduled', remarks: null },
    { id: 'assignment-003', staffId: 'user-006', departmentId: DEMO_DEPARTMENT_IDS[2], shiftId: 'shift-003', rosterDate: '2026-08-10', status: 'Scheduled', remarks: 'Covering for staff on leave' },
    { id: 'assignment-004', staffId: 'user-007', departmentId: DEMO_DEPARTMENT_IDS[1], shiftId: 'shift-001', rosterDate: '2026-08-09', status: 'Completed', remarks: null },
    { id: 'assignment-005', staffId: 'user-008', departmentId: DEMO_DEPARTMENT_IDS[0], shiftId: 'shift-005', rosterDate: '2026-08-11', status: 'Scheduled', remarks: null },
  ];
  return seeds.map((s) => ({ ...s, createdAt: now, updatedAt: null }));
}

function loadAssignments(): ShiftAssignment[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as ShiftAssignment[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed;
      }
    }
  } catch {
    // Corrupt/unavailable storage — fall through to seed data.
  }
  return seedAssignments();
}

function persist() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(assignments));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
  }
}

let assignments: ShiftAssignment[] = loadAssignments();
let nextSeq = assignments.reduce((max, a) => Math.max(max, Number(a.id.replace('assignment-', '')) || 0), 0) + 1;

export interface PagedShiftAssignments {
  items: ShiftAssignment[];
  meta: PaginationMeta;
}

function findAssignment(id: string): ShiftAssignment {
  const assignment = assignments.find((a) => a.id === id);
  if (!assignment) {
    throw new Error(`Mock shift assignment ${id} not found.`);
  }
  return assignment;
}

export function listMockShiftAssignments(query: ShiftAssignmentListQuery): PagedShiftAssignments {
  const page = query.page ?? 1;
  const pageSize = query.pageSize ?? 20;
  const search = query.search?.trim().toLowerCase();

  let items = assignments;
  if (search) {
    items = items.filter((a) => (a.remarks ?? '').toLowerCase().includes(search));
  }

  const sort = query.sort ?? '-rosterDate';
  const direction = sort.startsWith('-') ? -1 : 1;
  const field = (sort.startsWith('-') ? sort.slice(1) : sort) as keyof ShiftAssignment;
  items = [...items].sort((a, b) => String(a[field] ?? '').localeCompare(String(b[field] ?? '')) * direction);

  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = (page - 1) * pageSize;
  const pageItems = items.slice(start, start + pageSize);

  return { items: pageItems, meta: { page, pageSize, totalCount, totalPages } };
}

export function getMockShiftAssignmentById(id: string): ShiftAssignment | undefined {
  return assignments.find((a) => a.id === id);
}

export function createMockShiftAssignment(request: CreateShiftAssignmentRequest): ShiftAssignment {
  const seq = nextSeq++;
  const now = new Date().toISOString();
  const assignment: ShiftAssignment = {
    id: `assignment-${String(seq).padStart(3, '0')}`,
    staffId: request.staffId,
    departmentId: request.departmentId,
    shiftId: request.shiftId,
    rosterDate: request.rosterDate,
    status: request.status ?? 'Scheduled',
    remarks: request.remarks ?? null,
    createdAt: now,
    updatedAt: null,
  };
  assignments = [assignment, ...assignments];
  persist();
  return assignment;
}

export function updateMockShiftAssignment(id: string, request: UpdateShiftAssignmentRequest): ShiftAssignment {
  const existing = findAssignment(id);
  const updated: ShiftAssignment = {
    ...existing,
    staffId: request.staffId,
    departmentId: request.departmentId,
    shiftId: request.shiftId,
    rosterDate: request.rosterDate,
    status: request.status ?? existing.status,
    remarks: request.remarks ?? null,
    updatedAt: new Date().toISOString(),
  };
  assignments = assignments.map((a) => (a.id === id ? updated : a));
  persist();
  return updated;
}

export function deleteMockShiftAssignment(id: string): void {
  findAssignment(id);
  assignments = assignments.filter((a) => a.id !== id);
  persist();
}
