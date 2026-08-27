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

/** These lookup lists are small enough that a single large page approximates "all records" — there's no true unpaged endpoint. */
const ALL_RECORDS_PAGE_SIZE = 1000;

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
    const result = await mastersApi.list(entityKey, { page: 1, pageSize: ALL_RECORDS_PAGE_SIZE });
    return result.items as MasterRecord[];
  }

  async function create(values: Record<string, unknown>): Promise<MasterRecord> {
    return (await mastersApi.create(entityKey, values)) as MasterRecord;
  }

  async function update(id: string, values: Record<string, unknown>): Promise<MasterRecord> {
    return (await mastersApi.update(entityKey, id, values)) as MasterRecord;
  }

  return { list, getById, getAll, create, update };
}
