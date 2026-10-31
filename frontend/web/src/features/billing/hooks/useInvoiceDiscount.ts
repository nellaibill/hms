import type { CreateInvoicePaymentRequest, InvoiceDiscountLineRequest, InvoiceDiscountRequestListQuery, InvoiceDiscountType } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { billingApi } from '@/services/apiClient';
import { payInvoice, requestInvoiceDiscount } from '../apiBillingRepository';

/** Bill discount requests (ADR-109). Every key sits under 'billings', so the invoice queries and
 * these refresh together once a discount is requested or decided. */
export const invoiceDiscountStatusQueryKey = (invoiceId: string) => ['billings', 'discount-status', invoiceId] as const;
export const discountRequestsQueryKey = (query: InvoiceDiscountRequestListQuery) => ['billings', 'discount-requests', query] as const;
export const pendingDiscountCountQueryKey = ['billings', 'discount-requests', 'pending-count'] as const;

export function useInvoiceDiscountStatusQuery(invoiceId: string | undefined) {
  return useQuery({
    queryKey: invoiceDiscountStatusQueryKey(invoiceId ?? ''),
    queryFn: () => billingApi.getInvoiceDiscountStatus(invoiceId as string),
    enabled: Boolean(invoiceId),
    // Same 15 s check-back as useBillingQuery while the request waits on a Super Admin.
    refetchInterval: (query) => (query.state.data?.requests[0]?.status === 'Pending' ? 15_000 : false),
  });
}

export function useDiscountRequestsQuery(query: InvoiceDiscountRequestListQuery) {
  return useQuery({
    queryKey: discountRequestsQueryKey(query),
    queryFn: () => billingApi.getDiscountRequests(query),
    placeholderData: (previous) => previous,
  });
}

export function usePendingDiscountCountQuery(enabled = true) {
  return useQuery({
    queryKey: pendingDiscountCountQueryKey,
    queryFn: () => billingApi.getPendingDiscountRequestCount(),
    enabled,
  });
}

function useInvalidateBillings() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['billings'] });
}

export function useRequestInvoiceDiscountMutation() {
  const invalidate = useInvalidateBillings();
  return useMutation({
    mutationFn: ({ invoiceId, ...request }: { invoiceId: string; type: InvoiceDiscountType; value: number; reason: string; lines: InvoiceDiscountLineRequest[] }) =>
      requestInvoiceDiscount(invoiceId, request),
    onSuccess: invalidate,
  });
}

export function useApproveDiscountRequestMutation() {
  const invalidate = useInvalidateBillings();
  return useMutation({
    mutationFn: ({ id, notes }: { id: string; notes?: string }) => billingApi.approveDiscountRequest(id, { notes: notes || null }),
    onSuccess: invalidate,
  });
}

export function useRejectDiscountRequestMutation() {
  const invalidate = useInvalidateBillings();
  return useMutation({
    mutationFn: ({ id, notes }: { id: string; notes: string }) => billingApi.rejectDiscountRequest(id, { notes }),
    onSuccess: invalidate,
  });
}

/** Collects everything still owed on a bill in one go. */
export function usePayInvoiceMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ billingId, payments }: { billingId: string; payments: CreateInvoicePaymentRequest[] }) => payInvoice(billingId, payments),
    // Same as useRecordPaymentMutation: paying a consultation can give the visit its OP No.
    onSuccess: () =>
      Promise.all([queryClient.invalidateQueries({ queryKey: ['billings'] }), queryClient.invalidateQueries({ queryKey: ['patient-visits'] })]),
  });
}
