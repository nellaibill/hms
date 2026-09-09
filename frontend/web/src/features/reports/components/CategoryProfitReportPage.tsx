import type { ComponentType } from 'react';
import { ArrowLeft, Loader2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useInvoicesForReportQuery, type BillingType } from '@/features/billing';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { useMasterOptionsQuery } from '@/features/masters';
import { getProfitByTest, getProfitRows, getProfitTotals } from '../profitReport';
import { Pagination } from './Pagination';
import { paginate } from '../pagination';
import { ProfitExportButtons } from './ProfitExportButtons';
import { ProfitSummaryCards } from './ProfitSummaryCards';
import { ProfitTable } from './ProfitTable';
import { CategoryBreakdownCard } from './CategoryBreakdownCard';
import { ReportDateRangeFilter } from './ReportDateRangeFilter';
import { ReportNavTabs } from './ReportNavTabs';
import type { ReportDateRange } from '../types';

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
 * page. Deliberately OPD-billed only, same scope Hospital Profit Report has: IPD-originated
 * lab/radiology charges are folded into generic `InpatientCharge` lines (ADR-066) with no
 * catalog reference to resolve a cost from — extending this would mean reworking IPD's already-
 * shipped Final Billing charge-posting, out of scope for this pass (their revenue still counts
 * correctly in Hospital Profit Report's totals, just not broken out per test here).
 */
export function CategoryProfitReportPage({ billingType, title, description, icon: Icon }: CategoryProfitReportPageProps) {
  const [range, setRange] = useState<ReportDateRange>(defaultRange);
  const [page, setPage] = useState(1);

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
    const allRows = getProfitRows(billings ?? [], range);
    return allRows.filter((row) => row.billingType === billingType);
  }, [billings, range, billingType, diagnosticTestOptions, departmentOptions, consultantOptions, consultationTypeOptions, radiologyServices, laboratoryServices]);
  const totals = useMemo(() => getProfitTotals(rows), [rows]);
  const byTest = useMemo(() => getProfitByTest(rows), [rows]);

  const pagedRows = paginate(rows, page, ROWS_PER_PAGE);

  function handleRangeChange(next: ReportDateRange) {
    setRange(next);
    setPage(1);
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/finance/accounts" className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to Accounts and Finance
        </Link>
      </div>

      <div className="relative mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Icon className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">{title}</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">{description} OPD-billed data only.</p>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <ReportNavTabs />

        <div className="flex w-full flex-col gap-4">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <ReportDateRangeFilter range={range} onChange={handleRangeChange} />
            <ProfitExportButtons range={range} rows={rows} />
          </div>

          <ProfitSummaryCards totals={totals} />

          <CategoryBreakdownCard title={`Profit by ${billingType} Test`} rows={byTest} tone="success" maxRows={10} />

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
