import { describeReportService, type BillingType, type CollectionPayment } from '@/features/billing';
import { toLocalDateInputValue } from '@/lib/reportDateRange';
import { resolveLineCost, roundToPaise } from './profitReport';

/** One payment, split into the consultant's and the hospital's share (ADR-125). */
export interface CollectionReportRow {
  id: string;
  /** The local calendar day the money was taken (YYYY-MM-DD). */
  date: string;
  paidAt: string;
  amount: number;
  method: string;
  invoiceId: string;
  invoiceNumber?: string;
  patientId: string;
  patientName: string;
  billingType: BillingType;
  serviceLabel: string;
  departmentId?: string;
  billedConsultantId?: string;
  /** The consultant's part of this payment — the line's cost (what OP Report calls Consultant
   * Cost) in proportion to how much of the line this payment covered. Null when the line's cost
   * isn't known (an uncosted service, a package, Pharmacy). */
  consultantAmount: number | null;
  /** amount − consultantAmount; null whenever consultantAmount is. */
  hospitalAmount: number | null;
}

/** The Collection Summary's day-wise columns; every other billing type falls under "Other". */
export const COLLECTION_HEADS = ['Consultation', 'Laboratory', 'Radiology', 'Procedure'] as const;
export type CollectionHead = (typeof COLLECTION_HEADS)[number] | 'Other';

export function collectionHeadOf(billingType: BillingType): CollectionHead {
  return (COLLECTION_HEADS as readonly string[]).includes(billingType) ? (billingType as CollectionHead) : 'Other';
}

/** Collection Summary covers OP visits only, like OP Report (ADR-121/125). A payment whose visit
 * type is unknown is left out too. */
export function onlyOpVisitPayments(payments: CollectionPayment[]): CollectionPayment[] {
  return payments.filter((payment) => payment.encounterType === 'OP');
}

/** Splits every payment by the same rule OP Report splits a line (resolveLineCost): the line's
 * cost is the consultant's, the rest the hospital's. A part-payment takes the same share of
 * each, so the consultant's and hospital's amounts always add up to what was collected. */
export function getCollectionRows(payments: CollectionPayment[]): CollectionReportRow[] {
  return payments.map((payment) => {
    const { item } = payment;
    const { cost } = resolveLineCost(item);
    // A ₹0 line can still carry a cost (e.g. a fully discounted consultation); with nothing
    // collected there is nothing to split, so the consultant's part is 0, not a division by 0.
    const consultantAmount = cost === null ? null : item.total > 0 ? roundToPaise((payment.amount * cost) / item.total) : 0;
    return {
      id: payment.paymentId,
      date: toLocalDateInputValue(new Date(payment.paidAt)),
      paidAt: payment.paidAt,
      amount: payment.amount,
      method: payment.method,
      invoiceId: payment.invoiceId,
      invoiceNumber: payment.invoiceNumber,
      patientId: payment.patientId,
      patientName: payment.patientName,
      billingType: item.billingType as BillingType,
      serviceLabel: describeReportService(item),
      departmentId: item.departmentId,
      billedConsultantId: item.billedConsultantId,
      consultantAmount,
      hospitalAmount: consultantAmount === null ? null : roundToPaise(payment.amount - consultantAmount),
    };
  });
}

/** Totals for a set of collection rows. Consultant and hospital amounts only cover rows whose
 * cost is known; `collectedWithUnknownCost` is the rest, so the gap is shown, not hidden. */
export interface CollectionTotals {
  collected: number;
  consultantAmount: number;
  hospitalAmount: number;
  collectedWithUnknownCost: number;
  payments: number;
}

export function sumCollections(rows: CollectionReportRow[]): CollectionTotals {
  const totals: CollectionTotals = { collected: 0, consultantAmount: 0, hospitalAmount: 0, collectedWithUnknownCost: 0, payments: rows.length };
  for (const row of rows) {
    totals.collected += row.amount;
    if (row.consultantAmount === null || row.hospitalAmount === null) totals.collectedWithUnknownCost += row.amount;
    else {
      totals.consultantAmount += row.consultantAmount;
      totals.hospitalAmount += row.hospitalAmount;
    }
  }
  totals.collected = roundToPaise(totals.collected);
  totals.consultantAmount = roundToPaise(totals.consultantAmount);
  totals.hospitalAmount = roundToPaise(totals.hospitalAmount);
  totals.collectedWithUnknownCost = roundToPaise(totals.collectedWithUnknownCost);
  return totals;
}

/** Money taken per payment method (Cash, Upi, Card, BankTransfer), largest first. */
export function getCollectionsByMethod(rows: CollectionReportRow[]): { method: string; amount: number }[] {
  const byMethod = new Map<string, number>();
  for (const row of rows) byMethod.set(row.method, (byMethod.get(row.method) ?? 0) + row.amount);
  return [...byMethod].map(([method, amount]) => ({ method, amount: roundToPaise(amount) })).sort((a, b) => b.amount - a.amount);
}

/** Section 1 — one row per day with money taken, oldest first: the amount per head, the
 * consultant's and hospital's parts of it, and the total. Like CollectionTotals, the two parts
 * cover only payments whose cost is known, so they fall short of total when one isn't. */
export interface DailyCollectionRow {
  date: string;
  byHead: Record<CollectionHead, number>;
  consultantAmount: number;
  hospitalAmount: number;
  total: number;
}

function emptyHeads(): Record<CollectionHead, number> {
  return { Consultation: 0, Laboratory: 0, Radiology: 0, Procedure: 0, Other: 0 };
}

export function getDailyCollections(rows: CollectionReportRow[]): DailyCollectionRow[] {
  const byDate = new Map<string, DailyCollectionRow>();
  for (const row of rows) {
    const day = byDate.get(row.date) ?? { date: row.date, byHead: emptyHeads(), consultantAmount: 0, hospitalAmount: 0, total: 0 };
    day.byHead[collectionHeadOf(row.billingType)] += row.amount;
    if (row.consultantAmount !== null && row.hospitalAmount !== null) {
      day.consultantAmount += row.consultantAmount;
      day.hospitalAmount += row.hospitalAmount;
    }
    day.total += row.amount;
    byDate.set(row.date, day);
  }
  return [...byDate.values()]
    .map((day) => ({
      date: day.date,
      consultantAmount: roundToPaise(day.consultantAmount),
      hospitalAmount: roundToPaise(day.hospitalAmount),
      total: roundToPaise(day.total),
      byHead: Object.fromEntries(Object.entries(day.byHead).map(([head, amount]) => [head, roundToPaise(amount)])) as Record<CollectionHead, number>,
    }))
    .sort((a, b) => a.date.localeCompare(b.date));
}

/** The day-wise table's totals row. */
export function sumDailyCollections(days: DailyCollectionRow[]): Omit<DailyCollectionRow, 'date'> {
  const byHead = emptyHeads();
  let consultantAmount = 0;
  let hospitalAmount = 0;
  let total = 0;
  for (const day of days) {
    for (const head of Object.keys(byHead) as CollectionHead[]) byHead[head] += day.byHead[head];
    consultantAmount += day.consultantAmount;
    hospitalAmount += day.hospitalAmount;
    total += day.total;
  }
  for (const head of Object.keys(byHead) as CollectionHead[]) byHead[head] = roundToPaise(byHead[head]);
  return { byHead, consultantAmount: roundToPaise(consultantAmount), hospitalAmount: roundToPaise(hospitalAmount), total: roundToPaise(total) };
}

/** Section 2 (and each head in section 3) — one row per consultant. `consultantId` is undefined
 * for lines billed without one (Injection, File). */
export interface ConsultantCollectionRow extends CollectionTotals {
  consultantId?: string;
}

/** Rows grouped by billed consultant, largest collection first; the no-consultant row last. */
export function getConsultantCollections(rows: CollectionReportRow[]): ConsultantCollectionRow[] {
  const byConsultant = new Map<string, CollectionReportRow[]>();
  for (const row of rows) {
    const key = row.billedConsultantId ?? '';
    byConsultant.set(key, [...(byConsultant.get(key) ?? []), row]);
  }
  return [...byConsultant]
    .map(([key, group]) => ({ consultantId: key || undefined, ...sumCollections(group) }))
    .sort((a, b) => (a.consultantId ? 0 : 1) - (b.consultantId ? 0 : 1) || b.collected - a.collected);
}

/** Section 3 — one block per billing type (Consultation, Laboratory, Radiology, Procedure,
 * Injection, File, …) in that order, each with its consultants and a subtotal. */
export interface HeadCollectionSection {
  billingType: BillingType;
  consultants: ConsultantCollectionRow[];
  subtotal: CollectionTotals;
}

// Server-generated lines (Pharmacy, InpatientCharge) aren't in the web BillingType union, but a
// payment can still be for one, so the order is by name.
const HEAD_ORDER: string[] = ['Consultation', 'Laboratory', 'Radiology', 'Procedure', 'Injection', 'File', 'Pharmacy', 'InpatientCharge'];

export function getHeadCollections(rows: CollectionReportRow[]): HeadCollectionSection[] {
  const others = rows.filter((row) => !HEAD_ORDER.includes(row.billingType));
  return [...HEAD_ORDER.map((billingType) => rows.filter((row) => row.billingType === billingType)), others]
    .filter((group) => group.length > 0)
    .map((group) => ({ billingType: group[0].billingType, consultants: getConsultantCollections(group), subtotal: sumCollections(group) }));
}
