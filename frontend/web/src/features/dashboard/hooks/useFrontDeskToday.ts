import { keepPreviousData, useQuery } from '@tanstack/react-query';
import type { InvoiceResponse } from '@hms/shared';
import { toRangeEnd } from '@/lib/reportDateRange';
import { billingApi, opdApi, patientsApi } from '@/services/apiClient';
import type { TodayRange } from '../frontDeskToday';

/** The front desk's figures keep themselves current without a page reload. */
const REFRESH_MS = 60_000;

const live = {
  refetchInterval: REFRESH_MS,
  refetchOnWindowFocus: true,
  placeholderData: keepPreviousData,
};

/** Today's OP visits per consultant: waiting, with the doctor, done (ADR-121: OP visits only). */
export function useOpdTodayQuery(range: TodayRange, enabled: boolean) {
  return useQuery({
    queryKey: ['front-desk', 'opd-summary', range.date],
    // OPD's To is inclusive, so the day ends at 23:59:59.999 rather than the next midnight.
    queryFn: () => opdApi.getConsultationSummary({ from: range.from, to: toRangeEnd(range.date) }),
    enabled,
    ...live,
  });
}

/** Patients first registered today (registration date alone, ADR-129). */
export function useRegistrationsTodayQuery(range: TodayRange, enabled: boolean) {
  return useQuery({
    queryKey: ['front-desk', 'registrations', range.date],
    queryFn: async () =>
      (
        await patientsApi.getPatients({
          registeredFrom: range.from,
          registeredTo: range.to,
          pageSize: 1,
        })
      ).meta.totalCount,
    enabled,
    ...live,
  });
}

/** Money taken today, every encounter type, voided bills left out (ADR-125's collections API). */
export function useCollectionsTodayQuery(range: TodayRange, enabled: boolean) {
  return useQuery({
    queryKey: ['front-desk', 'collections', range.date],
    queryFn: () => billingApi.getCollections(range.from, range.to),
    enabled,
    ...live,
  });
}

const BILLS_PAGE_SIZE = 100;
/** A runaway guard: 10 pages is 1,000 bills in a day. */
const MAX_BILL_PAGES = 10;

/** Bills raised today. The list comes newest first, so paging stops at the first bill from before today. */
export function useBillsTodayQuery(range: TodayRange, enabled: boolean) {
  return useQuery({
    queryKey: ['front-desk', 'bills', range.date],
    queryFn: async () => {
      const from = Date.parse(range.from);
      const to = Date.parse(range.to);
      const bills: InvoiceResponse[] = [];
      for (let page = 1; page <= MAX_BILL_PAGES; page += 1) {
        const result = await billingApi.getInvoices({ page, pageSize: BILLS_PAGE_SIZE });
        const createdAt = (bill: InvoiceResponse) => Date.parse(bill.createdAt);
        bills.push(
          ...result.items.filter((bill) => createdAt(bill) >= from && createdAt(bill) < to),
        );
        const reachedYesterday = result.items.some((bill) => createdAt(bill) < from);
        if (reachedYesterday || page >= result.meta.totalPages) break;
      }
      return bills;
    },
    enabled,
    ...live,
  });
}

/** Requests waiting for a Super Admin's or consultant's decision, across every day. */
export function usePendingApprovalsQuery(enabled: boolean) {
  return useQuery({
    queryKey: ['front-desk', 'pending-approvals'],
    queryFn: async () => {
      const [discounts, reverts] = await Promise.all([
        billingApi.getPendingDiscountRequestCount(),
        billingApi.getPendingRevertRequestCount(),
      ]);
      return { discounts, reverts };
    },
    enabled,
    ...live,
  });
}
