import type {
  CollectionPaymentResponse,
  InvoiceResponse,
  OpdConsultationSummaryItem,
  PaymentMethod,
} from '@hms/shared';
import { toLocalDateInputValue } from '@/lib/reportDateRange';

/**
 * The receptionist's "Today" dashboard figures (ADR-129), worked out in the browser from the
 * lists the front desk already reads. Kept apart from the components so the rules for what
 * counts as unpaid or on hold live in one place.
 */

/** Today's local calendar day, and the instants that bound it: local midnight to the next. */
export interface TodayRange {
  /** YYYY-MM-DD in the browser's (the hospital's) time zone. */
  date: string;
  from: string;
  to: string;
}

export function getTodayRange(now: Date = new Date()): TodayRange {
  const date = toLocalDateInputValue(now);
  const start = new Date(`${date}T00:00:00`);
  const end = new Date(start);
  end.setDate(end.getDate() + 1);
  return { date, from: start.toISOString(), to: end.toISOString() };
}

export interface OpdTodayTotals {
  total: number;
  waiting: number;
  inConsultation: number;
  completed: number;
}

export function sumOpdSummary(rows: OpdConsultationSummaryItem[]): OpdTodayTotals {
  return rows.reduce<OpdTodayTotals>(
    (totals, row) => ({
      total: totals.total + row.totalPatients,
      waiting: totals.waiting + row.waiting,
      inConsultation: totals.inConsultation + row.inConsultation,
      completed: totals.completed + row.completed,
    }),
    { total: 0, waiting: 0, inConsultation: 0, completed: 0 },
  );
}

export interface CollectionsToday {
  total: number;
  payments: number;
  /** Largest first; only methods that took money today. */
  byMethod: { method: PaymentMethod; amount: number }[];
}

export function sumCollectionsToday(payments: CollectionPaymentResponse[]): CollectionsToday {
  const byMethod = new Map<PaymentMethod, number>();
  let total = 0;
  for (const payment of payments) {
    total += payment.amount;
    byMethod.set(payment.method, (byMethod.get(payment.method) ?? 0) + payment.amount);
  }
  return {
    total,
    payments: payments.length,
    byMethod: [...byMethod]
      .map(([method, amount]) => ({ method, amount }))
      .sort((a, b) => b.amount - a.amount),
  };
}

/** How a bill reads at the desk — the same order RecentPatientBillsTable shows it in. */
export type FrontDeskBillState = 'Voided' | 'Reverted' | 'OnHold' | 'Pending' | 'Paid';

export function frontDeskBillState(bill: InvoiceResponse): FrontDeskBillState {
  if (bill.isVoided) return bill.isReverted ? 'Reverted' : 'Voided';
  if (bill.discountStatus === 'Pending') return 'OnHold';
  return bill.paymentStatus === 'Paid' ? 'Paid' : 'Pending';
}

/** What's still to be collected on a bill: its unpaid lines. A part-paid bill owes less than its net amount. */
export function amountDue(bill: InvoiceResponse): number {
  return bill.items
    .filter((item) => item.paymentStatus === 'Pending')
    .reduce((sum, item) => sum + item.total, 0);
}

export interface BillsToday {
  /** Bills raised today, voided ones included. */
  count: number;
  /** Waiting for payment — not voided and not on hold. */
  unpaidCount: number;
  unpaidAmount: number;
  /** Held for a discount decision (ADR-109/111); can't be paid until it's decided. */
  onHoldCount: number;
  /** Newest first. */
  recent: InvoiceResponse[];
}

export function summarizeBillsToday(bills: InvoiceResponse[], recentCount = 8): BillsToday {
  let unpaidCount = 0;
  let unpaidAmount = 0;
  let onHoldCount = 0;
  for (const bill of bills) {
    const state = frontDeskBillState(bill);
    if (state === 'Pending') {
      unpaidCount += 1;
      unpaidAmount += amountDue(bill);
    } else if (state === 'OnHold') {
      onHoldCount += 1;
    }
  }
  const recent = [...bills]
    .sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt))
    .slice(0, recentCount);
  return { count: bills.length, unpaidCount, unpaidAmount, onHoldCount, recent };
}
