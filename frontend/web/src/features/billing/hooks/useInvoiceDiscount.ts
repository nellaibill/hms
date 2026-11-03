import type { CreateInvoicePaymentRequest, InvoiceDiscountLineRequest, InvoiceDiscountRequestListQuery, InvoiceDiscountType } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { billingApi } from '@/services/apiClient';
import { payInvoice, requestInvoiceDiscount } from '../apiBillingRepository';

/** Bill discount requests (ADR-109, ADR-111). Every key sits under 'billings', so the invoice
 * queries and these refresh together once a discount is requested or a part of it decided. */
export const invoiceDiscountStatusQueryKey = (invoiceId: string) => ['billings', 'discount-status', invoiceId] as const;
export const discountRequestsQueryKey = (query: InvoiceDiscountRequestListQuery) => ['billings', 'discount-requests', query] as const;
export const pendingDiscountCountQueryKey = ['billings', 'discount-requests', 'pending-count'] as const;
export const myDiscountApprovalsQueryKey = (query: InvoiceDiscountRequestListQuery) => ['billings', 'my-discount-approvals', query] as const;
export const myPendingDiscountApprovalCountQueryKey = ['billings', 'my-discount-approvals', 'pending-count'] as const;
export const discountReasonSuggestionsQueryKey = ['billings', 'discount-reasons'] as const;

export function useInvoiceDiscountStatusQuery(invoiceId: string | undefined) {
  return useQuery({
    queryKey: invoiceDiscountStatusQueryKey(invoiceId ?? ''),
    queryFn: () => billingApi.getInvoiceDiscountStatus(invoiceId as string),
    enabled: Boolean(invoiceId),
    // Same 15 s check-back as useBillingQuery while the request waits on its approvers.
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

/** Reasons this hospital has given for discounts at least twice, most-used first (the server
 * leaves out one-offs and placeholders like "Test") — the Reason box's suggestions. Under
 * 'billings', so a reason joins them once it's been used again. Without access
 * (or offline) it just stays empty and the built-in suggestions are used. */
export function useDiscountReasonSuggestionsQuery() {
  return useQuery({
    queryKey: discountReasonSuggestionsQueryKey,
    queryFn: () => billingApi.getDiscountReasonSuggestions(20),
    staleTime: 5 * 60_000,
    retry: false,
  });
}

/** A consultant login's own discount approvals (ADR-111) — checks back every 15 s while showing
 * the waiting ones, so a request raised at the counter turns up without a reload. */
export function useMyDiscountApprovalsQuery(query: InvoiceDiscountRequestListQuery, enabled = true) {
  return useQuery({
    queryKey: myDiscountApprovalsQueryKey(query),
    queryFn: () => billingApi.getMyDiscountApprovals(query),
    enabled,
    placeholderData: (previous) => previous,
    refetchInterval: query.status === 'Pending' ? 15_000 : false,
  });
}

/** The consultant's tab badge — same 15 s check-back. */
export function useMyPendingDiscountApprovalCountQuery(enabled = true) {
  return useQuery({
    queryKey: myPendingDiscountApprovalCountQueryKey,
    queryFn: () => billingApi.getMyPendingDiscountApprovalCount(),
    enabled,
    refetchInterval: 15_000,
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

/** Decides one approval part of a discount request — `approvalId`, not the request's id. */
export function useApproveDiscountMutation() {
  const invalidate = useInvalidateBillings();
  return useMutation({
    mutationFn: ({ approvalId, notes }: { approvalId: string; notes?: string }) => billingApi.approveDiscountApproval(approvalId, { notes: notes || null }),
    onSuccess: invalidate,
  });
}

export function useRejectDiscountMutation() {
  const invalidate = useInvalidateBillings();
  return useMutation({
    mutationFn: ({ approvalId, notes }: { approvalId: string; notes: string }) => billingApi.rejectDiscountApproval(approvalId, { notes }),
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
