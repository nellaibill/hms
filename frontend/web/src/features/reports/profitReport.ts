import { describeBillingItem, resolveItemCostPrice, type Billing, type BillingType } from '@/features/billing';
import type { BreakdownRow } from './incomeExpenseReport';
import type { ReportDateRange } from './types';

function dateOnly(iso: string): string {
  return iso.slice(0, 10);
}

function inRange(date: string, range: ReportDateRange): boolean {
  return date >= range.from && date <= range.to;
}

/** One row per billed line item (not per invoice) — cost and margin are inherently a
 * per-service concept, so a single invoice mixing e.g. a costed Lab test and an uncosted
 * Pharmacy dispense needs to show that split rather than one blended invoice-level figure. */
export interface ProfitReportRow {
  id: string;
  invoiceId: string;
  invoiceNumber?: string;
  date: string;
  patientName: string;
  billingType: BillingType;
  serviceLabel: string;
  /** Only ever set on Consultation-type rows — the only BillingType carrying consultant
   * attribution at all. Sourced from `billedConsultantId`, not `consultantId` (see
   * InvoiceLineItemResponse.billedConsultantId's own doc comment), so this stays populated
   * even after the line item is paid. */
  billedConsultantId?: string;
  quantity: number;
  /** Already net of discount — same figure the invoice itself bills. */
  revenue: number;
  /** Per-unit cost resolved from the matching master's CostPrice, or null if unknown/not
   * yet costed (see resolveItemCostPrice) — kept alongside `cost` for a per-unit column. */
  costPerUnit: number | null;
  /** quantity × costPerUnit, or null when costPerUnit is null — null must never collapse
   * to 0 here, or a report reader would mistake "unknown cost" for "confirmed free." */
  cost: number | null;
  /** revenue − cost, or null when cost is null. */
  profit: number | null;
  /** profit / revenue as a whole-number percent, or null when profit is null. Revenue of 0
   * with a null cost also yields null (nothing to divide). */
  marginPercent: number | null;
}

/** Voided invoices are excluded — discarded transactions, same treatment as the Income
 * report's getIncomeRows. */
export function getProfitRows(billings: Billing[], range: ReportDateRange): ProfitReportRow[] {
  const rows: ProfitReportRow[] = [];
  for (const billing of billings) {
    if (billing.isVoided) continue;
    const date = dateOnly(billing.createdAt);
    if (!inRange(date, range)) continue;

    for (const item of billing.items) {
      const { serviceLabel } = describeBillingItem(item);
      const costPerUnit = resolveItemCostPrice(item);
      const cost = costPerUnit === null ? null : costPerUnit * item.quantity;
      const profit = cost === null ? null : item.total - cost;
      const marginPercent = profit === null || item.total <= 0 ? null : Math.round((profit / item.total) * 100);

      rows.push({
        id: `${billing.id}-${item.id}`,
        invoiceId: billing.id,
        invoiceNumber: billing.invoiceNumber,
        date,
        patientName: billing.patientName,
        billingType: item.billingType as BillingType,
        serviceLabel,
        billedConsultantId: item.billedConsultantId,
        quantity: item.quantity,
        revenue: item.total,
        costPerUnit,
        cost,
        profit,
        marginPercent,
      });
    }
  }
  return rows.sort((a, b) => b.date.localeCompare(a.date));
}

export interface ProfitTotals {
  totalRevenue: number;
  /** Sum of `revenue` only over rows with a known cost — the denominator overallMarginPercent
   * is computed against, since revenue with unknown cost has no profit figure to include. */
  revenueWithKnownCost: number;
  /** Revenue from rows where cost couldn't be resolved (Pharmacy, packages, or anything not
   * yet costed) — surfaced so a reader knows how much of Total Revenue this report's profit
   * figures don't yet cover, rather than that gap silently vanishing into a rounding error. */
  revenueWithUnknownCost: number;
  totalCost: number;
  totalProfit: number;
  overallMarginPercent: number | null;
}

export function getProfitTotals(rows: ProfitReportRow[]): ProfitTotals {
  const totalRevenue = rows.reduce((sum, row) => sum + row.revenue, 0);
  const knownCostRows = rows.filter((row) => row.cost !== null);
  const revenueWithKnownCost = knownCostRows.reduce((sum, row) => sum + row.revenue, 0);
  const totalCost = knownCostRows.reduce((sum, row) => sum + (row.cost ?? 0), 0);
  const totalProfit = knownCostRows.reduce((sum, row) => sum + (row.profit ?? 0), 0);
  const revenueWithUnknownCost = totalRevenue - revenueWithKnownCost;
  const overallMarginPercent = revenueWithKnownCost > 0 ? Math.round((totalProfit / revenueWithKnownCost) * 100) : null;

  return { totalRevenue, revenueWithKnownCost, revenueWithUnknownCost, totalCost, totalProfit, overallMarginPercent };
}

/** Profit by service — only rows with a known cost contribute (see ProfitTotals'
 * revenueWithKnownCost for the same reasoning); a service that's never been costed simply
 * doesn't appear here rather than showing a misleading ₹0 profit bar. */
export function getProfitByTest(rows: ProfitReportRow[]): BreakdownRow[] {
  const totals = new Map<string, number>();
  for (const row of rows) {
    if (row.profit === null) continue;
    totals.set(row.serviceLabel, (totals.get(row.serviceLabel) ?? 0) + row.profit);
  }
  return Array.from(totals, ([label, amount]) => ({ label, amount })).sort((a, b) => b.amount - a.amount);
}

/** Profit by billing type (Consultation/Radiology/Laboratory/Procedure/Injection/File) —
 * Pharmacy never appears here since it has no cost concept (resolveItemCostPrice always
 * returns null for it), same reasoning as getProfitByTest. */
export function getProfitByBillingType(rows: ProfitReportRow[]): BreakdownRow[] {
  const totals = new Map<string, number>();
  for (const row of rows) {
    if (row.profit === null) continue;
    totals.set(row.billingType, (totals.get(row.billingType) ?? 0) + row.profit);
  }
  return Array.from(totals, ([label, amount]) => ({ label, amount })).sort((a, b) => b.amount - a.amount);
}

export interface ConsultantProfitRow {
  consultantId: string;
  revenue: number;
  itemCount: number;
  /** null when none of this consultant's rows have a known cost yet (see ProfitReportRow.cost) —
   * kept alongside revenue rather than collapsed into it, same "unknown isn't zero" reasoning as
   * ProfitTotals. */
  cost: number | null;
  profit: number | null;
  marginPercent: number | null;
}

/** Revenue/cost/profit per consultant — Consultation-type rows only, the only BillingType
 * carrying consultant attribution at all. Grouped by `billedConsultantId`, not `consultantId`,
 * so a paid consultation still counts (see ProfitReportRow.billedConsultantId's own doc
 * comment). A Consultation row billed before this field existed has no billedConsultantId and
 * is simply excluded rather than grouped under a misleading "Unknown" bucket. */
export function getProfitByConsultant(rows: ProfitReportRow[]): ConsultantProfitRow[] {
  const totals = new Map<string, { revenue: number; cost: number; hasCost: boolean; profit: number; itemCount: number }>();
  for (const row of rows) {
    if (row.billingType !== 'Consultation' || !row.billedConsultantId) continue;
    const entry = totals.get(row.billedConsultantId) ?? { revenue: 0, cost: 0, hasCost: false, profit: 0, itemCount: 0 };
    entry.revenue += row.revenue;
    entry.itemCount += 1;
    if (row.cost !== null) {
      entry.cost += row.cost;
      entry.profit += row.profit ?? 0;
      entry.hasCost = true;
    }
    totals.set(row.billedConsultantId, entry);
  }
  return Array.from(totals, ([consultantId, t]) => ({
    consultantId,
    revenue: t.revenue,
    itemCount: t.itemCount,
    cost: t.hasCost ? t.cost : null,
    profit: t.hasCost ? t.profit : null,
    marginPercent: t.hasCost && t.revenue > 0 ? Math.round((t.profit / t.revenue) * 100) : null,
  })).sort((a, b) => b.revenue - a.revenue);
}
