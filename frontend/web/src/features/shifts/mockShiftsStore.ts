import type { CreateShiftRequest, PaginationMeta, Shift, ShiftListQuery, UpdateShiftRequest } from '@hms/shared';

/**
 * Offline fallback store used only when the real API is unreachable (see the NetworkError
 * catches in the Shifts hooks) — mirrors features/roles/mockRolesStore.ts. Persisted to
 * localStorage so the demo survives page refreshes.
 */
const STORAGE_KEY = 'hms-mock-shifts';

function seedShifts(): Shift[] {
  const now = new Date().toISOString();
  const seeds: Array<Omit<Shift, 'createdAt' | 'updatedAt'>> = [
    { id: 'shift-001', code: 'MORNING', name: 'Morning Shift', startTime: '07:00:00', endTime: '15:00:00', breakMinutes: 30, graceMinutes: 10, isNightShift: false, isActive: true },
    { id: 'shift-002', code: 'EVENING', name: 'Evening Shift', startTime: '15:00:00', endTime: '23:00:00', breakMinutes: 30, graceMinutes: 10, isNightShift: false, isActive: true },
    { id: 'shift-003', code: 'NIGHT', name: 'Night Shift', startTime: '23:00:00', endTime: '07:00:00', breakMinutes: 30, graceMinutes: 15, isNightShift: true, isActive: true },
    { id: 'shift-004', code: 'GENERAL', name: 'General Duty', startTime: '09:00:00', endTime: '17:00:00', breakMinutes: 60, graceMinutes: 10, isNightShift: false, isActive: true },
    { id: 'shift-005', code: 'OPD', name: 'OPD Shift', startTime: '08:00:00', endTime: '14:00:00', breakMinutes: 15, graceMinutes: 5, isNightShift: false, isActive: true },
  ];
  return seeds.map((s) => ({ ...s, createdAt: now, updatedAt: null }));
}

function loadShifts(): Shift[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as Shift[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed;
      }
    }
  } catch {
    // Corrupt/unavailable storage — fall through to seed data.
  }
  return seedShifts();
}

function persist() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(shifts));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
  }
}

let shifts: Shift[] = loadShifts();
let nextSeq = shifts.reduce((max, s) => Math.max(max, Number(s.id.replace('shift-', '')) || 0), 0) + 1;

export interface PagedShifts {
  items: Shift[];
  meta: PaginationMeta;
}

function findShift(id: string): Shift {
  const shift = shifts.find((s) => s.id === id);
  if (!shift) {
    throw new Error(`Mock shift ${id} not found.`);
  }
  return shift;
}

export function listMockShifts(query: ShiftListQuery): PagedShifts {
  const page = query.page ?? 1;
  const pageSize = query.pageSize ?? 20;
  const search = query.search?.trim().toLowerCase();

  let items = shifts;
  if (query.isActive !== undefined) {
    items = items.filter((s) => s.isActive === query.isActive);
  }
  if (search) {
    items = items.filter((s) => [s.code, s.name].some((field) => field.toLowerCase().includes(search)));
  }

  const sort = query.sort ?? 'code';
  const direction = sort.startsWith('-') ? -1 : 1;
  const field = (sort.startsWith('-') ? sort.slice(1) : sort) as keyof Shift;
  items = [...items].sort((a, b) => String(a[field] ?? '').localeCompare(String(b[field] ?? '')) * direction);

  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = (page - 1) * pageSize;
  const pageItems = items.slice(start, start + pageSize);

  return { items: pageItems, meta: { page, pageSize, totalCount, totalPages } };
}

export function getMockShiftById(id: string): Shift | undefined {
  return shifts.find((s) => s.id === id);
}

export function createMockShift(request: CreateShiftRequest): Shift {
  const seq = nextSeq++;
  const now = new Date().toISOString();
  const shift: Shift = {
    id: `shift-${String(seq).padStart(3, '0')}`,
    code: request.code,
    name: request.name,
    startTime: request.startTime,
    endTime: request.endTime,
    breakMinutes: request.breakMinutes,
    graceMinutes: request.graceMinutes,
    isNightShift: request.isNightShift,
    isActive: request.isActive,
    createdAt: now,
    updatedAt: null,
  };
  shifts = [shift, ...shifts];
  persist();
  return shift;
}

export function updateMockShift(id: string, request: UpdateShiftRequest): Shift {
  const existing = findShift(id);
  const updated: Shift = {
    ...existing,
    name: request.name,
    startTime: request.startTime,
    endTime: request.endTime,
    breakMinutes: request.breakMinutes,
    graceMinutes: request.graceMinutes,
    isNightShift: request.isNightShift,
    isActive: request.isActive,
    updatedAt: new Date().toISOString(),
  };
  shifts = shifts.map((s) => (s.id === id ? updated : s));
  persist();
  return updated;
}

export function deleteMockShift(id: string): void {
  findShift(id);
  shifts = shifts.filter((s) => s.id !== id);
  persist();
}
