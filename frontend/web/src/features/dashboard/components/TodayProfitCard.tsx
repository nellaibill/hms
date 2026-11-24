import { ArrowRight, Loader2 } from 'lucide-react';
import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardContent } from '@/components/ui/card';
import { formatCurrency, useInvoicesSinceQuery } from '@/features/billing';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { resolveRecordLabel, useMasterOptionsQuery } from '@/features/masters';
import { accountsPath, getProfitRows, onlyOpAccountBillings } from '@/features/reports';
import { groupDayWiseProfit, profitPercent } from '@/features/reports/dayWiseProfit';
import { getTodayRange } from '../frontDeskToday';

/**
 * Today's OP and Emergency profit by charge head (ADR-142, ADR-146) — revenue, consultant share, running cost and the
 * hospital's profit for Consultation, Laboratory, Radiology and the rest, worked out the same way
 * as the Day-wise Profit report it links to. Fetches only today's bills.
 */
export function TodayProfitCard() {
  const today = useMemo(() => getTodayRange(), []);
  // Report lines are dated by their UTC day (as on OP Report), so fetch from whichever of the
  // local and UTC midnight comes first.
  const fetchFrom = useMemo(() => new Date(Math.min(Date.parse(today.from), Date.parse(`${today.date}T00:00:00Z`))).toISOString(), [today]);
  const { data: bills, isPending } = useInvoicesSinceQuery(fetchFrom);

  const { data: diagnosticTestOptions } = useMasterOptionsQuery('diagnosticTest');
  const { data: consultantOptions } = useMasterOptionsQuery('consultant');
  const { data: consultationTypeOptions } = useMasterOptionsQuery('consultationType');
  const { services: radiologyServices } = useDiagnosticServices('Radiology');
  const { services: laboratoryServices } = useDiagnosticServices('Laboratory');
  const diagnosticPackages = usePrimeDiagnosticPackageCache();

  const report = useMemo(
    () => groupDayWiseProfit(getProfitRows(onlyOpAccountBillings(bills ?? []), { from: today.date, to: today.date }), (id) => resolveRecordLabel('consultant', id)),
    // eslint-disable-next-line react-hooks/exhaustive-deps -- the priming hooks' data is what makes the cost resolvers' answers change
    [bills, today, diagnosticTestOptions, consultantOptions, consultationTypeOptions, radiologyServices, laboratoryServices, diagnosticPackages],
  );
  const percent = profitPercent(report.total);

  return (
    <Card>
      <CardContent className="flex flex-col gap-4 p-5">
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <Figure label="Revenue" value={formatCurrency(report.total.revenue)} />
          <Figure label="Consultant share" value={formatCurrency(report.total.consultantShare)} />
          <Figure label="Running cost" value={formatCurrency(report.total.runningCost)} />
          <Figure label="Hospital profit" value={formatCurrency(report.total.hospitalProfit)} hint={percent === null ? undefined : `${percent}%`} emphasis />
        </div>

        {isPending ? (
          <p className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading today&apos;s bills…
          </p>
        ) : report.byHead.length === 0 ? (
          <p className="text-sm text-muted-foreground">No OP or Emergency bills yet today.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[560px] text-sm">
              <thead>
                <tr className="border-b border-border text-left text-xs text-muted-foreground">
                  <th scope="col" className="py-2 pr-3 font-medium">Charge head</th>
                  <th scope="col" className="px-3 py-2 text-right font-medium">Revenue</th>
                  <th scope="col" className="px-3 py-2 text-right font-medium">Consultant share</th>
                  <th scope="col" className="px-3 py-2 text-right font-medium">Running cost</th>
                  <th scope="col" className="py-2 pl-3 text-right font-medium">Hospital profit</th>
                </tr>
              </thead>
              <tbody>
                {report.byHead.map((head) => (
                  <tr key={head.head} className="border-b border-border/60 last:border-0">
                    <th scope="row" className="py-2 pr-3 text-left font-medium">
                      {head.label}
                      <span className="ml-1.5 text-xs font-normal text-muted-foreground">· {head.figures.lines}</span>
                    </th>
                    <td className="px-3 py-2 text-right tabular-nums">{formatCurrency(head.figures.revenue)}</td>
                    <td className="px-3 py-2 text-right tabular-nums text-muted-foreground">{head.figures.consultantShare > 0 ? formatCurrency(head.figures.consultantShare) : '—'}</td>
                    <td className="px-3 py-2 text-right tabular-nums text-muted-foreground">{head.figures.runningCost > 0 ? formatCurrency(head.figures.runningCost) : '—'}</td>
                    <td className="py-2 pl-3 text-right font-semibold tabular-nums text-success">{formatCurrency(head.figures.hospitalProfit)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {report.total.notCostedRevenue > 0 && (
          <p className="text-xs text-muted-foreground">{formatCurrency(report.total.notCostedRevenue)} of today&apos;s revenue has no cost set yet and isn&apos;t in the profit.</p>
        )}

        <Link to={accountsPath('op', 'reports/day-wise')} className="inline-flex items-center gap-1 self-start text-sm font-medium text-primary hover:underline">
          Open Day-wise Profit
          <ArrowRight aria-hidden="true" className="h-4 w-4" />
        </Link>
      </CardContent>
    </Card>
  );
}

function Figure({ label, value, hint, emphasis = false }: { label: string; value: string; hint?: string; emphasis?: boolean }) {
  return (
    <div className="flex flex-col gap-0.5 rounded-lg bg-muted/60 px-3 py-2">
      <span className="text-xs text-muted-foreground">{label}</span>
      <span className={emphasis ? 'text-lg font-semibold text-success' : 'text-lg font-semibold text-foreground'}>
        {value}
        {hint && <span className="ml-1.5 text-xs font-normal text-muted-foreground">{hint}</span>}
      </span>
    </div>
  );
}
