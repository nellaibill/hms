import { useQuery } from '@tanstack/react-query';
import { getMasterStore } from '../engine/registry';

export function useMasterQuery(entityKey: string, id: string | undefined) {
  return useQuery({
    queryKey: ['masters', entityKey, 'detail', id],
    queryFn: () => {
      const store = getMasterStore(entityKey);
      const record = store?.getById(id as string);
      if (!record) {
        throw new Error('Record not found.');
      }
      return record;
    },
    enabled: Boolean(id),
  });
}

/** All records for an entity, unpaged — used to populate reference-field dropdowns. */
export function useMasterOptionsQuery(entityKey: string | undefined) {
  return useQuery({
    queryKey: ['masters', entityKey, 'options'],
    queryFn: () => getMasterStore(entityKey)?.getAll() ?? [],
    enabled: Boolean(entityKey),
  });
}
