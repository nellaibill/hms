import type { CreateDispenseCartRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { pharmacyApi } from '../../../../services/apiClient';

export function useCreateDispenseCartMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateDispenseCartRequest) => pharmacyApi.createDispenseCart(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pharmacy', 'dispenses'] });
      // A checkout decreases every line's product/batch on-hand balance, so balances and the
      // combined ledger go stale at the same time — mirrors useCreateDispenseMutation's own fan-out.
      queryClient.invalidateQueries({ queryKey: ['pharmacy', 'stock-balances'] });
      queryClient.invalidateQueries({ queryKey: ['pharmacy', 'stock-ledger'] });
    },
  });
}
