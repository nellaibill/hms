import { useMutation, useQueryClient } from '@tanstack/react-query';
import { recordPayment } from '../apiBillingRepository';
import type { PaymentMethod } from '../types';

export function useRecordPaymentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ billingId, itemId, method }: { billingId: string; itemId: string; method: PaymentMethod }) =>
      recordPayment(billingId, itemId, method),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['billings'] }),
  });
}
