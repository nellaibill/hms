import { FileBarChart2, FileSearch, Loader2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { useInvoicesForReportQuery } from '@/features/billing';
import {
  CategoryBreakdownCard,
  ExpenseTable,
  ExportButtons,
  filterExpenseRows,
  filterIncomeRows,
  getExpenseRows,
  getExpensesByCategory,
  getIncomeByBillingType,
  getIncomeRows,
  getReportTotals,
  IncomeTable,
  Pagination,
  paginate,
  ReportFilterBar,
  AccountsNavTabs,
  ReportSummaryCards,
} from '@/features/reports';
import type { ProfitReportFilterState, ReportDateRange } from '@/features/reports';

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

/**
 * Finance & Billing's Income & Expense Report (docs/ScreenInventory.md "Reports" screen type).
 * Income comes from the real Billing API (useInvoicesForReportQuery); expenses are still mock
 * data (features/reports/mockExpenses.ts) pending an Accounts/Expenses backend.
 *
 * Same shared filter bar and draft/applied/"click Search" pattern every other Finance report
 * uses (see ReportFilterBar's own doc comment) — Billing Type/Department/Consultant dropdowns
 * are all hidden here: Income is invoice-level (no per-line billing type/department/consultant
 * breakdown at this granularity) and Expenses are hospital-wide mock entries with none of that
 * attribution yet, so only Date Range + Search apply.
 */
export default function IncomeExpenseReportPage() {
  const [draftRange, setDraftRange] = useState<ReportDateRange>(defaultRange);
  const [draftFilters, setDraftFilters] = useState<ProfitReportFilterState>({});
  const [appliedRange, setAppliedRange] = useState<ReportDateRange>(defaultRange);
  const [appliedFilters, setAppliedFilters] = useState<ProfitReportFilterState>({});
  const [hasSearched, setHasSearched] = useState(false);
  const [incomePage, setIncomePage] = useState(1);
  const [expensePage, setExpensePage] = useState(1);

  const { data: billings, isPending: isLoadingBillings } = useInvoicesForReportQuery();
  const incomeRows = useMemo(
    () => (hasSearched ? filterIncomeRows(getIncomeRows(billings ?? [], appliedRange), appliedFilters.search) : []),
    [hasSearched, billings, appliedRange, appliedFilters.search],
  );
  const expenseRows = useMemo(
    () => (hasSearched ? filterExpenseRows(getExpenseRows(appliedRange), appliedFilters.search) : []),
    [hasSearched, appliedRange, appliedFilters.search],
  );
  const totals = useMemo(() => getReportTotals(incomeRows, expenseRows), [incomeRows, expenseRows]);
  // getIncomeByBillingType reads straight from each invoice's own line items (see its own doc
  // comment) rather than IncomeReportRow — restricting it to only the invoices that survived
  // the search above keeps this breakdown consistent with the Income table right below it,
  // instead of the search silently only narrowing one of the two.
  const incomeRowIds = useMemo(() => new Set(incomeRows.map((row) => row.id)), [incomeRows]);
  const incomeByType = useMemo(
    () => getIncomeByBillingType((billings ?? []).filter((billing) => incomeRowIds.has(billing.id)), appliedRange),
    [billings, appliedRange, incomeRowIds],
  );
  const expensesByCategory = useMemo(() => getExpensesByCategory(expenseRows), [expenseRows]);

  const pagedIncome = paginate(incomeRows, incomePage, ROWS_PER_PAGE);
  const pagedExpense = paginate(expenseRows, expensePage, ROWS_PER_PAGE);

  function handleSearch() {
    setAppliedRange(draftRange);
    setAppliedFilters(draftFilters);
    setHasSearched(true);
    setIncomePage(1);
    setExpensePage(1);
  }

  function handleReset() {
    const fresh = defaultRange();
    setDraftRange(fresh);
    setDraftFilters({});
    setAppliedRange(fresh);
    setAppliedFilters({});
    setHasSearched(false);
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
          <ReportFilterBar
            range={draftRange}
            filters={draftFilters}
            onRangeChange={setDraftRange}
            onFiltersChange={setDraftFilters}
            onSearch={handleSearch}
            onReset={handleReset}
            showBillingType={false}
            showDepartment={false}
            showConsultant={false}
            searchPlaceholder="Search by patient, invoice, category, or description…"
            exportSlot={<ExportButtons range={appliedRange} income={incomeRows} expense={expenseRows} />}
          />

          {!hasSearched ? (
            <div className="flex flex-1 flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-border bg-card px-6 py-20 text-center">
              <span className="flex h-16 w-16 items-center justify-center rounded-full bg-primary/10 text-primary">
                <FileSearch className="h-8 w-8" aria-hidden="true" />
              </span>
              <p className="text-base font-medium text-foreground">No data to display</p>
              <p className="max-w-sm text-sm text-muted-foreground">
                Please select a date range and filters, then click Search to view this report.
              </p>
            </div>
          ) : (
            <>
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
                  <p className="text-sm text-muted-foreground">No income matches the current filters.</p>
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
                  <p className="text-sm text-muted-foreground">No expenses match the current filters.</p>
                ) : (
                  <>
                    <ExpenseTable rows={pagedExpense.items} />
                    <Pagination meta={pagedExpense.meta} onPageChange={setExpensePage} />
                  </>
                )}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
