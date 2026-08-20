import type { CreateStockReceiptRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { pharmacyApi } from '../../../../services/apiClient';

export function useCreateStockReceiptMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateStockReceiptRequest) => pharmacyApi.createStockReceipt(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pharmacy', 'stock-receipts'] });
      // A receipt increases the product/batch's on-hand balance, so balances and the
      // combined ledger go stale at the same time — mirrors IPD's own invalidation fan-out
      // (e.g. admit/transfer/discharge also invalidating beds/dashboard alongside admissions).
      queryClient.invalidateQueries({ queryKey: ['pharmacy', 'stock-balances'] });
      queryClient.invalidateQueries({ queryKey: ['pharmacy', 'stock-ledger'] });
    },
  });
}
