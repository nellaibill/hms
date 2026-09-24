import { FileSearch, Loader2, TrendingUp } from 'lucide-react';
import { useMemo, useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { useInvoicesForReportQuery } from '@/features/billing';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { useMasterOptionsQuery } from '@/features/masters';
import {
  CategoryBreakdownCard,
  filterProfitRows,
  getProfitByBillingType,
  getProfitByTest,
  getProfitRows,
  getProfitTotals,
  Pagination,
  paginate,
  ProfitExportButtons,
  ProfitSummaryCards,
  ProfitTable,
  ReportFilterBar,
  AccountsNavTabs,
} from '@/features/reports';
import type { ProfitReportFilterState, ReportDateRange } from '@/features/reports';
import { defaultReportDateRange } from '@/lib/reportDateRange';

const ROWS_PER_PAGE = 10;

/**
 * Finance & Billing's Profit Report — margin per billed service line, computed live from
 * each service's current CostPrice against what was actually billed (see
 * features/reports/profitReport.ts's own doc comments for why this is a live lookup rather
 * than a historical snapshot, and why Pharmacy/uncosted services show "—" instead of a
 * fabricated figure). Primes every reference cache describeBillingItem/resolveItemCostPrice
 * read from, the same set InvoiceDetailCard primes for an equivalent reason: this page can be
 * opened directly, without ever visiting a live billing form first.
 *
 * Filters are a draft/applied split, not live: the filter bar edits `draftRange`/`draftFilters`
 * on every keystroke, but the report only recomputes once "Search" commits them into
 * `appliedRange`/`appliedFilters` (see ReportFilterBar's own doc comment for why — shared with
 * every other Finance report, not just this one). The page shows nothing until the first Search
 * — `hasSearched` gates both the report body and `rows` itself, so exporting before ever
 * searching produces an empty file rather than silently exporting the untouched default range.
 */
export default function ProfitReportPage() {
  const [draftRange, setDraftRange] = useState<ReportDateRange>(defaultReportDateRange);
  const [draftFilters, setDraftFilters] = useState<ProfitReportFilterState>({});
  const [appliedRange, setAppliedRange] = useState<ReportDateRange>(defaultReportDateRange);
  const [appliedFilters, setAppliedFilters] = useState<ProfitReportFilterState>({});
  const [hasSearched, setHasSearched] = useState(false);
  const [page, setPage] = useState(1);

  const { data: billings, isPending: isLoadingBillings } = useInvoicesForReportQuery();

  // Each hook below primes a synchronous, module-level reference cache (Masters' registry.ts /
  // diagnostics' referenceCache.ts) that resolveItemCostPrice/describeBillingItem read
  // directly — mutating that cache doesn't itself trigger a re-render. Capturing each hook's
  // `data` and listing it in the rows useMemo's dependency array below is what makes `rows`
  // actually recompute once priming resolves; without it, this page would keep showing its
  // first (cache-still-empty) render's service labels/costs forever, since `billings` and
  // `appliedRange` — the memo's other real inputs — never themselves change when priming
  // finishes.
  const { data: diagnosticTestOptions } = useMasterOptionsQuery('diagnosticTest');
  const { data: departmentOptions } = useMasterOptionsQuery('department');
  const { data: consultantOptions } = useMasterOptionsQuery('consultant');
  const { data: consultationTypeOptions } = useMasterOptionsQuery('consultationType');
  const { services: radiologyServices } = useDiagnosticServices('Radiology');
  const { services: laboratoryServices } = useDiagnosticServices('Laboratory');
  usePrimeDiagnosticPackageCache();

  const rows = useMemo(
    () => (hasSearched ? getProfitRows(billings ?? [], appliedRange) : []),
    [
      hasSearched,
      billings,
      appliedRange,
      diagnosticTestOptions,
      departmentOptions,
      consultantOptions,
      consultationTypeOptions,
      radiologyServices,
      laboratoryServices,
    ],
  );
  const filteredRows = useMemo(() => filterProfitRows(rows, appliedFilters), [rows, appliedFilters]);
  const totals = useMemo(() => getProfitTotals(filteredRows), [filteredRows]);
  const byTest = useMemo(() => getProfitByTest(filteredRows), [filteredRows]);
  const byBillingType = useMemo(() => getProfitByBillingType(filteredRows), [filteredRows]);

  const pagedRows = paginate(filteredRows, page, ROWS_PER_PAGE);

  function handleSearch() {
    setAppliedRange(draftRange);
    setAppliedFilters(draftFilters);
    setHasSearched(true);
    setPage(1);
  }

  function handleReset() {
    const fresh = defaultReportDateRange();
    setDraftRange(fresh);
    setDraftFilters({});
    setAppliedRange(fresh);
    setAppliedFilters({});
    setHasSearched(false);
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
          <ReportFilterBar
            range={draftRange}
            filters={draftFilters}
            onRangeChange={setDraftRange}
            onFiltersChange={setDraftFilters}
            onSearch={handleSearch}
            onReset={handleReset}
            exportSlot={<ProfitExportButtons range={appliedRange} rows={filteredRows} />}
          />

          {!hasSearched ? (
            <div className="flex flex-1 flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-border bg-card px-6 py-20 text-center">
              <span className="flex h-16 w-16 items-center justify-center rounded-full bg-primary/10 text-primary">
                <FileSearch className="h-8 w-8" aria-hidden="true" />
              </span>
              <p className="text-base font-medium text-foreground">No data to display</p>
              <p className="max-w-sm text-sm text-muted-foreground">
                Please select a date range and filters, then click Search to view the hospital profit report.
              </p>
            </div>
          ) : (
            <>
              <ProfitSummaryCards totals={totals} />

              <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
                <CategoryBreakdownCard title="Profit by Service" rows={byTest} tone="success" maxRows={8} />
                <CategoryBreakdownCard title="Profit by Billing Type" rows={byBillingType} tone="success" maxRows={8} />
              </div>

              <div className="flex flex-col gap-3">
                <h2 className="text-sm font-semibold text-foreground">
                  Billed Services <span className="font-normal text-muted-foreground">({filteredRows.length} line items)</span>
                </h2>
                {isLoadingBillings ? (
                  <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Loading invoices…
                  </div>
                ) : filteredRows.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No billed services match the current filters.</p>
                ) : (
                  <>
                    <ProfitTable rows={pagedRows.items} />
                    <Pagination meta={pagedRows.meta} onPageChange={setPage} />
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
