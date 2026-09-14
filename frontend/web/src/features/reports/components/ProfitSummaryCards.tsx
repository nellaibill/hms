import { ArrowUpCircle, CircleDollarSign, Percent, TrendingUp } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { formatCurrency } from '@/features/billing';
import { cn } from '@/lib/utils';
import type { ProfitTotals } from '../profitReport';

interface ProfitSummaryCardsProps {
  totals: ProfitTotals;
}

export function ProfitSummaryCards({ totals }: ProfitSummaryCardsProps) {
  return (
    <div className="flex flex-col gap-2">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardContent className="flex items-center gap-3 py-4">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-success/15 text-success">
              <ArrowUpCircle className="h-5 w-5" />
            </span>
            <div className="flex flex-col">
              <span className="text-xs text-muted-foreground">Total Revenue</span>
              <span className="text-lg font-semibold text-foreground">{formatCurrency(totals.totalRevenue)}</span>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 py-4">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-warning/15 text-warning">
              <CircleDollarSign className="h-5 w-5" />
            </span>
            <div className="flex flex-col">
              <span className="text-xs text-muted-foreground">Total Cost</span>
              <span className="text-lg font-semibold text-foreground">{formatCurrency(totals.totalCost)}</span>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 py-4">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-accent text-accent-foreground">
              <TrendingUp className="h-5 w-5" />
            </span>
            <div className="flex flex-col">
              <span className="text-xs text-muted-foreground">Total Profit</span>
              <span className={cn('text-lg font-semibold', totals.totalProfit >= 0 ? 'text-success' : 'text-destructive')}>
                {formatCurrency(totals.totalProfit)}
              </span>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 py-4">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary/15 text-primary">
              <Percent className="h-5 w-5" />
            </span>
            <div className="flex flex-col">
              <span className="text-xs text-muted-foreground">Margin</span>
              <span className="text-lg font-semibold text-foreground">
                {totals.overallMarginPercent === null ? '—' : `${totals.overallMarginPercent}%`}
              </span>
            </div>
          </CardContent>
        </Card>
      </div>
      {totals.revenueWithUnknownCost > 0 && (
        <p className="text-xs text-muted-foreground">
          {formatCurrency(totals.revenueWithUnknownCost)} of revenue has no cost data yet (Pharmacy, Laboratory packages, a service not yet
          costed, or a consultant with no charge set for that consultation type) and isn't reflected in the figures above.
        </p>
      )}
    </div>
  );
}
