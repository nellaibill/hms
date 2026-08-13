import type { WardListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { wardsApi } from '../../../../services/apiClient';

export const wardsQueryKey = (query: WardListQuery) => ['ipd', 'wards', 'list', query] as const;

export function useWardsQuery(query: WardListQuery) {
  return useQuery({
    queryKey: wardsQueryKey(query),
    queryFn: () => wardsApi.getWards(query),
    placeholderData: (previous) => previous,
  });
}
