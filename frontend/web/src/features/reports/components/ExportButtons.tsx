import { FileSpreadsheet, FileText, Sheet } from 'lucide-react';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { exportReportToCsv, exportReportToExcel, exportReportToPdf, type ReportSection } from '../exportUtils';
import type { ExpenseReportRow, IncomeReportRow, ReportDateRange } from '../types';

interface ExportButtonsProps {
  range: ReportDateRange;
  income: IncomeReportRow[];
  expense: ExpenseReportRow[];
}

/** Exports every row matching the current date filter — not just the current page — since an export is expected to be the complete report, not a screenshot of one page of it. */
function buildSections(income: IncomeReportRow[], expense: ExpenseReportRow[]): ReportSection[] {
  return [
    {
      heading: 'Income',
      headers: ['Date', 'Invoice', 'Patient', 'UHID', 'Billing Type(s)', 'Amount', 'Payment Status'],
      rows: income.map((row) => [row.date, row.id, row.patientName, row.patientUhid, row.billingTypes, row.amount, row.paymentStatus]),
    },
    {
      heading: 'Expenses',
      headers: ['Date', 'Category', 'Description', 'Amount'],
      rows: expense.map((row) => [row.date, row.category, row.description, row.amount]),
    },
  ];
}

export function ExportButtons({ range, income, expense }: ExportButtonsProps) {
  const [isExportingExcel, setIsExportingExcel] = useState(false);
  const filenameBase = `income-expense-report_${range.from}_to_${range.to}`;

  function handleExportCsv() {
    exportReportToCsv(`${filenameBase}.csv`, buildSections(income, expense));
  }

  async function handleExportExcel() {
    setIsExportingExcel(true);
    try {
      await exportReportToExcel(`${filenameBase}.xlsx`, buildSections(income, expense));
    } finally {
      setIsExportingExcel(false);
    }
  }

  function handleExportPdf() {
    exportReportToPdf(`${filenameBase}.pdf`, `Income & Expense Report (${range.from} to ${range.to})`, buildSections(income, expense));
  }

  return (
    <div className="flex flex-wrap gap-2">
      <Button type="button" variant="outline" size="sm" className="gap-1.5" onClick={handleExportCsv}>
        <FileText className="h-4 w-4" />
        Export CSV
      </Button>
      <Button type="button" variant="outline" size="sm" className="gap-1.5" onClick={handleExportExcel} disabled={isExportingExcel}>
        <FileSpreadsheet className="h-4 w-4" />
        {isExportingExcel ? 'Exporting…' : 'Export Excel'}
      </Button>
      <Button type="button" variant="outline" size="sm" className="gap-1.5" onClick={handleExportPdf}>
        <Sheet className="h-4 w-4" />
        Export PDF
      </Button>
    </div>
  );
}
