import type { CreateSwapRequest, PaginationMeta, SwapRequest, SwapRequestListQuery, UpdateSwapRequest } from '@hms/shared';

/**
 * Offline fallback store used only when the real API is unreachable (see the NetworkError
 * catches in the Shift Swap Request hooks) — mirrors features/roles/mockRolesStore.ts.
 * Persisted to localStorage so the demo survives page refreshes. Staff ids reference
 * mockUsers.ts; assignment ids reference mockShiftAssignmentsStore.ts's seeded records.
 */
const STORAGE_KEY = 'hms-mock-shift-swap-requests';

function seedRequests(): SwapRequest[] {
  const now = new Date().toISOString();
  const seeds: Array<Omit<SwapRequest, 'createdAt' | 'updatedAt'>> = [
    {
      id: 'swap-001',
      requestedByStaffId: 'user-005',
      requestedToStaffId: 'user-006',
      currentShiftAssignmentId: 'assignment-002',
      requestedShiftAssignmentId: 'assignment-003',
      status: 'Pending',
      requestedDate: '2026-08-06T09:00:00.000Z',
      approvedDate: null,
      approvedBy: null,
      remarks: 'Family function that week',
    },
    {
      id: 'swap-002',
      requestedByStaffId: 'user-004',
      requestedToStaffId: 'user-008',
      currentShiftAssignmentId: 'assignment-001',
      requestedShiftAssignmentId: 'assignment-005',
      status: 'Approved',
      requestedDate: '2026-08-01T09:00:00.000Z',
      approvedDate: '2026-08-02T10:00:00.000Z',
      approvedBy: 'user-009',
      remarks: null,
    },
  ];
  return seeds.map((s) => ({ ...s, createdAt: now, updatedAt: null }));
}

function loadRequests(): SwapRequest[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as SwapRequest[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed;
      }
    }
  } catch {
    // Corrupt/unavailable storage — fall through to seed data.
  }
  return seedRequests();
}

function persist() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(requests));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
  }
}

let requests: SwapRequest[] = loadRequests();
let nextSeq = requests.reduce((max, r) => Math.max(max, Number(r.id.replace('swap-', '')) || 0), 0) + 1;

export interface PagedSwapRequests {
  items: SwapRequest[];
  meta: PaginationMeta;
}

function findRequest(id: string): SwapRequest {
  const request = requests.find((r) => r.id === id);
  if (!request) {
    throw new Error(`Mock shift swap request ${id} not found.`);
  }
  return request;
}

export function listMockSwapRequests(query: SwapRequestListQuery): PagedSwapRequests {
  const page = query.page ?? 1;
  const pageSize = query.pageSize ?? 20;
  const search = query.search?.trim().toLowerCase();

  let items = requests;
  if (search) {
    items = items.filter((r) => (r.remarks ?? '').toLowerCase().includes(search));
  }

  const sort = query.sort ?? '-requestedDate';
  const direction = sort.startsWith('-') ? -1 : 1;
  const field = (sort.startsWith('-') ? sort.slice(1) : sort) as keyof SwapRequest;
  items = [...items].sort((a, b) => String(a[field] ?? '').localeCompare(String(b[field] ?? '')) * direction);

  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const start = (page - 1) * pageSize;
  const pageItems = items.slice(start, start + pageSize);

  return { items: pageItems, meta: { page, pageSize, totalCount, totalPages } };
}

export function getMockSwapRequestById(id: string): SwapRequest | undefined {
  return requests.find((r) => r.id === id);
}

export function createMockSwapRequest(request: CreateSwapRequest): SwapRequest {
  const seq = nextSeq++;
  const now = new Date().toISOString();
  const created: SwapRequest = {
    id: `swap-${String(seq).padStart(3, '0')}`,
    requestedByStaffId: request.requestedByStaffId,
    requestedToStaffId: request.requestedToStaffId,
    currentShiftAssignmentId: request.currentShiftAssignmentId,
    requestedShiftAssignmentId: request.requestedShiftAssignmentId,
    status: request.status ?? 'Pending',
    requestedDate: request.requestedDate,
    approvedDate: request.approvedDate ?? null,
    approvedBy: request.approvedBy ?? null,
    remarks: request.remarks ?? null,
    createdAt: now,
    updatedAt: null,
  };
  requests = [created, ...requests];
  persist();
  return created;
}

export function updateMockSwapRequest(id: string, request: UpdateSwapRequest): SwapRequest {
  const existing = findRequest(id);
  const updated: SwapRequest = {
    ...existing,
    requestedByStaffId: request.requestedByStaffId,
    requestedToStaffId: request.requestedToStaffId,
    currentShiftAssignmentId: request.currentShiftAssignmentId,
    requestedShiftAssignmentId: request.requestedShiftAssignmentId,
    status: request.status ?? existing.status,
    requestedDate: request.requestedDate,
    approvedDate: request.approvedDate ?? null,
    approvedBy: request.approvedBy ?? null,
    remarks: request.remarks ?? null,
    updatedAt: new Date().toISOString(),
  };
  requests = requests.map((r) => (r.id === id ? updated : r));
  persist();
  return updated;
}

export function deleteMockSwapRequest(id: string): void {
  findRequest(id);
  requests = requests.filter((r) => r.id !== id);
  persist();
}
