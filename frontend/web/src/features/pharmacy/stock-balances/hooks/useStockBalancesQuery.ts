import type { StockBalanceListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { pharmacyApi } from '../../../../services/apiClient';

export const stockBalancesQueryKey = (query: StockBalanceListQuery) => ['pharmacy', 'stock-balances', 'list', query] as const;

export function useStockBalancesQuery(query: StockBalanceListQuery) {
  return useQuery({
    queryKey: stockBalancesQueryKey(query),
    queryFn: () => pharmacyApi.getStockBalances(query),
    placeholderData: (previous) => previous,
  });
}
