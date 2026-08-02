import { ArrowDownCircle, ArrowUpCircle, Scale } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { formatCurrency } from '@/features/billing';
import { cn } from '@/lib/utils';
import type { ReportTotals } from '../incomeExpenseReport';

interface ReportSummaryCardsProps {
  totals: ReportTotals;
}

export function ReportSummaryCards({ totals }: ReportSummaryCardsProps) {
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
      <Card>
        <CardContent className="flex items-center gap-3 py-4">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-success/15 text-success">
            <ArrowUpCircle className="h-5 w-5" />
          </span>
          <div className="flex flex-col">
            <span className="text-xs text-muted-foreground">Total Income</span>
            <span className="text-lg font-semibold text-foreground">{formatCurrency(totals.totalIncome)}</span>
          </div>
        </CardContent>
      </Card>
      <Card>
        <CardContent className="flex items-center gap-3 py-4">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-destructive/15 text-destructive">
            <ArrowDownCircle className="h-5 w-5" />
          </span>
          <div className="flex flex-col">
            <span className="text-xs text-muted-foreground">Total Expenses</span>
            <span className="text-lg font-semibold text-foreground">{formatCurrency(totals.totalExpense)}</span>
          </div>
        </CardContent>
      </Card>
      <Card>
        <CardContent className="flex items-center gap-3 py-4">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-accent text-accent-foreground">
            <Scale className="h-5 w-5" />
          </span>
          <div className="flex flex-col">
            <span className="text-xs text-muted-foreground">Net</span>
            <span className={cn('text-lg font-semibold', totals.net >= 0 ? 'text-success' : 'text-destructive')}>
              {formatCurrency(totals.net)}
            </span>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
