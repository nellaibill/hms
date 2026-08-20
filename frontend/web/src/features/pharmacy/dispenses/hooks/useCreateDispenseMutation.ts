import type { CreateDispenseRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { pharmacyApi } from '../../../../services/apiClient';

export function useCreateDispenseMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateDispenseRequest) => pharmacyApi.createDispense(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pharmacy', 'dispenses'] });
      // A dispense decreases the product/batch's on-hand balance, so balances and the
      // combined ledger go stale at the same time — mirrors useCreateStockReceiptMutation's
      // own fan-out.
      queryClient.invalidateQueries({ queryKey: ['pharmacy', 'stock-balances'] });
      queryClient.invalidateQueries({ queryKey: ['pharmacy', 'stock-ledger'] });
    },
  });
}
