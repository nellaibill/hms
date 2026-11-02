import type { InvoiceDiscountRequestListQuery, InvoiceRevertRequestListQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '@/features/auth/AuthContext';
import { billingApi } from '@/services/apiClient';
import { formatCurrency } from '../billingCalculations';
import { discountRequestsQueryKey, myDiscountApprovalsQueryKey } from './useInvoiceDiscount';
import { revertRequestsQueryKey } from './useInvoiceRevert';

/** How many of each kind the header's Tasks menu lists — the rest are on the approval page. */
const TASKS_PER_KIND = 5;
/** Same check-back as the Notifications bell, so a request raised at the counter turns up. */
const POLL_INTERVAL_MS = 30_000;

const PENDING_REVERTS: InvoiceRevertRequestListQuery = { page: 1, pageSize: TASKS_PER_KIND, status: 'Pending' };
const PENDING_DISCOUNTS: InvoiceDiscountRequestListQuery = { page: 1, pageSize: TASKS_PER_KIND, status: 'Pending' };

/** One request waiting on the signed-in user — `to` is where they decide it. */
export interface ApprovalTask {
  id: string;
  title: string;
  detail: string;
  requestedAt: string;
  to: string;
}

export interface ApprovalTaskGroup {
  key: 'reverts' | 'discounts' | 'myDiscounts';
  label: string;
  /** Every waiting request of this kind, not just the ones listed. */
  total: number;
  tasks: ApprovalTask[];
  /** The full approval queue. */
  viewAllTo: string;
}

function requestedBy(name: string | null | undefined, reason: string): string {
  return `${name ? `Requested by ${name}` : 'Requested'} · ${reason}`;
}

/**
 * The approvals waiting on the signed-in user, for the header's Tasks menu: a Super Admin's OP
 * bill revert requests (ADR-106) and bill discount requests (ADR-109/111), and a consultant
 * login's discounts on their own consultation fee (ADR-111). Oldest first, as on the approval
 * pages. Anyone else has nothing to approve, so every query stays off and the list is empty.
 * Keys sit under 'billings', so approving or rejecting anywhere refreshes the menu.
 */
export function useApprovalTasks() {
  const { user, scopedConsultantId } = useAuth();
  const isSuperAdmin = user?.role === 'superAdmin';

  const reverts = useQuery({
    queryKey: revertRequestsQueryKey(PENDING_REVERTS),
    queryFn: () => billingApi.getRevertRequests(PENDING_REVERTS),
    enabled: isSuperAdmin,
    refetchInterval: POLL_INTERVAL_MS,
    retry: false,
  });
  const discounts = useQuery({
    queryKey: discountRequestsQueryKey(PENDING_DISCOUNTS),
    queryFn: () => billingApi.getDiscountRequests(PENDING_DISCOUNTS),
    enabled: isSuperAdmin,
    refetchInterval: POLL_INTERVAL_MS,
    retry: false,
  });
  const myDiscounts = useQuery({
    queryKey: myDiscountApprovalsQueryKey(PENDING_DISCOUNTS),
    queryFn: () => billingApi.getMyDiscountApprovals(PENDING_DISCOUNTS),
    enabled: Boolean(scopedConsultantId),
    refetchInterval: POLL_INTERVAL_MS,
    retry: false,
  });

  const groups: ApprovalTaskGroup[] = [];

  if (isSuperAdmin && reverts.data) {
    groups.push({
      key: 'reverts',
      label: 'Bill revert requests',
      total: reverts.data.meta.totalCount,
      viewAllTo: '/finance/accounts/revert-requests',
      tasks: reverts.data.items.map((request) => ({
        id: `revert-${request.id}`,
        title: `Revert ${request.invoiceNumber} — ${request.patientName}`,
        detail: requestedBy(request.requestedByName, request.reason),
        requestedAt: request.requestedAt,
        to: `/finance/accounts/${request.invoiceId}`,
      })),
    });
  }

  if (isSuperAdmin && discounts.data) {
    groups.push({
      key: 'discounts',
      label: 'Discount requests',
      total: discounts.data.meta.totalCount,
      viewAllTo: '/finance/accounts/discount-requests',
      tasks: discounts.data.items.map((request) => ({
        id: `discount-${request.id}`,
        title: `Discount ${formatCurrency(request.totalDiscount)} on ${request.invoiceNumber} — ${request.patientName}`,
        detail: requestedBy(request.requestedByName, request.reason),
        requestedAt: request.requestedAt,
        to: `/finance/accounts/${request.invoiceId}`,
      })),
    });
  }

  if (scopedConsultantId && myDiscounts.data) {
    groups.push({
      key: 'myDiscounts',
      label: 'Discounts on your fee',
      total: myDiscounts.data.meta.totalCount,
      viewAllTo: '/clinical/opd?tab=discounts',
      tasks: myDiscounts.data.items.map((request) => {
        // Only this consultant's part — the rest of the bill's discount is the Super Admin's.
        const ownDiscount = request.approvals
          .filter((approval) => approval.consultantId === scopedConsultantId)
          .reduce((sum, approval) => sum + approval.totalDiscount, 0);
        return {
          id: `my-discount-${request.id}`,
          title: `Discount ${formatCurrency(ownDiscount)} on your fee — ${request.patientName}`,
          detail: requestedBy(request.requestedByName, request.reason),
          requestedAt: request.requestedAt,
          // Consultants usually can't open billing pages; their OPD tab has Approve/Reject.
          to: '/clinical/opd?tab=discounts',
        };
      }),
    });
  }

  const total = groups.reduce((sum, group) => sum + group.total, 0);
  return { groups, total };
}
