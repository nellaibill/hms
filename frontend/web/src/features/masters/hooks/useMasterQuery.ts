import { useQueries, useQuery } from '@tanstack/react-query';
import { getMasterStore, primeReferenceCache } from '../engine/registry';
import type { MasterEntityConfig } from '../engine/types';

export function useMasterQuery(entityKey: string, id: string | undefined) {
  return useQuery({
    queryKey: ['masters', entityKey, 'detail', id],
    queryFn: async () => {
      const store = getMasterStore(entityKey);
      const record = await store?.getById(id as string);
      if (!record) {
        throw new Error('Record not found.');
      }
      return record;
    },
    enabled: Boolean(id),
  });
}

/**
 * All records for an entity, unpaged — used to populate reference-field dropdowns, and
 * primes the registry's reference cache (see engine/registry.ts) so resolveRecordLabel can
 * resolve ids to labels synchronously during render once this query settles.
 */
export function useMasterOptionsQuery(entityKey: string | undefined) {
  return useQuery({
    queryKey: ['masters', entityKey, 'options'],
    queryFn: async () => {
      const records = (await getMasterStore(entityKey)?.getAll()) ?? [];
      primeReferenceCache(entityKey as string, records);
      return records;
    },
    enabled: Boolean(entityKey),
  });
}

/**
 * Primes the reference cache for every entity a config's reference-type fields point at —
 * used by MasterTable (list page) so reference columns resolve to labels instead of raw ids.
 * Uses useQueries (not a loop of useMasterOptionsQuery) since the set of referenced entity
 * keys varies by config, and hooks can't be called a variable number of times.
 */
export function useMasterReferenceOptions(config: MasterEntityConfig | undefined): void {
  const referencedKeys = Array.from(
    new Set((config?.fields ?? []).filter((field) => field.type === 'reference').map((field) => field.referenceEntityKey as string)),
  );

  useQueries({
    queries: referencedKeys.map((entityKey) => ({
      queryKey: ['masters', entityKey, 'options'],
      queryFn: async () => {
        const records = (await getMasterStore(entityKey)?.getAll()) ?? [];
        primeReferenceCache(entityKey, records);
        return records;
      },
    })),
  });
}
