import type { DispenseListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { pharmacyApi } from '../../../../services/apiClient';

export const dispensesQueryKey = (query: DispenseListQuery) => ['pharmacy', 'dispenses', 'list', query] as const;

export function useDispensesQuery(query: DispenseListQuery) {
  return useQuery({
    queryKey: dispensesQueryKey(query),
    queryFn: () => pharmacyApi.getDispenses(query),
    placeholderData: (previous) => previous,
  });
}
