import { NetworkError, type MastersEntityKey } from '@hms/shared';
import { mastersApi } from '../../../services/apiClient';
import type { MasterEntityConfig, MasterListQuery, MasterRecord, PagedMasters } from './types';

/**
 * One store per entity, keyed by config.key. Talks to the real /api/v1/masters/* backend
 * first; only when that's genuinely unreachable (NetworkError, not a rejected request —
 * see docs/FrontendArchitecture.md §9) does it fall back to a seeded localStorage mock, the
 * same pattern features/patients/mockPatientsStore.ts uses. Same interface either way, so
 * the generic list/form pages and hooks built on top of it don't need per-entity code.
 */
export interface MasterStore {
  list(query: MasterListQuery): Promise<PagedMasters>;
  getById(id: string): Promise<MasterRecord | undefined>;
  getAll(): Promise<MasterRecord[]>;
  create(values: Record<string, unknown>): Promise<MasterRecord>;
  update(id: string, values: Record<string, unknown>): Promise<MasterRecord>;
}

/** These lookup lists are small enough that a single large page approximates "all records" — there's no true unpaged endpoint. */
const ALL_RECORDS_PAGE_SIZE = 1000;

// ---------------------------------------------------------------------------------------
// Offline fallback store — localStorage-backed, seeded from config.seed. Used only when the
// real API is unreachable (see the NetworkError catches in createMasterStore below).
// ---------------------------------------------------------------------------------------

function storageKey(entityKey: string) {
  return `hms-mock-masters-${entityKey}`;
}

function seedRecord(raw: Record<string, unknown> & { id: string }, index: number): MasterRecord {
  const now = new Date(2026, 0, 1 + index).toISOString();
  return {
    isActive: true,
    createdAt: now,
    updatedAt: now,
    ...raw,
  } as MasterRecord;
}

function loadRecords(config: MasterEntityConfig): MasterRecord[] {
  try {
    const raw = localStorage.getItem(storageKey(config.key));
    if (raw) {
      const parsed = JSON.parse(raw) as MasterRecord[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed;
      }
    }
  } catch {
    // Corrupt/unavailable storage — fall through to seed data.
  }
  return config.seed.map((row, index) => seedRecord(row, index));
}

function fieldValuesAreSearchable(config: MasterEntityConfig, record: MasterRecord, search: string): boolean {
  const haystack: string[] = [];
  if (config.codeField) haystack.push(String(record[config.codeField] ?? ''));
  if (config.nameField) haystack.push(String(record[config.nameField] ?? ''));
  for (const field of config.fields) {
    if (field.type === 'text' || field.type === 'textarea') {
      haystack.push(String(record[field.key] ?? ''));
    }
  }
  return haystack.some((value) => value.toLowerCase().includes(search));
}

interface LocalMasterStore {
  list(query: MasterListQuery): Omit<PagedMasters, 'source'>;
  getById(id: string): MasterRecord | undefined;
  getAll(): MasterRecord[];
  create(values: Record<string, unknown>): MasterRecord;
  update(id: string, values: Record<string, unknown>): MasterRecord;
}

function createMasterLocalStore(config: MasterEntityConfig): LocalMasterStore {
  let records: MasterRecord[] = loadRecords(config);
  let nextSeq =
    records.reduce((max, r) => {
      const match = /(\d+)$/.exec(r.id);
      return match ? Math.max(max, Number(match[1])) : max;
    }, 0) + 1;

  function persist() {
    try {
      localStorage.setItem(storageKey(config.key), JSON.stringify(records));
    } catch {
      // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
    }
  }

  function sortField(sort: string): string {
    return sort.startsWith('-') ? sort.slice(1) : sort;
  }

  function list(query: MasterListQuery): Omit<PagedMasters, 'source'> {
    const page = query.page ?? 1;
    const pageSize = query.pageSize ?? 20;
    const search = query.search?.trim().toLowerCase();

    let items = records;
    if (query.isActive !== undefined) {
      items = items.filter((r) => r.isActive === query.isActive);
    }
    if (search) {
      items = items.filter((r) => fieldValuesAreSearchable(config, r, search));
    }

    const sort = query.sort ?? config.nameField ?? 'updatedAt';
    const direction = sort.startsWith('-') ? -1 : 1;
    const field = sortField(sort);
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

  function getById(id: string): MasterRecord | undefined {
    return records.find((r) => r.id === id);
  }

  function getAll(): MasterRecord[] {
    return records;
  }

  function create(values: Record<string, unknown>): MasterRecord {
    const seq = nextSeq++;
    const now = new Date().toISOString();
    const record: MasterRecord = {
      id: `${config.key}-${String(seq).padStart(3, '0')}`,
      isActive: true,
      createdAt: now,
      updatedAt: now,
      ...values,
    };
    records = [record, ...records];
    persist();
    return record;
  }

  function update(id: string, values: Record<string, unknown>): MasterRecord {
    const existing = getById(id);
    if (!existing) {
      throw new Error(`Mock ${config.label} record ${id} not found.`);
    }
    const updated: MasterRecord = { ...existing, ...values, updatedAt: new Date().toISOString() };
    records = records.map((r) => (r.id === id ? updated : r));
    persist();
    return updated;
  }

  return { list, getById, getAll, create, update };
}

// ---------------------------------------------------------------------------------------
// Combined store: real API first, offline mock fallback on NetworkError only — a real
// ApiError (backend up, request rejected, e.g. validation) must still surface normally.
// ---------------------------------------------------------------------------------------

export function createMasterStore(config: MasterEntityConfig): MasterStore {
  const entityKey = config.key as MastersEntityKey;
  const localStore = createMasterLocalStore(config);

  async function list(query: MasterListQuery): Promise<PagedMasters> {
    try {
      const result = await mastersApi.list(entityKey, query);
      return { items: result.items as MasterRecord[], meta: result.meta, source: 'live' };
    } catch (err) {
      if (err instanceof NetworkError) {
        return { ...localStore.list(query), source: 'mock' };
      }
      throw err;
    }
  }

  async function getById(id: string): Promise<MasterRecord | undefined> {
    try {
      return (await mastersApi.getById(entityKey, id)) as MasterRecord;
    } catch (err) {
      if (err instanceof NetworkError) {
        return localStore.getById(id);
      }
      throw err;
    }
  }

  async function getAll(): Promise<MasterRecord[]> {
    try {
      const result = await mastersApi.list(entityKey, { page: 1, pageSize: ALL_RECORDS_PAGE_SIZE });
      return result.items as MasterRecord[];
    } catch (err) {
      if (err instanceof NetworkError) {
        return localStore.getAll();
      }
      throw err;
    }
  }

  async function create(values: Record<string, unknown>): Promise<MasterRecord> {
    try {
      return (await mastersApi.create(entityKey, values)) as MasterRecord;
    } catch (err) {
      if (err instanceof NetworkError) {
        return localStore.create(values);
      }
      throw err;
    }
  }

  async function update(id: string, values: Record<string, unknown>): Promise<MasterRecord> {
    try {
      return (await mastersApi.update(entityKey, id, values)) as MasterRecord;
    } catch (err) {
      if (err instanceof NetworkError) {
        return localStore.update(id, values);
      }
      throw err;
    }
  }

  return { list, getById, getAll, create, update };
}
