import type {
  CopyWeeklyRosterRequest,
  CreateWeeklyRosterRequest,
  MonthlyWeeklyRosterQuery,
  PaginationMeta,
  UpdateWeeklyRosterRequest,
  WeeklyRoster,
  WeeklyRosterListQuery,
} from '@hms/shared';

/**
 * Offline fallback store used only when the real API is unreachable (see the NetworkError
 * catches in the Weekly Roster hooks) — mirrors features/roles/mockRolesStore.ts. Persisted
 * to localStorage so the demo survives page refreshes. departmentId values are fixed demo
 * GUIDs (no Department module exists — same gap the real backend has) reused across the
 * Weekly Roster and Shift Assignment mock seeds for consistency.
 */
const STORAGE_KEY = 'hms-mock-weekly-rosters';

const DEMO_DEPARTMENT_IDS = [
  '10000000-0000-0000-0000-000000000001', // General Medicine
  '10000000-0000-0000-0000-000000000002', // Emergency
  '10000000-0000-0000-0000-000000000003', // ICU
];

function seedRosters(): WeeklyRoster[] {
  const now = new Date().toISOString();
  const seeds: Array<Omit<WeeklyRoster, 'createdAt' | 'updatedAt'>> = [
    { id: 'roster-001', weekStartDate: '2026-08-03', departmentId: DEMO_DEPARTMENT_IDS[0], published: true, publishedDate: '2026-07-28T09:00:00.000Z' },
    { id: 'roster-002', weekStartDate: '2026-08-10', departmentId: DEMO_DEPARTMENT_IDS[0], published: true, publishedDate: '2026-08-04T09:00:00.000Z' },
    { id: 'roster-003', weekStartDate: '2026-08-17', departmentId: DEMO_DEPARTMENT_IDS[0], published: false, publishedDate: null },
    { id: 'roster-004', weekStartDate: '2026-08-10', departmentId: DEMO_DEPARTMENT_IDS[1], published: true, publishedDate: '2026-08-03T09:00:00.000Z' },
    { id: 'roster-005', weekStartDate: '2026-08-10', departmentId: DEMO_DEPARTMENT_IDS[2], published: false, publishedDate: null },
  ];
  return seeds.map((s) => ({ ...s, createdAt: now, updatedAt: null }));
}

function loadRosters(): WeeklyRoster[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as WeeklyRoster[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed;
      }
    }
  } catch {
    // Corrupt/unavailable storage — fall through to seed data.
  }
  return seedRosters();
}

function persist() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(rosters));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
  }
}

let rosters: WeeklyRoster[] = loadRosters();
let nextSeq = rosters.reduce((max, r) => Math.max(max, Number(r.id.replace('roster-', '')) || 0), 0) + 1;

export interface PagedWeeklyRosters {
  items: WeeklyRoster[];
  meta: PaginationMeta;
}

function findRoster(id: string): WeeklyRoster {
  const roster = rosters.find((r) => r.id === id);
  if (!roster) {
    throw new Error(`Mock weekly roster ${id} not found.`);
  }
  return roster;
}

function paginate(items: WeeklyRoster[], page: number, pageSize: number, sort: string): PagedWeeklyRosters {
  const direction = sort.startsWith('-') ? -1 : 1;
  const field = (sort.startsWith('-') ? sort.slice(1) : sort) as keyof WeeklyRoster;
  const sorted = [...items].sort((a, b) => String(a[field] ?? '').localeCompare(String(b[field] ?? '')) * direction);

  const totalCount = sorted.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = (page - 1) * pageSize;
  const pageItems = sorted.slice(start, start + pageSize);

  return { items: pageItems, meta: { page, pageSize, totalCount, totalPages } };
}

export function listMockWeeklyRosters(query: WeeklyRosterListQuery): PagedWeeklyRosters {
  const page = query.page ?? 1;
  const pageSize = query.pageSize ?? 20;
  return paginate(rosters, page, pageSize, query.sort ?? '-weekStartDate');
}

export function getMockWeeklyRosterById(id: string): WeeklyRoster | undefined {
  return rosters.find((r) => r.id === id);
}

export function createMockWeeklyRoster(request: CreateWeeklyRosterRequest): WeeklyRoster {
  const seq = nextSeq++;
  const now = new Date().toISOString();
  const roster: WeeklyRoster = {
    id: `roster-${String(seq).padStart(3, '0')}`,
    weekStartDate: request.weekStartDate,
    departmentId: request.departmentId,
    published: request.published,
    publishedDate: request.publishedDate ?? null,
    createdAt: now,
    updatedAt: null,
  };
  rosters = [roster, ...rosters];
  persist();
  return roster;
}

export function updateMockWeeklyRoster(id: string, request: UpdateWeeklyRosterRequest): WeeklyRoster {
  const existing = findRoster(id);
  const updated: WeeklyRoster = {
    ...existing,
    weekStartDate: request.weekStartDate,
    departmentId: request.departmentId,
    published: request.published,
    publishedDate: request.publishedDate ?? null,
    updatedAt: new Date().toISOString(),
  };
  rosters = rosters.map((r) => (r.id === id ? updated : r));
  persist();
  return updated;
}

export function deleteMockWeeklyRoster(id: string): void {
  findRoster(id);
  rosters = rosters.filter((r) => r.id !== id);
  persist();
}

/** Idempotent, matching the real backend — publishing an already-published roster is a no-op. */
export function publishMockWeeklyRoster(id: string): WeeklyRoster {
  const existing = findRoster(id);
  if (existing.published) {
    return existing;
  }
  const updated: WeeklyRoster = { ...existing, published: true, publishedDate: new Date().toISOString(), updatedAt: new Date().toISOString() };
  rosters = rosters.map((r) => (r.id === id ? updated : r));
  persist();
  return updated;
}

/** Duplicates the source roster's department onto a new, unpublished roster for the
 * caller-chosen target week — mirrors WeeklyRostersController.Copy. */
export function copyMockWeeklyRoster(id: string, request: CopyWeeklyRosterRequest): WeeklyRoster {
  const source = findRoster(id);
  return createMockWeeklyRoster({
    weekStartDate: request.targetWeekStartDate,
    departmentId: source.departmentId,
    published: false,
    publishedDate: null,
  });
}

/** Rosters whose weekStartDate falls within the given calendar month — mirrors
 * WeeklyRostersController.GetMonthly (a read-only view over the same aggregate). */
export function getMockWeeklyRostersForMonth(query: MonthlyWeeklyRosterQuery): PagedWeeklyRosters {
  const page = query.page ?? 1;
  const pageSize = query.pageSize ?? 20;
  const items = rosters.filter((r) => {
    const date = new Date(r.weekStartDate);
    return date.getFullYear() === query.year && date.getMonth() + 1 === query.month;
  });
  return paginate(items, page, pageSize, query.sort ?? 'weekStartDate');
}
