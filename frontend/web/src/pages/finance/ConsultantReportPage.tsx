import { FileSearch, Stethoscope } from 'lucide-react';
import { useMemo, useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { useInvoicesForReportQuery } from '@/features/billing';
import { useMasterOptionsQuery } from '@/features/masters';
import {
  ConsultantProfitTable,
  ProfitSummaryCards,
  ReportFilterBar,
  AccountsNavTabs,
  filterProfitRows,
  getProfitByConsultant,
  getProfitRows,
  getProfitTotals,
  type ProfitReportFilterState,
  type ReportDateRange,
} from '@/features/reports';
import { defaultReportDateRange } from '@/lib/reportDateRange';

/**
 * Consultant Profit Report — revenue/cost/profit attributed to each consultant. Scoped to
 * Consultation-type invoice lines only, the only BillingType that carries consultant
 * attribution at all (stated in the subtitle, not silently implied). Reads
 * `billedConsultantId` rather than `consultantId` so a *paid* consultation still attributes
 * correctly — `consultantId` itself is cleared once a line item is paid (ADR-048); the whole
 * point of adding `billedConsultantId` was to make this report possible without that gap
 * silently omitting the majority of real (paid) revenue.
 *
 * Same shared filter bar and draft/applied/"click Search" pattern every other Finance report
 * uses (see ReportFilterBar's own doc comment) — Billing Type and Consultant dropdowns are
 * hidden here: Billing Type is always Consultation, and this report is already one row per
 * consultant, so filtering it down to a single one has no real use.
 */
export default function ConsultantReportPage() {
  const [draftRange, setDraftRange] = useState<ReportDateRange>(defaultReportDateRange);
  const [draftFilters, setDraftFilters] = useState<ProfitReportFilterState>({});
  const [appliedRange, setAppliedRange] = useState<ReportDateRange>(defaultReportDateRange);
  const [appliedFilters, setAppliedFilters] = useState<ProfitReportFilterState>({});
  const [hasSearched, setHasSearched] = useState(false);

  const { data: billings, isPending: isLoadingBillings } = useInvoicesForReportQuery();
  const { data: consultantOptions } = useMasterOptionsQuery('consultant');

  // Depends on consultantOptions even though getProfitRows doesn't take it as a parameter —
  // resolveItemCostPrice (called inside getProfitRows) reads the consultant reference cache
  // synchronously for Consultation lines (billed consultant's charge per consultation type),
  // same as resolveRecordLabel does for names; without this dependency, `rows` (and everything
  // derived from it below) would freeze at whatever the cache held on the very first render —
  // usually still empty — and never pick up the real figures once the priming query resolves.
  // Mirrors ProfitReportPage's identical reasoning for its own `rows` memo.
  const rows = useMemo(
    () => (hasSearched ? getProfitRows(billings ?? [], appliedRange) : []),
    [hasSearched, billings, appliedRange, consultantOptions],
  );
  const filteredRows = useMemo(() => filterProfitRows(rows, appliedFilters), [rows, appliedFilters]);
  const consultationRows = useMemo(() => filteredRows.filter((row) => row.billingType === 'Consultation'), [filteredRows]);
  const totals = useMemo(() => getProfitTotals(consultationRows), [consultationRows]);
  const byConsultant = useMemo(() => getProfitByConsultant(consultationRows), [consultationRows]);

  function handleSearch() {
    setAppliedRange(draftRange);
    setAppliedFilters(draftFilters);
    setHasSearched(true);
  }

  function handleReset() {
    const fresh = defaultReportDateRange();
    setDraftRange(fresh);
    setDraftFilters({});
    setAppliedRange(fresh);
    setAppliedFilters({});
    setHasSearched(false);
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Stethoscope}
        title="Consultant Profit Report"
        subtitle="Revenue and profit per consultant — Consultation charges only, the only billing type that carries consultant attribution."
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
            showConsultant={false}
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
              <ProfitSummaryCards totals={totals} />

              <div className="flex flex-col gap-3">
                <h2 className="text-sm font-semibold text-foreground">
                  By Consultant <span className="font-normal text-muted-foreground">({byConsultant.length} consultants)</span>
                </h2>
                {isLoadingBillings ? (
                  <p className="text-sm text-muted-foreground">Loading invoices…</p>
                ) : byConsultant.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No consultation charges match the current filters.</p>
                ) : (
                  <ConsultantProfitTable rows={byConsultant} />
                )}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
