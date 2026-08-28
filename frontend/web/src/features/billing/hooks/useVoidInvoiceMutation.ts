import { useMutation, useQueryClient } from '@tanstack/react-query';
import { voidInvoice } from '../apiBillingRepository';

export function useVoidInvoiceMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ billingId, reason }: { billingId: string; reason: string }) => voidInvoice(billingId, reason),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['billings'] }),
  });
}
