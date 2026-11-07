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
}

/** Exports every row matching the current date filter — not just the current page — same
 * "a full report, not a screenshot of one page" reasoning as the Income & Expense report's
 * ExportButtons. "—" (not a blank cell) for an unknown cost/profit/margin, so an exported
 * file can't be misread as a confirmed zero. */
function buildSections(rows: ProfitReportRow[], labels: ProfitLabels): ReportSection[] {
  return [
    {
      heading: 'Billed Services',
      headers: ['Date', 'Invoice', 'Patient', 'Type', 'Service', 'Qty', 'Charge', 'Discount', 'Revenue', labels.cost, labels.profit, 'Margin %'],
      rows: rows.map((row) => [
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
      ]),
    },
  ];
}

export function ProfitExportButtons({ range, rows, title, labels = DEFAULT_PROFIT_LABELS }: ProfitExportButtonsProps) {
  const filenameBase = `${title.toLowerCase().replace(/[^a-z0-9]+/g, '-')}_${range.from}_to_${range.to}`;

  async function handleExport(format: ExportFormatOption) {
    const sections = buildSections(rows, labels);
    if (format === 'csv') exportReportToCsv(`${filenameBase}.csv`, sections);
    else if (format === 'excel') await exportReportToExcel(`${filenameBase}.xlsx`, sections);
    else exportReportToPdf(`${filenameBase}.pdf`, `${title} (${range.from} to ${range.to})`, sections);
  }

  return <ExportMenu onExport={handleExport} />;
}
