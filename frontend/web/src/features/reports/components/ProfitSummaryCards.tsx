import { ArrowUpCircle, CircleDollarSign, Percent, Tag, TrendingUp } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { formatCurrency } from '@/features/billing';
import { cn } from '@/lib/utils';
import { DEFAULT_PROFIT_LABELS, type ProfitLabels, type ProfitTotals } from '../profitReport';

interface ProfitSummaryCardsProps {
  totals: ProfitTotals;
  /** OP Report passes OP_REPORT_PROFIT_LABELS (ADR-124); other reports keep Cost/Profit. */
  labels?: ProfitLabels;
  /** Leaves the Margin card out — OP Report doesn't show margin (ADR-126). */
  hideMargin?: boolean;
}

export function ProfitSummaryCards({ totals, labels = DEFAULT_PROFIT_LABELS, hideMargin = false }: ProfitSummaryCardsProps) {
  return (
    <div className="flex flex-col gap-2">
      <div className={cn('grid grid-cols-1 gap-4 sm:grid-cols-2', hideMargin ? 'xl:grid-cols-4' : 'lg:grid-cols-3 xl:grid-cols-5')}>
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
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-destructive/15 text-destructive">
              <Tag className="h-5 w-5" />
            </span>
            <div className="flex flex-col">
              <span className="text-xs text-muted-foreground">Total Discount</span>
              <span className="text-lg font-semibold text-foreground">{formatCurrency(totals.totalDiscount)}</span>
              {totals.totalDiscount > 0 && (
                <span className="text-xs text-muted-foreground">on {formatCurrency(totals.totalCharge)} charged</span>
              )}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-3 py-4">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-warning/15 text-warning">
              <CircleDollarSign className="h-5 w-5" />
            </span>
            <div className="flex flex-col">
              <span className="text-xs text-muted-foreground">Total {labels.cost}</span>
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
              <span className="text-xs text-muted-foreground">Total {labels.profit}</span>
              <span className={cn('text-lg font-semibold', totals.totalProfit >= 0 ? 'text-success' : 'text-destructive')}>
                {formatCurrency(totals.totalProfit)}
              </span>
            </div>
          </CardContent>
        </Card>
        {!hideMargin && (
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
        )}
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
