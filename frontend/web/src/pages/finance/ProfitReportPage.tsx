import { Loader2, Printer, TrendingUp } from 'lucide-react';
import { useMemo, useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { useInvoicesForReportQuery } from '@/features/billing';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { resolveRecordLabel, useMasterOptionsQuery } from '@/features/masters';
import { formatDateRangeLabel } from '@/lib/reportDateRange';
import {
  filterProfitRows,
  getProfitRows,
  getProfitTotals,
  onlyOpVisitBillings,
  opReportHiddenColumns,
  OP_REPORT_PROFIT_LABELS,
  Pagination,
  paginate,
  ProfitExportButtons,
  ProfitReportPrint,
  ProfitSummaryCards,
  ProfitTable,
  ReportFilterBar,
  AccountsNavTabs,
  useReportFilters,
} from '@/features/reports';

const ROWS_PER_PAGE = 10;
const REPORT_TITLE = 'OP Report';

/**
 * Finance & Billing's OP Report (was "Hospital Profit Report"; ADR-119) — margin per billed
 * service line on bills for OP visits only (ADR-121), computed live from
 * each service's current CostPrice against what was actually billed (see
 * features/reports/profitReport.ts's own doc comments for why this is a live lookup rather
 * than a historical snapshot, and why Pharmacy/uncosted services show "—" instead of a
 * fabricated figure). Primes every reference cache describeBillingItem/resolveItemCostPrice
 * read from, the same set InvoiceDetailCard primes for an equivalent reason: this page can be
 * opened directly, without ever visiting a live billing form first.
 *
 * Filters are live and remembered for the browser session — see useReportFilters' own doc
 * comment (shared with every other Finance report, not just this one). The report for the
 * default range is shown on first load; Reset returns to it.
 */
export default function ProfitReportPage() {
  const [page, setPage] = useState(1);
  const { range, filters, appliedRange, appliedFilters, setRange, setFilters, reset } = useReportFilters('profit', () => setPage(1));

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
    () => getProfitRows(onlyOpVisitBillings(billings ?? []), appliedRange),
    [
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

  const pagedRows = paginate(filteredRows, page, ROWS_PER_PAGE);

  // A column whose value the filters already fix is left out (ADR-126); the value is named in
  // the heading (and the printout's header line) instead.
  const hidden = opReportHiddenColumns(appliedRange, appliedFilters);
  // One consultant picked hides Department too — name it when all their lines share one.
  const consultantDepartmentIds =
    appliedFilters.consultantId && !appliedFilters.departmentId ? [...new Set(filteredRows.map((row) => row.departmentId).filter(Boolean))] : [];
  const departmentId = appliedFilters.departmentId ?? (consultantDepartmentIds.length === 1 ? consultantDepartmentIds[0] : undefined);
  const filterSummary = [
    appliedFilters.billingType && `Type: ${appliedFilters.billingType}`,
    departmentId && `Department: ${resolveRecordLabel('department', departmentId)}`,
    appliedFilters.consultantId && `Consultant: ${resolveRecordLabel('consultant', appliedFilters.consultantId)}`,
    appliedFilters.search?.trim() && `Search: "${appliedFilters.search.trim()}"`,
  ].filter((part): part is string => Boolean(part));

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={TrendingUp}
        title={REPORT_TITLE}
        subtitle="Revenue per billed service on OP visits — the consultant's and the hospital's share for the selected period."
        backTo="/finance/accounts"
        backLabel="Back to Accounts and Finance"
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <AccountsNavTabs />

        <div className="flex w-full flex-col gap-4">
          <ReportFilterBar
            range={range}
            filters={filters}
            onRangeChange={setRange}
            onFiltersChange={setFilters}
            onReset={reset}
            exportSlot={
              <>
                <Button type="button" variant="outline" size="sm" className="gap-1.5" onClick={() => window.print()} disabled={filteredRows.length === 0}>
                  <Printer className="h-4 w-4" />
                  Print
                </Button>
                <ProfitExportButtons range={appliedRange} rows={filteredRows} title={REPORT_TITLE} labels={OP_REPORT_PROFIT_LABELS} hideMargin />
              </>
            }
          />

          <ProfitSummaryCards totals={totals} labels={OP_REPORT_PROFIT_LABELS} hideMargin />

          <div className="flex flex-col gap-3">
            <div className="flex flex-col gap-0.5">
              <h2 className="text-sm font-semibold text-foreground">
                Billed Services <span className="font-normal text-muted-foreground">({filteredRows.length} line items)</span>
              </h2>
              <p className="text-xs text-muted-foreground">{[formatDateRangeLabel(appliedRange.from, appliedRange.to), ...filterSummary].join(' · ')}</p>
            </div>
            {isLoadingBillings ? (
              <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" />
                Loading invoices…
              </div>
            ) : filteredRows.length === 0 ? (
              <p className="text-sm text-muted-foreground">No billed services match the current filters.</p>
            ) : (
              <>
                <ProfitTable rows={pagedRows.items} labels={OP_REPORT_PROFIT_LABELS} hidden={hidden} />
                <Pagination meta={pagedRows.meta} onPageChange={setPage} />
              </>
            )}
          </div>
        </div>
      </div>

      <ProfitReportPrint
        title={REPORT_TITLE}
        range={appliedRange}
        filterSummary={filterSummary}
        rows={filteredRows}
        totals={totals}
        labels={OP_REPORT_PROFIT_LABELS}
        hidden={hidden}
      />
    </div>
  );
}
