import { ArrowLeft, FileBarChart2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  ExpenseTable,
  ExportButtons,
  getExpenseRows,
  getIncomeRows,
  getReportTotals,
  IncomeTable,
  Pagination,
  paginate,
  ReportDateRangeFilter,
  ReportSummaryCards,
} from '@/features/reports';
import type { ReportDateRange } from '@/features/reports';

const ROWS_PER_PAGE = 10;

function toDateInputValue(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function defaultRange(): ReportDateRange {
  const today = new Date();
  const from = new Date(today);
  from.setDate(from.getDate() - 30);
  return { from: toDateInputValue(from), to: toDateInputValue(today) };
}

/** Finance & Billing's Income & Expense Report (docs/ScreenInventory.md "Reports" screen type) — mock data for now, same as the rest of Finance & Billing (see mockBillingStore.ts / features/reports/mockExpenses.ts). */
export default function IncomeExpenseReportPage() {
  const [range, setRange] = useState<ReportDateRange>(defaultRange);
  const [incomePage, setIncomePage] = useState(1);
  const [expensePage, setExpensePage] = useState(1);

  const incomeRows = useMemo(() => getIncomeRows(range), [range]);
  const expenseRows = useMemo(() => getExpenseRows(range), [range]);
  const totals = useMemo(() => getReportTotals(incomeRows, expenseRows), [incomeRows, expenseRows]);

  const pagedIncome = paginate(incomeRows, incomePage, ROWS_PER_PAGE);
  const pagedExpense = paginate(expenseRows, expensePage, ROWS_PER_PAGE);

  function handleRangeChange(next: ReportDateRange) {
    setRange(next);
    setIncomePage(1);
    setExpensePage(1);
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/finance/accounts" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to Accounts and Finance
        </Link>
      </div>

      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="relative mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <FileBarChart2 className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Income & Expense Report</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Revenue from patient billing against hospital expenses for the selected period.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <div className="flex w-full max-w-6xl flex-col gap-4">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <ReportDateRangeFilter range={range} onChange={handleRangeChange} />
            <ExportButtons range={range} income={incomeRows} expense={expenseRows} />
          </div>

          <ReportSummaryCards totals={totals} />

          <div className="flex flex-col gap-3">
            <h2 className="text-sm font-semibold text-foreground">
              Income <span className="font-normal text-muted-foreground">({incomeRows.length} invoices)</span>
            </h2>
            {incomeRows.length === 0 ? (
              <p className="text-sm text-muted-foreground">No income recorded in this period.</p>
            ) : (
              <>
                <IncomeTable rows={pagedIncome.items} />
                <Pagination meta={pagedIncome.meta} onPageChange={setIncomePage} />
              </>
            )}
          </div>

          <div className="flex flex-col gap-3">
            <h2 className="text-sm font-semibold text-foreground">
              Expenses <span className="font-normal text-muted-foreground">({expenseRows.length} entries)</span>
            </h2>
            {expenseRows.length === 0 ? (
              <p className="text-sm text-muted-foreground">No expenses recorded in this period.</p>
            ) : (
              <>
                <ExpenseTable rows={pagedExpense.items} />
                <Pagination meta={pagedExpense.meta} onPageChange={setExpensePage} />
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
