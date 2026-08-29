import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { formatCurrency } from '@/features/billing';
import type { BreakdownRow } from '../incomeExpenseReport';

interface CategoryBreakdownCardProps {
  title: string;
  rows: BreakdownRow[];
  /** Bar color token — 'success' for income (matches ReportSummaryCards' Total Income icon),
   * 'destructive' for expenses (matches its Total Expenses icon). */
  tone: 'success' | 'destructive';
}

/** A quick per-category breakdown (income by billing type, expenses by category) — each row's
 * bar width is proportional to the largest row in the same list, so the biggest contributor is
 * obvious at a glance without needing a full chart library for one bar per category. */
export function CategoryBreakdownCard({ title, rows, tone }: CategoryBreakdownCardProps) {
  const max = Math.max(...rows.map((row) => row.amount), 1);
  const barClass = tone === 'success' ? 'bg-success' : 'bg-destructive';

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm font-semibold">{title}</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        {rows.length === 0 ? (
          <p className="text-sm text-muted-foreground">No data in this period.</p>
        ) : (
          rows.map((row) => (
            <div key={row.label} className="flex flex-col gap-1">
              <div className="flex items-center justify-between gap-3 text-sm">
                <span className="text-foreground">{row.label}</span>
                <span className="font-medium text-foreground">{formatCurrency(row.amount)}</span>
              </div>
              <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
                <div className={`h-full rounded-full ${barClass}`} style={{ width: `${(row.amount / max) * 100}%` }} />
              </div>
            </div>
          ))
        )}
      </CardContent>
    </Card>
  );
}
