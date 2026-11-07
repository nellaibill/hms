import { describeBillingItem, resolveItemCostPrice, type Billing, type BillingItem, type BillingType } from '@/features/billing';
import type { BreakdownRow } from './incomeExpenseReport';
import type { ReportDateRange } from './types';

function dateOnly(iso: string): string {
  return iso.slice(0, 10);
}

function inRange(date: string, range: ReportDateRange): boolean {
  return date >= range.from && date <= range.to;
}

function roundToPaise(amount: number): number {
  return Math.round(amount * 100) / 100;
}

/** One row per billed line item (not per invoice) — cost and margin are inherently a
 * per-service concept, so a single invoice mixing e.g. a costed Lab test and an uncosted
 * Pharmacy dispense needs to show that split rather than one blended invoice-level figure. */
export interface ProfitReportRow {
  id: string;
  invoiceId: string;
  invoiceNumber?: string;
  date: string;
  patientId: string;
  patientName: string;
  billingType: BillingType;
  serviceLabel: string;
  departmentId?: string;
  /** Set whenever the underlying line item had a consultant at billing time — every billing
   * type except Injection/File (no doctor involved there). Sourced from `billedConsultantId`,
   * not `consultantId` (see InvoiceLineItemResponse.billedConsultantId's own doc comment), so
   * this stays populated even after the line item is paid. */
  billedConsultantId?: string;
  quantity: number;
  /** quantity × unitPrice — what the line was charged before any discount. */
  charge: number;
  /** The line's approved discount (ADR-109/111); 0 when none. A discount still waiting for
   * approval isn't on the line yet, so it isn't counted here either. */
  discount: number;
  /** charge − discount — already net of discount, the same figure the invoice itself bills. */
  revenue: number;
  /** Per-unit cost resolved from the matching master's CostPrice, or null if unknown/not
   * yet costed (see resolveItemCostPrice) — kept alongside `cost` for a per-unit column. */
  costPerUnit: number | null;
  /** quantity × costPerUnit, or null when costPerUnit is null — null must never collapse
   * to 0 here, or a report reader would mistake "unknown cost" for "confirmed free." For a
   * Consultation line this is the consultant's share, cut by the same percentage as the fee
   * (ADR-113) — see `costDiscount`. */
  cost: number | null;
  /** How much of the line's discount came off `cost` rather than off the hospital's profit:
   * for a Consultation line, the consultant's share of it — the share times discount ÷ charge,
   * so a 40% discount takes 40% off both the share and the hospital's part (ADR-113); 0 for
   * every other line, whose cost isn't anyone's share of the fee. */
  costDiscount: number;
  /** revenue − cost, or null when cost is null. */
  profit: number | null;
  /** profit / revenue as a whole-number percent, or null when profit is null. Revenue of 0
   * with a null cost also yields null (nothing to divide). */
  marginPercent: number | null;
}

/** What a profit report calls its cost and profit figures, on screen, in exports and in print. */
export interface ProfitLabels {
  cost: string;
  profit: string;
}

export const DEFAULT_PROFIT_LABELS: ProfitLabels = { cost: 'Cost', profit: 'Profit' };

/** OP Report names the two figures by whose share they are (ADR-124): the cost on a
 * consultation line is the consultant's share of the fee, and what's left is the hospital's.
 * For other lines (lab, scans, procedures) the "consultant cost" is still that service's cost
 * price. */
export const OP_REPORT_PROFIT_LABELS: ProfitLabels = { cost: 'Consultant Cost', profit: 'Hospital Cost' };

/** OP Report counts only bills for visits registered as OP (ADR-121) — IP, Emergency, Day
 * Care and Observation visits are billed through the same counter but aren't outpatients. A
 * bill whose visit type is unknown (its visit can't be found) is left out too. */
export function onlyOpVisitBillings(billings: Billing[]): Billing[] {
  return billings.filter((billing) => billing.encounterType === 'OP');
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
      const charge = item.quantity * item.unitPrice;
      const fullCost = costPerUnit === null ? null : costPerUnit * item.quantity;
      // ADR-113: a consultation discount is shared by the consultant and the hospital in
      // proportion to their parts of the fee — the same percentage comes off both.
      const costDiscount =
        fullCost !== null && item.billingType === 'Consultation' && charge > 0 ? roundToPaise((fullCost * Math.min(item.discount, charge)) / charge) : 0;
      const cost = fullCost === null ? null : fullCost - costDiscount;
      const profit = cost === null ? null : item.total - cost;
      const marginPercent = profit === null || item.total <= 0 ? null : Math.round((profit / item.total) * 100);

      rows.push({
        id: `${billing.id}-${item.id}`,
        invoiceId: billing.id,
        invoiceNumber: billing.invoiceNumber,
        date,
        patientId: billing.patientId,
        patientName: billing.patientName,
        billingType: item.billingType as BillingType,
        serviceLabel,
        departmentId: item.departmentId,
        billedConsultantId: item.billedConsultantId,
        quantity: item.quantity,
        charge,
        discount: item.discount,
        revenue: item.total,
        costPerUnit,
        cost,
        costDiscount,
        profit,
        marginPercent,
      });
    }
  }
  return rows.sort((a, b) => b.date.localeCompare(a.date));
}

export interface ProfitTotals {
  /** Sum of `charge` — what was billed before discounts. */
  totalCharge: number;
  totalDiscount: number;
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
  const totalCharge = rows.reduce((sum, row) => sum + row.charge, 0);
  const totalDiscount = rows.reduce((sum, row) => sum + row.discount, 0);
  const totalRevenue = rows.reduce((sum, row) => sum + row.revenue, 0);
  const knownCostRows = rows.filter((row) => row.cost !== null);
  const revenueWithKnownCost = knownCostRows.reduce((sum, row) => sum + row.revenue, 0);
  const totalCost = knownCostRows.reduce((sum, row) => sum + (row.cost ?? 0), 0);
  const totalProfit = knownCostRows.reduce((sum, row) => sum + (row.profit ?? 0), 0);
  const revenueWithUnknownCost = totalRevenue - revenueWithKnownCost;
  const overallMarginPercent = revenueWithKnownCost > 0 ? Math.round((totalProfit / revenueWithKnownCost) * 100) : null;

  return { totalCharge, totalDiscount, totalRevenue, revenueWithKnownCost, revenueWithUnknownCost, totalCost, totalProfit, overallMarginPercent };
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

export interface ConsultantProfitRow {
  consultantId: string;
  /** Approved discounts on this consultant's consultation fees — `revenue` is already net of it. */
  discount: number;
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
  const totals = new Map<string, { discount: number; revenue: number; cost: number; hasCost: boolean; profit: number; itemCount: number }>();
  for (const row of rows) {
    if (row.billingType !== 'Consultation' || !row.billedConsultantId) continue;
    const entry = totals.get(row.billedConsultantId) ?? { discount: 0, revenue: 0, cost: 0, hasCost: false, profit: 0, itemCount: 0 };
    entry.discount += row.discount;
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
    discount: t.discount,
    revenue: t.revenue,
    itemCount: t.itemCount,
    cost: t.hasCost ? t.cost : null,
    profit: t.hasCost ? t.profit : null,
    marginPercent: t.hasCost && t.revenue > 0 ? Math.round((t.profit / t.revenue) * 100) : null,
  })).sort((a, b) => b.revenue - a.revenue);
}

/** OP Report's own filter bar state (see ProfitReportFilters) — every field is
 * optional/unset meaning "don't narrow by this," so `{}` is the natural "no filters" value. */
export interface ProfitReportFilterState {
  billingType?: BillingItem['billingType'];
  departmentId?: string;
  consultantId?: string;
  /** Case-insensitive substring match against service/package name, patient name, or invoice
   * number — one box standing in for a formal per-catalog service/package picker, since
   * diagnostic tests/services/consultation types live in separate Masters catalogs with no
   * single combined list to build a dropdown from. */
  search?: string;
}

/** Applies the OP Report's filter bar on top of the date-ranged rows — plain
 * `.filter()`, no different in kind from the date-range filter `getProfitRows` already applies,
 * just a second pass so the date range and these filters stay independently reasoned about. */
export function filterProfitRows(rows: ProfitReportRow[], filters: ProfitReportFilterState): ProfitReportRow[] {
  const search = filters.search?.trim().toLowerCase();
  return rows.filter((row) => {
    if (filters.billingType && row.billingType !== filters.billingType) return false;
    if (filters.departmentId && row.departmentId !== filters.departmentId) return false;
    if (filters.consultantId && row.billedConsultantId !== filters.consultantId) return false;
    if (search) {
      const haystack = `${row.serviceLabel} ${row.patientName} ${row.invoiceNumber ?? ''}`.toLowerCase();
      if (!haystack.includes(search)) return false;
    }
    return true;
  });
}
