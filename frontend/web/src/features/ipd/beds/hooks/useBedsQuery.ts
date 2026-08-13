import type { BedListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { bedsApi } from '../../../../services/apiClient';

export const bedsQueryKey = (query: BedListQuery) => ['ipd', 'beds', 'list', query] as const;

export function useBedsQuery(query: BedListQuery) {
  return useQuery({
    queryKey: bedsQueryKey(query),
    queryFn: () => bedsApi.getBeds(query),
    placeholderData: (previous) => previous,
  });
}
