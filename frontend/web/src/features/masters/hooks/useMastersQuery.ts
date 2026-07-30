import { useQuery } from '@tanstack/react-query';
import { getMasterStore } from '../engine/registry';
import type { MasterListQuery, PagedMasters } from '../engine/types';

export const mastersQueryKey = (entityKey: string, query: MasterListQuery) => ['masters', entityKey, 'list', query] as const;

export function useMastersQuery(entityKey: string, query: MasterListQuery) {
  return useQuery<PagedMasters>({
    queryKey: mastersQueryKey(entityKey, query),
    queryFn: () => {
      const store = getMasterStore(entityKey);
      if (!store) throw new Error(`Unknown Masters entity "${entityKey}".`);
      return store.list(query);
    },
    placeholderData: (previous) => previous,
  });
}
