import { useQuery } from '@tanstack/react-query';
import { pharmacyApi } from '../../../../services/apiClient';

// Keyed under 'stock-balances' so the receipt/dispense mutations' existing invalidation of
// that prefix refreshes what can be dispensed without either having to know about this query.
export const availableBatchesQueryKey = (productId: string | undefined) =>
  ['pharmacy', 'stock-balances', 'available-batches', productId] as const;

/** The batches of a product that can be dispensed right now, earliest expiry first — only
 * enabled once a product has been chosen. */
export function useAvailableBatchesQuery(productId: string | undefined) {
  return useQuery({
    queryKey: availableBatchesQueryKey(productId),
    queryFn: () => pharmacyApi.getAvailableBatches(productId as string),
    enabled: Boolean(productId),
  });
}
