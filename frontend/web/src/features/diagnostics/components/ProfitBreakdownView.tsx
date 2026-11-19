import { cn } from '@/lib/utils';
import { formatRupees, isLowProfit, LOW_PROFIT_PERCENT, type ProfitBreakdown } from '../tariff';

interface ProfitBreakdownViewProps {
  breakdown: ProfitBreakdown;
  /** What the running cost is called for this kind (e.g. "Lab charge" for an outsourced test). */
  runningCostLabel?: string;
  /** Radiology shows the doctor's share and the hospital's income; lab tests and packages don't. */
  showDoctorShare?: boolean;
}

/**
 * The "worked out for you" half of the price-and-profit cards (ADR-140): a bar splitting the
 * price into doctor's share, running cost and profit, then the profit and profit % (of the
 * price, as in the tariff sheet). Flags a profit below LOW_PROFIT_PERCENT and a loss.
 */
export function ProfitBreakdownView({ breakdown, runningCostLabel = 'Running cost', showDoctorShare = false }: ProfitBreakdownViewProps) {
  const { price, doctorShare, runningCost, hospitalIncome, profit, profitPercent, isCosted } = breakdown;
  const width = (value: number) => (price > 0 ? `${Math.max(0, Math.min(100, (value / price) * 100))}%` : '0%');
  const isLoss = isCosted && profit < 0;
  const barLabel = showDoctorShare
    ? `Of ${formatRupees(price)}: doctor's share ${formatRupees(doctorShare)}, ${runningCostLabel.toLowerCase()} ${formatRupees(runningCost)}, profit ${formatRupees(profit)}`
    : `Of ${formatRupees(price)}: ${runningCostLabel.toLowerCase()} ${formatRupees(runningCost)}, profit ${formatRupees(profit)}`;

  return (
    <div className="flex flex-col gap-3 border-t border-dashed border-border pt-4">
      <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Worked out for you</span>

      {isCosted ? (
        <>
          <div role="img" aria-label={barLabel} className="flex h-3 overflow-hidden rounded-full bg-muted">
            {showDoctorShare && <span className="block bg-amber-600" style={{ width: width(doctorShare) }} />}
            <span className="block bg-slate-500" style={{ width: width(runningCost) }} />
            <span className="block bg-blue-700" style={{ width: width(Math.max(profit, 0)) }} />
          </div>
          <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-foreground">
            {showDoctorShare && <LegendItem colorClass="bg-amber-600" label={`Doctor ${formatRupees(doctorShare)}`} />}
            <LegendItem colorClass="bg-slate-500" label={`${runningCostLabel} ${formatRupees(runningCost)}`} />
            <LegendItem colorClass="bg-blue-700" label={`Profit ${formatRupees(profit)}`} />
          </div>
        </>
      ) : (
        <p className="text-sm text-muted-foreground">Enter the {showDoctorShare ? "doctor's share or running cost" : 'running cost'} to see the profit.</p>
      )}

      <dl className={cn('grid gap-2', showDoctorShare ? 'grid-cols-3' : 'grid-cols-2')}>
        {showDoctorShare && (
          <Stat label="Hospital income" value={formatRupees(hospitalIncome)} hint="price − doctor" />
        )}
        <Stat label="Profit" value={isCosted ? formatRupees(profit) : '—'} emphasis />
        <Stat label="Profit %" value={isCosted && profitPercent !== null ? `${profitPercent}%` : '—'} hint="of price" emphasis />
      </dl>

      {isLoss ? (
        <p role="status" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          The cost is higher than the price — the hospital loses {formatRupees(-profit)} each time.
        </p>
      ) : (
        isLowProfit(breakdown) && (
          <p role="status" className="rounded-md bg-amber-100 px-3 py-2 text-sm text-amber-900 dark:bg-amber-950 dark:text-amber-200">
            Below {LOW_PROFIT_PERCENT}% profit — flagged in the Price List.
          </p>
        )
      )}
    </div>
  );
}

function LegendItem({ colorClass, label }: { colorClass: string; label: string }) {
  return (
    <span className="inline-flex items-center gap-1.5">
      <span aria-hidden="true" className={cn('h-2.5 w-2.5 rounded-sm', colorClass)} />
      {label}
    </span>
  );
}

function Stat({ label, value, hint, emphasis = false }: { label: string; value: string; hint?: string; emphasis?: boolean }) {
  return (
    <div className={cn('rounded-lg px-3 py-2', emphasis ? 'bg-blue-50 dark:bg-blue-950/40' : 'bg-muted')}>
      <dt className="text-xs text-muted-foreground">{label}</dt>
      <dd className={cn('text-lg font-bold tabular-nums', emphasis && 'text-blue-900 dark:text-blue-200')}>{value}</dd>
      {hint && <dd className="text-[11px] text-muted-foreground">{hint}</dd>}
    </div>
  );
}
