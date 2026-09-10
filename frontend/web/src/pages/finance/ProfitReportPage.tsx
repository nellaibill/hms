import { Loader2, TrendingUp } from 'lucide-react';
import { useMemo, useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { useInvoicesForReportQuery } from '@/features/billing';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { useMasterOptionsQuery } from '@/features/masters';
import {
  CategoryBreakdownCard,
  getProfitByBillingType,
  getProfitByTest,
  getProfitRows,
  getProfitTotals,
  Pagination,
  paginate,
  ProfitExportButtons,
  ProfitSummaryCards,
  ProfitTable,
  ReportDateRangeFilter,
  AccountsNavTabs,
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

/** Finance & Billing's Profit Report — margin per billed service line, computed live from
 * each service's current CostPrice against what was actually billed (see
 * features/reports/profitReport.ts's own doc comments for why this is a live lookup rather
 * than a historical snapshot, and why Pharmacy/uncosted services show "—" instead of a
 * fabricated figure). Primes every reference cache describeBillingItem/resolveItemCostPrice
 * read from, the same set InvoiceDetailCard primes for an equivalent reason: this page can be
 * opened directly, without ever visiting a live billing form first. */
export default function ProfitReportPage() {
  const [range, setRange] = useState<ReportDateRange>(defaultRange);
  const [page, setPage] = useState(1);

  const { data: billings, isPending: isLoadingBillings } = useInvoicesForReportQuery();

  // Each hook below primes a synchronous, module-level reference cache (Masters' registry.ts /
  // diagnostics' referenceCache.ts) that resolveItemCostPrice/describeBillingItem read
  // directly — mutating that cache doesn't itself trigger a re-render. Capturing each hook's
  // `data` and listing it in the rows useMemo's dependency array below is what makes `rows`
  // actually recompute once priming resolves; without it, this page would keep showing its
  // first (cache-still-empty) render's service labels/costs forever, since `billings` and
  // `range` — the memo's only other inputs — never themselves change when priming finishes.
  const { data: diagnosticTestOptions } = useMasterOptionsQuery('diagnosticTest');
  const { data: departmentOptions } = useMasterOptionsQuery('department');
  const { data: consultantOptions } = useMasterOptionsQuery('consultant');
  const { data: consultationTypeOptions } = useMasterOptionsQuery('consultationType');
  const { services: radiologyServices } = useDiagnosticServices('Radiology');
  const { services: laboratoryServices } = useDiagnosticServices('Laboratory');
  usePrimeDiagnosticPackageCache();

  const rows = useMemo(
    () => getProfitRows(billings ?? [], range),
    [billings, range, diagnosticTestOptions, departmentOptions, consultantOptions, consultationTypeOptions, radiologyServices, laboratoryServices],
  );
  const totals = useMemo(() => getProfitTotals(rows), [rows]);
  const byTest = useMemo(() => getProfitByTest(rows), [rows]);
  const byBillingType = useMemo(() => getProfitByBillingType(rows), [rows]);

  const pagedRows = paginate(rows, page, ROWS_PER_PAGE);

  function handleRangeChange(next: ReportDateRange) {
    setRange(next);
    setPage(1);
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={TrendingUp}
        title="Hospital Profit Report"
        subtitle="Margin per billed service — revenue against each service's running cost for the selected period."
        backTo="/finance/accounts"
        backLabel="Back to Accounts and Finance"
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <AccountsNavTabs />

        <div className="flex w-full flex-col gap-4">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <ReportDateRangeFilter range={range} onChange={handleRangeChange} />
            <ProfitExportButtons range={range} rows={rows} />
          </div>

          <ProfitSummaryCards totals={totals} />

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <CategoryBreakdownCard title="Profit by Service" rows={byTest} tone="success" maxRows={8} />
            <CategoryBreakdownCard title="Profit by Billing Type" rows={byBillingType} tone="success" maxRows={8} />
          </div>

          <div className="flex flex-col gap-3">
            <h2 className="text-sm font-semibold text-foreground">
              Billed Services <span className="font-normal text-muted-foreground">({rows.length} line items)</span>
            </h2>
            {isLoadingBillings ? (
              <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                Loading invoices…
              </div>
            ) : rows.length === 0 ? (
              <p className="text-sm text-muted-foreground">No billed services in this period.</p>
            ) : (
              <>
                <ProfitTable rows={pagedRows.items} />
                <Pagination meta={pagedRows.meta} onPageChange={setPage} />
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
