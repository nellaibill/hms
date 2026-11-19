import { hasConsultantShare, roundToPaise, type ProfitReportRow } from './profitReport';

/**
 * Day-wise Profit (ADR-142): OP bill lines grouped by day, then by the billed consultant, then
 * by charge head (Consultation, Laboratory, Radiology, Procedure, Injection, File…), with the
 * money at every level split the way OP Report splits a line (ADR-124/141):
 *   - Consultant share: the cost of a consultation, procedure, injection or file line.
 *   - Running cost: the cost of a lab or radiology line (a scan's includes its reporting
 *     doctor's share) — the hospital's expense, not the billed consultant's.
 *   - Hospital profit: revenue − consultant share − running cost.
 * A line with no cost yet adds its revenue to "not costed" and nothing to the shares or profit,
 * so an unknown cost is never shown as a confirmed zero.
 */

export interface ProfitFigures {
  lines: number;
  quantity: number;
  /** Before discount. */
  charge: number;
  discount: number;
  /** charge − discount: what the bills came to. */
  revenue: number;
  consultantShare: number;
  runningCost: number;
  hospitalProfit: number;
  /** Revenue of lines whose cost isn't known yet — left out of the three figures above. */
  notCostedRevenue: number;
}

export interface HeadGroup {
  head: string;
  label: string;
  figures: ProfitFigures;
}

export interface ConsultantGroup {
  /** Undefined for lines billed without a consultant (e.g. injections, files). */
  consultantId?: string;
  heads: HeadGroup[];
  figures: ProfitFigures;
}

export interface DayGroup {
  date: string;
  consultants: ConsultantGroup[];
  figures: ProfitFigures;
}

export interface DayWiseProfit {
  days: DayGroup[];
  /** The same lines totalled per charge head over the whole range. */
  byHead: HeadGroup[];
  total: ProfitFigures;
}

/** Charge heads in the order the billing screen lists them; anything else follows by name. */
const HEAD_ORDER = ['Consultation', 'Laboratory', 'Radiology', 'Procedure', 'Injection', 'File', 'Pharmacy', 'InpatientCharge'];

export function chargeHeadLabel(head: string): string {
  return head === 'InpatientCharge' ? 'Inpatient Charge' : head;
}

function emptyFigures(): ProfitFigures {
  return { lines: 0, quantity: 0, charge: 0, discount: 0, revenue: 0, consultantShare: 0, runningCost: 0, hospitalProfit: 0, notCostedRevenue: 0 };
}

function addRow(figures: ProfitFigures, row: ProfitReportRow): void {
  figures.lines += 1;
  figures.quantity += row.quantity;
  figures.charge += row.charge;
  figures.discount += row.discount;
  figures.revenue += row.revenue;
  if (row.cost === null || row.profit === null) {
    figures.notCostedRevenue += row.revenue;
    return;
  }
  if (hasConsultantShare(row.billingType)) figures.consultantShare += row.cost;
  else figures.runningCost += row.cost;
  figures.hospitalProfit += row.profit;
}

function rounded(figures: ProfitFigures): ProfitFigures {
  return {
    ...figures,
    charge: roundToPaise(figures.charge),
    discount: roundToPaise(figures.discount),
    revenue: roundToPaise(figures.revenue),
    consultantShare: roundToPaise(figures.consultantShare),
    runningCost: roundToPaise(figures.runningCost),
    hospitalProfit: roundToPaise(figures.hospitalProfit),
    notCostedRevenue: roundToPaise(figures.notCostedRevenue),
  };
}

/** Hospital profit as a share of the revenue whose cost is known; null when none is. */
export function profitPercent(figures: ProfitFigures): number | null {
  const costedRevenue = figures.revenue - figures.notCostedRevenue;
  return costedRevenue > 0 ? Math.round((figures.hospitalProfit / costedRevenue) * 100) : null;
}

function headRank(head: string): number {
  const index = HEAD_ORDER.indexOf(head);
  return index === -1 ? HEAD_ORDER.length : index;
}

function groupHeads(rows: ProfitReportRow[]): HeadGroup[] {
  const byHead = new Map<string, ProfitFigures>();
  for (const row of rows) {
    const figures = byHead.get(row.billingType) ?? emptyFigures();
    addRow(figures, row);
    byHead.set(row.billingType, figures);
  }
  return [...byHead]
    .map(([head, figures]) => ({ head, label: chargeHeadLabel(head), figures: rounded(figures) }))
    .sort((a, b) => headRank(a.head) - headRank(b.head) || a.head.localeCompare(b.head));
}

function totalOf(rows: ProfitReportRow[]): ProfitFigures {
  const figures = emptyFigures();
  for (const row of rows) addRow(figures, row);
  return rounded(figures);
}

/**
 * Groups profit rows by day (oldest first), then consultant (by name, "no consultant" last),
 * then charge head. `consultantName` resolves a consultant id for sorting.
 */
export function groupDayWiseProfit(rows: ProfitReportRow[], consultantName: (id: string) => string): DayWiseProfit {
  const byDay = new Map<string, ProfitReportRow[]>();
  for (const row of rows) {
    const dayRows = byDay.get(row.date) ?? [];
    dayRows.push(row);
    byDay.set(row.date, dayRows);
  }

  const days = [...byDay.keys()].sort().map((date) => {
    const dayRows = byDay.get(date) ?? [];
    const byConsultant = new Map<string, ProfitReportRow[]>();
    for (const row of dayRows) {
      const key = row.billedConsultantId ?? '';
      const consultantRows = byConsultant.get(key) ?? [];
      consultantRows.push(row);
      byConsultant.set(key, consultantRows);
    }
    const consultants = [...byConsultant]
      .map(([key, consultantRows]) => ({ consultantId: key || undefined, heads: groupHeads(consultantRows), figures: totalOf(consultantRows) }))
      .sort((a, b) => {
        if (!a.consultantId) return 1;
        if (!b.consultantId) return -1;
        return consultantName(a.consultantId).localeCompare(consultantName(b.consultantId));
      });
    return { date, consultants, figures: totalOf(dayRows) };
  });

  return { days, byHead: groupHeads(rows), total: totalOf(rows) };
}
