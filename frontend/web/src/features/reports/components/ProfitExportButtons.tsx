import { ExportMenu, type ExportFormatOption } from './ExportMenu';
import { exportReportToCsv, exportReportToExcel, exportReportToPdf, type ReportSection } from '../exportUtils';
import type { ProfitReportRow } from '../profitReport';
import type { ReportDateRange } from '../types';

interface ProfitExportButtonsProps {
  range: ReportDateRange;
  rows: ProfitReportRow[];
}

/** Exports every row matching the current date filter — not just the current page — same
 * "a full report, not a screenshot of one page" reasoning as the Income & Expense report's
 * ExportButtons. "—" (not a blank cell) for an unknown cost/profit/margin, so an exported
 * file can't be misread as a confirmed zero. */
function buildSections(rows: ProfitReportRow[]): ReportSection[] {
  return [
    {
      heading: 'Profit by Service Line',
      headers: ['Date', 'Invoice', 'Patient', 'Type', 'Service', 'Qty', 'Revenue', 'Cost', 'Profit', 'Margin %'],
      rows: rows.map((row) => [
        row.date,
        row.invoiceNumber ?? row.invoiceId,
        row.patientName,
        row.billingType,
        row.serviceLabel,
        row.quantity,
        row.revenue,
        row.cost ?? '—',
        row.profit ?? '—',
        row.marginPercent === null ? '—' : row.marginPercent,
      ]),
    },
  ];
}

export function ProfitExportButtons({ range, rows }: ProfitExportButtonsProps) {
  const filenameBase = `profit-report_${range.from}_to_${range.to}`;

  async function handleExport(format: ExportFormatOption) {
    if (format === 'csv') exportReportToCsv(`${filenameBase}.csv`, buildSections(rows));
    else if (format === 'excel') await exportReportToExcel(`${filenameBase}.xlsx`, buildSections(rows));
    else exportReportToPdf(`${filenameBase}.pdf`, `Profit Report (${range.from} to ${range.to})`, buildSections(rows));
  }

  return <ExportMenu onExport={handleExport} />;
}
