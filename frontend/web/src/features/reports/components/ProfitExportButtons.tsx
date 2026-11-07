import { formatDateRangeLabel } from '@/lib/reportDateRange';
import { ExportMenu, type ExportFormatOption } from './ExportMenu';
import { exportReportToCsv, exportReportToExcel, exportReportToPdf, type ReportSection } from '../exportUtils';
import { DEFAULT_PROFIT_LABELS, type ProfitLabels, type ProfitReportRow } from '../profitReport';
import type { ReportDateRange } from '../types';

interface ProfitExportButtonsProps {
  range: ReportDateRange;
  rows: ProfitReportRow[];
  /** The report's own name, e.g. "OP Report" or "Laboratory Report" — the PDF's title and,
   * lower-cased and hyphenated, the start of every file name. */
  title: string;
  labels?: ProfitLabels;
  /** Leaves the Margin % column out — OP Report doesn't show margin (ADR-126). */
  hideMargin?: boolean;
}

/** Exports every row matching the current date filter — not just the current page — same
 * "a full report, not a screenshot of one page" reasoning as the Income & Expense report's
 * ExportButtons. "—" (not a blank cell) for an unknown cost/profit/margin, so an exported
 * file can't be misread as a confirmed zero. */
function buildSections(rows: ProfitReportRow[], labels: ProfitLabels, hideMargin: boolean): ReportSection[] {
  const headers = ['Date', 'Invoice', 'Patient', 'Type', 'Service', 'Qty', 'Charge', 'Discount', 'Revenue', labels.cost, labels.profit, 'Margin %'];
  return [
    {
      heading: 'Billed Services',
      headers: hideMargin ? headers.slice(0, -1) : headers,
      rows: rows.map((row) => {
        const cells: (string | number)[] = [
          row.date,
          row.invoiceNumber ?? row.invoiceId,
          row.patientName,
          row.billingType,
          row.serviceLabel,
          row.quantity,
          row.charge,
          row.discount,
          row.revenue,
          row.cost ?? '—',
          row.profit ?? '—',
          row.marginPercent === null ? '—' : row.marginPercent,
        ];
        return hideMargin ? cells.slice(0, -1) : cells;
      }),
    },
  ];
}

export function ProfitExportButtons({ range, rows, title, labels = DEFAULT_PROFIT_LABELS, hideMargin = false }: ProfitExportButtonsProps) {
  const filenameBase = `${title.toLowerCase().replace(/[^a-z0-9]+/g, '-')}_${range.from}_to_${range.to}`;

  async function handleExport(format: ExportFormatOption) {
    const sections = buildSections(rows, labels, hideMargin);
    if (format === 'csv') exportReportToCsv(`${filenameBase}.csv`, sections);
    else if (format === 'excel') await exportReportToExcel(`${filenameBase}.xlsx`, sections);
    else exportReportToPdf(`${filenameBase}.pdf`, `${title} (${formatDateRangeLabel(range.from, range.to)})`, sections);
  }

  return <ExportMenu onExport={handleExport} />;
}
