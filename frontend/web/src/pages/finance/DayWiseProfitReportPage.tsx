import { CalendarRange, Loader2 } from 'lucide-react';
import { useMemo } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { formatCurrency, useInvoicesForReportQuery } from '@/features/billing';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { resolveRecordLabel, useMasterOptionsQuery } from '@/features/masters';
import { AccountsNavTabs, filterProfitRows, getProfitRows, onlyOpVisitBillings, ReportFilterBar, useReportFilters } from '@/features/reports';
import { DayWiseProfitExportButtons } from '@/features/reports/components/DayWiseProfitExportButtons';
import { DayWiseProfitTable, FigureCells } from '@/features/reports/components/DayWiseProfitTable';
import { groupDayWiseProfit, profitPercent } from '@/features/reports/dayWiseProfit';

const REPORT_TITLE = 'Day-wise Profit';

/**
 * Day-wise Profit (ADR-142) — for the admin and the front desk alike: every OP bill line in the
 * range, grouped by day, then by the billed consultant, then by charge head (Consultation,
 * Laboratory, Radiology, Procedure, Injection, File…). Each level shows what was billed, the
 * consultant's share, the running cost and the profit the hospital keeps — the same rules as
 * OP Report (ADR-124/141). Uses the shared, session-remembered report filters.
 */
export default function DayWiseProfitReportPage() {
  const { range, filters, appliedRange, appliedFilters, setRange, setFilters, reset } = useReportFilters('day-wise-profit');

  const { data: billings, isPending: isLoadingBillings } = useInvoicesForReportQuery();

  // Each hook primes a reference cache that the cost and label resolvers read synchronously;
  // listing their data in the memo below recomputes the rows once they load (see OP Report).
  const { data: diagnosticTestOptions } = useMasterOptionsQuery('diagnosticTest');
  const { data: departmentOptions } = useMasterOptionsQuery('department');
  const { data: consultantOptions } = useMasterOptionsQuery('consultant');
  const { data: consultationTypeOptions } = useMasterOptionsQuery('consultationType');
  const { services: radiologyServices } = useDiagnosticServices('Radiology');
  const { services: laboratoryServices } = useDiagnosticServices('Laboratory');
  const diagnosticPackages = usePrimeDiagnosticPackageCache();

  const rows = useMemo(
    () => getProfitRows(onlyOpVisitBillings(billings ?? []), appliedRange),
    // eslint-disable-next-line react-hooks/exhaustive-deps -- the priming hooks' data is what makes the resolvers' answers change
    [billings, appliedRange, diagnosticTestOptions, departmentOptions, consultantOptions, consultationTypeOptions, radiologyServices, laboratoryServices, diagnosticPackages],
  );
  const filteredRows = useMemo(() => filterProfitRows(rows, appliedFilters), [rows, appliedFilters]);
  const report = useMemo(
    () => groupDayWiseProfit(filteredRows, (id) => resolveRecordLabel('consultant', id)),
    // eslint-disable-next-line react-hooks/exhaustive-deps -- consultant names come from the primed cache
    [filteredRows, consultantOptions],
  );
  const percent = profitPercent(report.total);

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={CalendarRange}
        title={REPORT_TITLE}
        subtitle="Every OP bill by day, then consultant, then charge head — what was billed, the consultant's share, the running cost and the hospital's profit."
        backTo="/finance/accounts"
        backLabel="Back to Accounts and Finance"
      />

      <div className="flex flex-1 flex-col gap-4 p-4 sm:p-6 lg:p-8">
        <AccountsNavTabs />

        <ReportFilterBar
          range={range}
          filters={filters}
          onRangeChange={setRange}
          onFiltersChange={setFilters}
          onReset={reset}
          exportSlot={<DayWiseProfitExportButtons range={appliedRange} report={report} title={REPORT_TITLE} />}
        />

        <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
          <SummaryTile label="Revenue" value={formatCurrency(report.total.revenue)} hint={`${report.total.lines} bill lines`} />
          <SummaryTile label="Consultant share" value={formatCurrency(report.total.consultantShare)} hint="Consultations, procedures, injections, files" />
          <SummaryTile label="Running cost" value={formatCurrency(report.total.runningCost)} hint="Lab tests and scans" />
          <SummaryTile
            label="Hospital profit"
            value={formatCurrency(report.total.hospitalProfit)}
            hint={percent === null ? 'Nothing costed yet' : `${percent}% of costed revenue`}
            tone="success"
          />
        </div>
        {report.total.notCostedRevenue > 0 && (
          <p className="text-xs text-muted-foreground">
            {formatCurrency(report.total.notCostedRevenue)} of revenue has no cost set yet (a consultant with no charge for that consultation type, or a service with
            no running cost) and isn&apos;t in the shares or profit.
          </p>
        )}

        {isLoadingBillings ? (
          <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading bills…
          </div>
        ) : report.days.length === 0 ? (
          <p className="text-sm text-muted-foreground">No OP bills match the current filters.</p>
        ) : (
          <>
            <section aria-labelledby="dwp-heads" className="flex flex-col gap-2">
              <h2 id="dwp-heads" className="text-sm font-semibold text-foreground">
                By charge head
              </h2>
              <div className="overflow-x-auto rounded-lg border border-border">
                <table className="w-full min-w-[900px] text-sm">
                  <thead className="bg-muted/60 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
                    <tr>
                      <th scope="col" className="px-4 py-2">Charge head</th>
                      <th scope="col" className="px-3 py-2 text-right">Lines</th>
                      <th scope="col" className="px-3 py-2 text-right">Amount</th>
                      <th scope="col" className="px-3 py-2 text-right">Discount</th>
                      <th scope="col" className="px-3 py-2 text-right">Revenue</th>
                      <th scope="col" className="px-3 py-2 text-right">Consultant share</th>
                      <th scope="col" className="px-3 py-2 text-right">Running cost</th>
                      <th scope="col" className="px-3 py-2 text-right">Hospital profit</th>
                      <th scope="col" className="px-3 py-2 text-right">Profit %</th>
                    </tr>
                  </thead>
                  <tbody>
                    {report.byHead.map((head) => (
                      <tr key={head.head} className="border-t border-border">
                        <th scope="row" className="px-4 py-2 text-left font-medium">
                          {head.label}
                        </th>
                        <FigureCells figures={head.figures} />
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>

            <section aria-labelledby="dwp-days" className="flex flex-col gap-2">
              <h2 id="dwp-days" className="text-sm font-semibold text-foreground">
                By day and consultant
              </h2>
              <DayWiseProfitTable report={report} />
            </section>
          </>
        )}
      </div>
    </div>
  );
}

function SummaryTile({ label, value, hint, tone }: { label: string; value: string; hint: string; tone?: 'success' }) {
  return (
    <div className="flex flex-col gap-0.5 rounded-lg border border-border bg-card p-4">
      <span className="text-xs text-muted-foreground">{label}</span>
      <span className={tone === 'success' ? 'text-lg font-semibold text-success' : 'text-lg font-semibold text-foreground'}>{value}</span>
      <span className="text-[11px] text-muted-foreground">{hint}</span>
    </div>
  );
}
