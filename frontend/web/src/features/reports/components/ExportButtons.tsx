import { ExportMenu, type ExportFormatOption } from './ExportMenu';
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
  const filenameBase = `income-expense-report_${range.from}_to_${range.to}`;

  async function handleExport(format: ExportFormatOption) {
    if (format === 'csv') exportReportToCsv(`${filenameBase}.csv`, buildSections(income, expense));
    else if (format === 'excel') await exportReportToExcel(`${filenameBase}.xlsx`, buildSections(income, expense));
    else exportReportToPdf(`${filenameBase}.pdf`, `Income & Expense Report (${range.from} to ${range.to})`, buildSections(income, expense));
  }

  return <ExportMenu onExport={handleExport} />;
}
