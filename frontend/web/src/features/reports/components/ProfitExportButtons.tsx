import { ChevronDown, FileSpreadsheet, FileText, Sheet } from 'lucide-react';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
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
  const [isExportingExcel, setIsExportingExcel] = useState(false);
  const filenameBase = `profit-report_${range.from}_to_${range.to}`;

  function handleExportCsv() {
    exportReportToCsv(`${filenameBase}.csv`, buildSections(rows));
  }

  async function handleExportExcel() {
    setIsExportingExcel(true);
    try {
      await exportReportToExcel(`${filenameBase}.xlsx`, buildSections(rows));
    } finally {
      setIsExportingExcel(false);
    }
  }

  function handleExportPdf() {
    exportReportToPdf(`${filenameBase}.pdf`, `Profit Report (${range.from} to ${range.to})`, buildSections(rows));
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button type="button" variant="outline" size="sm" className="gap-1.5" disabled={isExportingExcel}>
          <FileText className="h-4 w-4" />
          {isExportingExcel ? 'Exporting…' : 'Export'}
          <ChevronDown className="h-3.5 w-3.5" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={handleExportPdf}>
          <Sheet className="h-4 w-4 text-destructive" />
          Export as PDF
        </DropdownMenuItem>
        <DropdownMenuItem onClick={handleExportExcel}>
          <FileSpreadsheet className="h-4 w-4 text-success" />
          Export as Excel
        </DropdownMenuItem>
        <DropdownMenuItem onClick={handleExportCsv}>
          <FileText className="h-4 w-4 text-muted-foreground" />
          Export as CSV
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
