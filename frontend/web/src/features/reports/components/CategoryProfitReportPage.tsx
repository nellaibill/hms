import type { ComponentType } from 'react';
import { Loader2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { useInvoicesForReportQuery, type BillingType } from '@/features/billing';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { useMasterOptionsQuery } from '@/features/masters';
import { filterProfitRows, getProfitByTest, getProfitRows, getProfitTotals } from '../profitReport';
import { Pagination } from './Pagination';
import { paginate } from '../pagination';
import { ProfitExportButtons } from './ProfitExportButtons';
import { ProfitSummaryCards } from './ProfitSummaryCards';
import { ProfitTable } from './ProfitTable';
import { CategoryBreakdownCard } from './CategoryBreakdownCard';
import { ReportFilterBar } from './ReportFilterBar';
import { AccountsNavTabs } from './AccountsNavTabs';
import { useReportFilters } from '../useReportFilters';

const ROWS_PER_PAGE = 10;

interface CategoryProfitReportPageProps {
  billingType: BillingType;
  title: string;
  description: string;
  icon: ComponentType<{ className?: string }>;
}

/**
 * Shared page for a single-`BillingType` profit breakdown (Laboratory, Radiology) — everything
 * here is the exact same data pipeline `ProfitReportPage` (Hospital Profit Report) uses, just
 * pre-filtered to one billing type, so the two near-identical reports don't duplicate the whole
 * page — including the same live, session-remembered filters and default-range report on first
 * load (see useReportFilters' own doc comment). Deliberately OPD-billed only, same scope Hospital Profit
 * Report has: IPD-originated lab/radiology charges are folded into generic `InpatientCharge`
 * lines (ADR-066) with no catalog reference to resolve a cost from — extending this would mean
 * reworking IPD's already-shipped Final Billing charge-posting, out of scope for this pass
 * (their revenue still counts correctly in Hospital Profit Report's totals, just not broken out
 * per test here).
 */
export function CategoryProfitReportPage({ billingType, title, description, icon: Icon }: CategoryProfitReportPageProps) {
  const [page, setPage] = useState(1);
  const { range, filters, appliedRange, appliedFilters, setRange, setFilters, reset } = useReportFilters(billingType.toLowerCase(), () =>
    setPage(1),
  );

  const { data: billings, isPending: isLoadingBillings } = useInvoicesForReportQuery();

  // Same reference-cache priming ProfitReportPage does — see that file's own comment for why
  // this is required even though the hooks' `data` looks unused at a glance.
  const { data: diagnosticTestOptions } = useMasterOptionsQuery('diagnosticTest');
  const { data: departmentOptions } = useMasterOptionsQuery('department');
  const { data: consultantOptions } = useMasterOptionsQuery('consultant');
  const { data: consultationTypeOptions } = useMasterOptionsQuery('consultationType');
  const { services: radiologyServices } = useDiagnosticServices('Radiology');
  const { services: laboratoryServices } = useDiagnosticServices('Laboratory');
  usePrimeDiagnosticPackageCache();

  const rows = useMemo(() => {
    const allRows = getProfitRows(billings ?? [], appliedRange);
    return allRows.filter((row) => row.billingType === billingType);
  }, [
    billings,
    appliedRange,
    billingType,
    diagnosticTestOptions,
    departmentOptions,
    consultantOptions,
    consultationTypeOptions,
    radiologyServices,
    laboratoryServices,
  ]);
  const filteredRows = useMemo(() => filterProfitRows(rows, appliedFilters), [rows, appliedFilters]);
  const totals = useMemo(() => getProfitTotals(filteredRows), [filteredRows]);
  const byTest = useMemo(() => getProfitByTest(filteredRows), [filteredRows]);

  const pagedRows = paginate(filteredRows, page, ROWS_PER_PAGE);

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Icon}
        title={title}
        subtitle={`${description} OPD-billed data only.`}
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
            showBillingType={false}
            exportSlot={<ProfitExportButtons range={appliedRange} rows={filteredRows} />}
          />

          <ProfitSummaryCards totals={totals} />

          <CategoryBreakdownCard title={`Profit by ${billingType} Test`} rows={byTest} tone="success" maxRows={10} />

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
        </div>
      </div>
    </div>
  );
}
