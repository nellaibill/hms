import type { MastersEntityKey } from '@hms/shared';
import { mastersApi } from '../../../services/apiClient';
import type { MasterEntityConfig, MasterListQuery, MasterRecord, PagedMasters } from './types';

/**
 * One store per entity, keyed by config.key. Talks to the real /api/v1/masters/* backend.
 * Same interface for every entity, so the generic list/form pages and hooks built on top of
 * it don't need per-entity code.
 */
export interface MasterStore {
  list(query: MasterListQuery): Promise<PagedMasters>;
  getById(id: string): Promise<MasterRecord | undefined>;
  getAll(): Promise<MasterRecord[]>;
  create(values: Record<string, unknown>): Promise<MasterRecord>;
  update(id: string, values: Record<string, unknown>): Promise<MasterRecord>;
}

/** The server clamps pageSize to this (see HMS.Shared.Kernel.PagedRequest.MaxPageSize) — there's no true unpaged endpoint, so "all records" means walking every page at the server's ceiling. */
const SERVER_MAX_PAGE_SIZE = 100;

export function createMasterStore(config: MasterEntityConfig): MasterStore {
  const entityKey = config.key as MastersEntityKey;

  async function list(query: MasterListQuery): Promise<PagedMasters> {
    const result = await mastersApi.list(entityKey, query);
    return { items: result.items as MasterRecord[], meta: result.meta };
  }

  async function getById(id: string): Promise<MasterRecord | undefined> {
    return (await mastersApi.getById(entityKey, id)) as MasterRecord;
  }

  async function getAll(): Promise<MasterRecord[]> {
    const first = await mastersApi.list(entityKey, { page: 1, pageSize: SERVER_MAX_PAGE_SIZE });
    const items = [...first.items] as MasterRecord[];
    for (let page = 2; page <= first.meta.totalPages; page += 1) {
      const next = await mastersApi.list(entityKey, { page, pageSize: SERVER_MAX_PAGE_SIZE });
      items.push(...(next.items as MasterRecord[]));
    }
    return items;
  }

  async function create(values: Record<string, unknown>): Promise<MasterRecord> {
    return (await mastersApi.create(entityKey, values)) as MasterRecord;
  }

  async function update(id: string, values: Record<string, unknown>): Promise<MasterRecord> {
    return (await mastersApi.update(entityKey, id, values)) as MasterRecord;
  }

  return { list, getById, getAll, create, update };
}
