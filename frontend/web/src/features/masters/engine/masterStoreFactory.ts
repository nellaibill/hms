import type { MasterEntityConfig, MasterListQuery, MasterRecord, PagedMasters } from './types';

/**
 * One localStorage-backed mock store per entity, keyed by config.key — same pattern as
 * mockRolesStore.ts, generalized so all 16 Masters entities share one implementation
 * instead of each hand-rolling create/list/update logic.
 */
export interface MasterStore {
  list(query: MasterListQuery): PagedMasters;
  getById(id: string): MasterRecord | undefined;
  getAll(): MasterRecord[];
  create(values: Record<string, unknown>): MasterRecord;
  update(id: string, values: Record<string, unknown>): MasterRecord;
}

function storageKey(entityKey: string) {
  return `hms-mock-masters-${entityKey}`;
}

function seedRecord(config: MasterEntityConfig, raw: Record<string, unknown> & { id: string }, index: number): MasterRecord {
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
  return config.seed.map((row, index) => seedRecord(config, row, index));
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

export function createMasterStore(config: MasterEntityConfig): MasterStore {
  let records: MasterRecord[] = loadRecords(config);
  let nextSeq = records.reduce((max, r) => {
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

  function list(query: MasterListQuery): PagedMasters {
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
