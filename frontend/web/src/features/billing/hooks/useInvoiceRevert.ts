import type { InvoiceRevertRequestListQuery } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { billingApi } from '@/services/apiClient';

/** OP bill revert requests (ADR-106). Every key sits under 'billings', so the invoice queries
 * and these refresh together once a revert is requested or decided. */
export const invoiceRevertStatusQueryKey = (invoiceId: string) => ['billings', 'revert-status', invoiceId] as const;
export const revertRequestsQueryKey = (query: InvoiceRevertRequestListQuery) => ['billings', 'revert-requests', query] as const;
export const pendingRevertCountQueryKey = ['billings', 'revert-requests', 'pending-count'] as const;

export function useInvoiceRevertStatusQuery(invoiceId: string | undefined) {
  return useQuery({
    queryKey: invoiceRevertStatusQueryKey(invoiceId ?? ''),
    queryFn: () => billingApi.getInvoiceRevertStatus(invoiceId as string),
    enabled: Boolean(invoiceId),
  });
}

export function useRevertRequestsQuery(query: InvoiceRevertRequestListQuery) {
  return useQuery({
    queryKey: revertRequestsQueryKey(query),
    queryFn: () => billingApi.getRevertRequests(query),
    placeholderData: (previous) => previous,
  });
}

export function usePendingRevertCountQuery(enabled = true) {
  return useQuery({
    queryKey: pendingRevertCountQueryKey,
    queryFn: () => billingApi.getPendingRevertRequestCount(),
    enabled,
  });
}

function useInvalidateBillings() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['billings'] });
}

export function useRequestInvoiceRevertMutation() {
  const invalidate = useInvalidateBillings();
  return useMutation({
    mutationFn: ({ invoiceId, reason }: { invoiceId: string; reason: string }) => billingApi.requestInvoiceRevert(invoiceId, { reason }),
    onSuccess: invalidate,
  });
}

export function useApproveRevertRequestMutation() {
  const invalidate = useInvalidateBillings();
  return useMutation({
    mutationFn: ({ id, notes }: { id: string; notes?: string }) => billingApi.approveRevertRequest(id, { notes: notes || null }),
    onSuccess: invalidate,
  });
}

export function useRejectRevertRequestMutation() {
  const invalidate = useInvalidateBillings();
  return useMutation({
    mutationFn: ({ id, notes }: { id: string; notes: string }) => billingApi.rejectRevertRequest(id, { notes }),
    onSuccess: invalidate,
  });
}
