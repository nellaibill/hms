import { Stethoscope } from 'lucide-react';
import { useMemo, useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { useInvoicesForReportQuery } from '@/features/billing';
import { useMasterOptionsQuery } from '@/features/masters';
import {
  ConsultantProfitTable,
  ProfitSummaryCards,
  ReportDateRangeFilter,
  AccountsNavTabs,
  getProfitByConsultant,
  getProfitRows,
  getProfitTotals,
  type ReportDateRange,
} from '@/features/reports';

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
 * Consultant Profit Report — revenue/cost/profit attributed to each consultant. Scoped to
 * Consultation-type invoice lines only, the only BillingType that carries consultant
 * attribution at all (stated in the subtitle, not silently implied). Reads
 * `billedConsultantId` rather than `consultantId` so a *paid* consultation still attributes
 * correctly — `consultantId` itself is cleared once a line item is paid (ADR-048); the whole
 * point of adding `billedConsultantId` was to make this report possible without that gap
 * silently omitting the majority of real (paid) revenue.
 */
export default function ConsultantReportPage() {
  const [range, setRange] = useState<ReportDateRange>(defaultRange);

  const { data: billings, isPending: isLoadingBillings } = useInvoicesForReportQuery();
  const { data: consultantOptions } = useMasterOptionsQuery('consultant');

  // Depends on consultantOptions even though getProfitRows doesn't take it as a parameter —
  // resolveItemCostPrice (called inside getProfitRows) reads the consultant reference cache
  // synchronously for Consultation lines (billed consultant's charge per consultation type),
  // same as resolveRecordLabel does for names; without this dependency, `rows` (and everything
  // derived from it below) would freeze at whatever the cache held on the very first render —
  // usually still empty — and never pick up the real figures once the priming query resolves.
  // Mirrors ProfitReportPage's identical reasoning for its own `rows` memo.
  const rows = useMemo(() => getProfitRows(billings ?? [], range), [billings, range, consultantOptions]);
  const consultationRows = useMemo(() => rows.filter((row) => row.billingType === 'Consultation'), [rows]);
  const totals = useMemo(() => getProfitTotals(consultationRows), [consultationRows]);
  const byConsultant = useMemo(() => getProfitByConsultant(consultationRows), [consultationRows]);

  function handleRangeChange(next: ReportDateRange) {
    setRange(next);
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
          <ReportDateRangeFilter range={range} onChange={handleRangeChange} />

          <ProfitSummaryCards totals={totals} />

          <div className="flex flex-col gap-3">
            <h2 className="text-sm font-semibold text-foreground">
              By Consultant <span className="font-normal text-muted-foreground">({byConsultant.length} consultants)</span>
            </h2>
            {isLoadingBillings ? (
              <p className="text-sm text-muted-foreground">Loading invoices…</p>
            ) : byConsultant.length === 0 ? (
              <p className="text-sm text-muted-foreground">No consultation charges in this period.</p>
            ) : (
              <ConsultantProfitTable rows={byConsultant} />
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
