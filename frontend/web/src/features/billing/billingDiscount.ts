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
  /** Who decides this line's discount (ADR-111): the consultant's name for their consultation
   * fee; undefined for every other line, which the Super Admin decides. */
  approver?: string;
}

/** "Dr. S. Karthikeyan and the Super Admin" — everyone who has to decide a discount on these
 * lines, consultants first. */
export function describeDiscountApprovers(lines: Pick<DiscountableLine, 'approver'>[]): string {
  const consultants = [...new Set(lines.map((line) => line.approver).filter((name): name is string => Boolean(name)))];
  const names = lines.some((line) => !line.approver) ? [...consultants, 'the Super Admin'] : consultants;
  if (names.length <= 1) return names[0] ?? 'the Super Admin';
  return `${names.slice(0, -1).join(', ')} and ${names[names.length - 1]}`;
}

/** One line's ₹ discount. */
export interface DiscountAllocation {
  lineId: string;
  amount: number;
}

/**
 * Splits a whole-bill ₹ discount across the lines in proportion to their charges, in whole rupees
 * (largest-remainder rounding, so the shares always add up to exactly the total) — e.g. ₹100 off
 * a ₹300 consultation and a ₹700 lab test is ₹30 + ₹70. The total is rounded to whole rupees and
 * capped at the lines' total; no line's share ever exceeds its own charge. Lines with a share of
 * zero are left out.
 */
export function allocateDiscount(lines: Pick<DiscountableLine, 'id' | 'amount'>[], amount: number): DiscountAllocation[] {
  const linesTotal = lines.reduce((sum, line) => sum + line.amount, 0);
  if (!(amount > 0) || !(linesTotal > 0)) return [];
  const total = Math.min(Math.round(amount), Math.floor(linesTotal));
  if (total === 0) return [];

  const shares = lines.map((line) => {
    const exact = (total * line.amount) / linesTotal;
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

/** "₹100" or "10%" — how the counter entered it. New discounts are always ₹ amounts entered
 * line by line; "%" only appears on requests raised before that change. */
export function describeDiscountEntry(type: InvoiceDiscountType, value: number): string {
  return type === 'Percent' ? `${value}%` : `₹${Math.round(value).toLocaleString('en-IN')}`;
}
