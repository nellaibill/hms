import { useQuery } from '@tanstack/react-query';
import { getAllInvoicesForReport } from '../apiBillingRepository';

/** Unpaginated invoice list for Finance & Billing's Income & Expense Report — see
 * apiBillingRepository.ts's getAllInvoicesForReport for the known page-size limitation. */
export function useInvoicesForReportQuery() {
  return useQuery({
    queryKey: ['billings', 'report-all'],
    queryFn: () => getAllInvoicesForReport(),
  });
}
