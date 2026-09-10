import { FileBarChart2, Loader2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { useInvoicesForReportQuery } from '@/features/billing';
import {
  CategoryBreakdownCard,
  ExpenseTable,
  ExportButtons,
  getExpenseRows,
  getExpensesByCategory,
  getIncomeByBillingType,
  getIncomeRows,
  getReportTotals,
  IncomeTable,
  Pagination,
  paginate,
  ReportDateRangeFilter,
  AccountsNavTabs,
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

/** Finance & Billing's Income & Expense Report (docs/ScreenInventory.md "Reports" screen type). Income comes from the real Billing API (useInvoicesForReportQuery); expenses are still mock data (features/reports/mockExpenses.ts) pending an Accounts/Expenses backend. */
export default function IncomeExpenseReportPage() {
  const [range, setRange] = useState<ReportDateRange>(defaultRange);
  const [incomePage, setIncomePage] = useState(1);
  const [expensePage, setExpensePage] = useState(1);

  const { data: billings, isPending: isLoadingBillings } = useInvoicesForReportQuery();
  const incomeRows = useMemo(() => getIncomeRows(billings ?? [], range), [billings, range]);
  const expenseRows = useMemo(() => getExpenseRows(range), [range]);
  const totals = useMemo(() => getReportTotals(incomeRows, expenseRows), [incomeRows, expenseRows]);
  const incomeByType = useMemo(() => getIncomeByBillingType(billings ?? [], range), [billings, range]);
  const expensesByCategory = useMemo(() => getExpensesByCategory(expenseRows), [expenseRows]);

  const pagedIncome = paginate(incomeRows, incomePage, ROWS_PER_PAGE);
  const pagedExpense = paginate(expenseRows, expensePage, ROWS_PER_PAGE);

  function handleRangeChange(next: ReportDateRange) {
    setRange(next);
    setIncomePage(1);
    setExpensePage(1);
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={FileBarChart2}
        title="Income & Expense Report"
        subtitle="Revenue from patient billing against hospital expenses for the selected period."
        backTo="/finance/accounts"
        backLabel="Back to Accounts and Finance"
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <AccountsNavTabs />

        <div className="flex w-full flex-col gap-4">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <ReportDateRangeFilter range={range} onChange={handleRangeChange} />
            <ExportButtons range={range} income={incomeRows} expense={expenseRows} />
          </div>

          <ReportSummaryCards totals={totals} />

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <CategoryBreakdownCard title="Income by Billing Type" rows={incomeByType} tone="success" />
            <CategoryBreakdownCard title="Expenses by Category" rows={expensesByCategory} tone="destructive" />
          </div>

          <div className="flex flex-col gap-3">
            <h2 className="text-sm font-semibold text-foreground">
              Income <span className="font-normal text-muted-foreground">({incomeRows.length} invoices)</span>
            </h2>
            {isLoadingBillings ? (
              <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                Loading invoices…
              </div>
            ) : incomeRows.length === 0 ? (
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
