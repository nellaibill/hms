import { useMutation, useQueryClient } from '@tanstack/react-query';
import { recordMockPayment } from '../mockBillingStore';

export function useRecordPaymentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ billingId, itemId }: { billingId: string; itemId: string }) => recordMockPayment(billingId, itemId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['billings'] }),
  });
}
