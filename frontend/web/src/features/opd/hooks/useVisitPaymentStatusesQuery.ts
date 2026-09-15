import { useQueries } from '@tanstack/react-query';
import { getOverallPaymentStatus } from '@/features/billing';
import { getInvoicesByPatientId } from '@/features/billing/apiBillingRepository';

export const OPD_PAYMENT_STATUSES = ['NotBilled', 'Pending', 'Paid'] as const;
export type OpdVisitPaymentStatus = (typeof OPD_PAYMENT_STATUSES)[number];

/**
 * Payment status has no direct link to a specific OPD consultation — Billing only tracks
 * PatientId/VisitId (one invoice can cover several consultations from the same visit, e.g.
 * "Add another Consultant"), so this is computed per VISIT: Paid only once every non-voided
 * invoice for that visit is itself fully Paid, Pending once at least one non-voided invoice
 * exists but isn't, and NotBilled when no invoice has been raised for the visit at all.
 *
 * Composed client-side from the existing GET .../invoices/by-patient/{patientId} endpoint
 * (usePatientInvoicesQuery's own query key/repository function, so the cache is shared) rather
 * than a new batched backend endpoint: Billing's module already depends on Patients' (it
 * validates PatientId/VisitId at invoice-creation time), so the reverse — Patients' own
 * OpdQueryService calling into Billing — would be a circular project reference. One request
 * per unique patientId on the current page is the same accepted N+1-at-OPD-page-volumes
 * tradeoff OpdQueryService's own Department/Consultant lookups already make.
 */
export function useVisitPaymentStatusesQuery(patientIds: string[]) {
  const uniquePatientIds = Array.from(new Set(patientIds));

  const results = useQueries({
    queries: uniquePatientIds.map((patientId) => ({
      queryKey: ['billings', 'by-patient', patientId],
      queryFn: () => getInvoicesByPatientId(patientId),
    })),
  });

  const isLoading = results.some((result) => result.isLoading);

  function getStatus(patientId: string, visitId: string): OpdVisitPaymentStatus {
    const index = uniquePatientIds.indexOf(patientId);
    const invoices = (index >= 0 ? results[index]?.data : undefined) ?? [];
    const visitInvoices = invoices.filter((invoice) => invoice.visitId === visitId && !invoice.isVoided);
    if (visitInvoices.length === 0) return 'NotBilled';
    return visitInvoices.every((invoice) => getOverallPaymentStatus(invoice.items) === 'Paid') ? 'Paid' : 'Pending';
  }

  return { getStatus, isLoading };
}
