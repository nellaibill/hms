import type { PatientVisit } from '@hms/shared';
import { toLocalDateInputValue } from '@/lib/reportDateRange';
import { getOverallPaymentStatus } from './billingCalculations';
import type { Billing } from './types';

/** Where a visit's billing stands: no live bill yet, every live bill paid, or something still
 * owed — 'OnHold' when that bill's discount is waiting for approval (ADR-109). */
export type VisitBillingStatus = 'NotBilled' | 'Pending' | 'OnHold' | 'Paid';

export function visitBillingStatus(invoices: Billing[], visitId: string): VisitBillingStatus {
  const visitInvoices = invoices.filter((invoice) => invoice.visitId === visitId && !invoice.isVoided);
  if (visitInvoices.length === 0) return 'NotBilled';
  const unpaid = visitInvoices.filter((invoice) => getOverallPaymentStatus(invoice.items) !== 'Paid');
  if (unpaid.length === 0) return 'Paid';
  return unpaid.some((invoice) => invoice.discountStatus === 'Pending') ? 'OnHold' : 'Pending';
}

/** The local calendar day a visit was recorded on (YYYY-MM-DD), to compare with today's. */
export function visitDay(visit: Pick<PatientVisit, 'createdAt'>): string {
  return toLocalDateInputValue(new Date(visit.createdAt));
}

export interface BillingVisitChoice {
  /** The visit the bill will be saved against; undefined until one is chosen or when the
   * patient has no visit. */
  visit: PatientVisit | undefined;
  /** More than one of today's visits isn't paid yet, so the counter must say which one it's
   * billing — guessing put one doctor's fee on another's visit (ADR-117). */
  needsChoice: boolean;
}

/**
 * Which visit OPD Billing Entry bills (ADR-117): the one asked for (picked on the page, or
 * ?visitId= from the OPD "Payment required" pop-up); otherwise today's only unpaid visit; when
 * today has more than one unpaid visit, none until the counter picks one; otherwise the newest
 * visit. `visits` is newest first, as the API returns them.
 */
export function chooseBillingVisit(
  visits: PatientVisit[],
  invoices: Billing[],
  requestedVisitId: string | null,
  today: string,
): BillingVisitChoice {
  const requested = requestedVisitId ? visits.find((visit) => visit.visitId === requestedVisitId) : undefined;
  if (requested) return { visit: requested, needsChoice: false };

  const unpaidToday = visits.filter((visit) => visitDay(visit) === today && visitBillingStatus(invoices, visit.visitId) !== 'Paid');
  if (unpaidToday.length === 1) return { visit: unpaidToday[0], needsChoice: false };
  if (unpaidToday.length > 1) return { visit: undefined, needsChoice: true };
  return { visit: visits[0], needsChoice: false };
}
