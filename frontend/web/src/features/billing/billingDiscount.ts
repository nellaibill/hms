import type { InvoiceDiscountType } from '@hms/shared';

/**
 * Bill discounts (ADR-109) — pure helpers shared by the billing form (billingValidation.ts/
 * billingCalculations.ts) and the "Request Discount" dialog on a saved bill. No imports from
 * either of those files, so both can depend on this one without a cycle.
 */

/** A bill line a discount may cover: its id (a form row's BillingItem id, or a saved line item's
 * id), a display label, and its charge before discount (quantity × unit price). */
export interface DiscountableLine {
  id: string;
  label: string;
  amount: number;
}

/** One line's share of a discount. */
export interface DiscountAllocation {
  lineId: string;
  amount: number;
}

/** The whole discount in whole rupees: a % of the chosen lines' total, or the ₹ amount typed —
 * never more than the chosen lines' total, never negative. */
export function discountTotalFor(selectedTotal: number, type: InvoiceDiscountType, value: number): number {
  if (!(value > 0) || !(selectedTotal > 0)) return 0;
  const raw = type === 'Percent' ? (selectedTotal * Math.min(value, 100)) / 100 : value;
  return Math.min(Math.round(raw), Math.floor(selectedTotal));
}

/**
 * Splits the discount across the chosen lines in proportion to their charges, in whole rupees
 * (largest-remainder rounding, so the shares always add up to exactly the total) — e.g. ₹100 off
 * a ₹300 consultation and a ₹700 lab test is ₹30 + ₹70. No line's share ever exceeds its own
 * charge. Lines with a share of zero are left out.
 */
export function allocateDiscount(lines: Pick<DiscountableLine, 'id' | 'amount'>[], type: InvoiceDiscountType, value: number): DiscountAllocation[] {
  const selectedTotal = lines.reduce((sum, line) => sum + line.amount, 0);
  const total = discountTotalFor(selectedTotal, type, value);
  if (total === 0) return [];

  const shares = lines.map((line) => {
    const exact = (total * line.amount) / selectedTotal;
    return { id: line.id, cap: Math.floor(line.amount), amount: Math.min(Math.floor(exact), Math.floor(line.amount)), remainder: exact - Math.floor(exact) };
  });

  let left = total - shares.reduce((sum, share) => sum + share.amount, 0);
  const byRemainder = [...shares].sort((a, b) => b.remainder - a.remainder);
  while (left > 0) {
    const next = byRemainder.find((share) => share.amount < share.cap);
    if (!next) break;
    next.amount += 1;
    left -= 1;
    // Rotate so the next rupee goes to the next-largest remainder, not the same line again.
    byRemainder.push(byRemainder.splice(byRemainder.indexOf(next), 1)[0]);
  }

  return shares.filter((share) => share.amount > 0).map((share) => ({ lineId: share.id, amount: share.amount }));
}

/** A snapshot of the bill's lines (id and charge) — a form discount was worked out against one
 * exact set of lines, so once this changes (a line added, removed or re-priced) it no longer
 * fits the bill and has to be applied again. */
export function linesSignature(lines: Pick<DiscountableLine, 'id' | 'amount'>[]): string {
  return lines.map((line) => `${line.id}:${line.amount}`).join('|');
}

export const DISCOUNT_TYPE_LABELS: Record<InvoiceDiscountType, string> = {
  Amount: '₹ Amount',
  Percent: '% Percent',
};

/** "₹100" or "10%" — how the counter entered it. */
export function describeDiscountEntry(type: InvoiceDiscountType, value: number): string {
  return type === 'Percent' ? `${value}%` : `₹${Math.round(value).toLocaleString('en-IN')}`;
}
