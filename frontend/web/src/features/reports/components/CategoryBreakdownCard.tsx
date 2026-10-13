import { ChevronDown, ChevronUp } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { formatCurrency } from '@/features/billing';
import type { BreakdownRow } from '../incomeExpenseReport';

interface CategoryBreakdownCardProps {
  title: string;
  rows: BreakdownRow[];
  /** Bar color token — 'success' for income (matches ReportSummaryCards' Total Income icon),
   * 'destructive' for expenses (matches its Total Expenses icon). */
  tone: 'success' | 'destructive';
  /** Caps how many rows render before falling back to a "+N more" toggle — without this, a list
   * with many more entries than its sibling card in the same grid row (e.g. Profit Report's
   * "Profit by Service" next to "Profit by Billing Type") both dominates the page and, since
   * a CSS grid row stretches every cell to the tallest one, leaves the shorter card with a big
   * empty gap. Rows are expected pre-sorted by amount descending (every caller already sorts
   * this way), so capping never hides the most significant contributors. Clicking the toggle
   * shows the rest in place ("Show less" collapses it again) — the cap is only the default view,
   * the full list is always one click away. */
  maxRows?: number;
}

/** A quick per-category breakdown (income by billing type, expenses by category) — each row's
 * bar width is proportional to the largest row in the same list, so the biggest contributor is
 * obvious at a glance without needing a full chart library for one bar per category. */
export function CategoryBreakdownCard({ title, rows, tone, maxRows }: CategoryBreakdownCardProps) {
  const [expanded, setExpanded] = useState(false);
  const max = Math.max(...rows.map((row) => row.amount), 1);
  const barClass = tone === 'success' ? 'bg-success' : 'bg-destructive';
  const overflowCount = maxRows ? Math.max(rows.length - maxRows, 0) : 0;
  const visibleRows = overflowCount > 0 && !expanded ? rows.slice(0, maxRows) : rows;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm font-semibold">{title}</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        {rows.length === 0 ? (
          <p className="text-sm text-muted-foreground">No data in this period.</p>
        ) : (
          <>
            {visibleRows.map((row) => (
              <div key={row.label} className="flex flex-col gap-1">
                <div className="flex items-center justify-between gap-3 text-sm">
                  <span className="text-foreground">{row.label}</span>
                  <span className="font-medium text-foreground">{formatCurrency(row.amount)}</span>
                </div>
                <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
                  <div className={`h-full rounded-full ${barClass}`} style={{ width: `${(row.amount / max) * 100}%` }} />
                </div>
              </div>
            ))}
            {overflowCount > 0 && (
              <button
                type="button"
                aria-expanded={expanded}
                onClick={() => setExpanded((prev) => !prev)}
                className="inline-flex w-fit items-center gap-0.5 text-xs font-medium text-primary hover:underline"
              >
                {expanded ? (
                  <>
                    Show less <ChevronUp className="h-3.5 w-3.5" />
                  </>
                ) : (
                  <>
                    +{overflowCount} more <ChevronDown className="h-3.5 w-3.5" />
                  </>
                )}
              </button>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}
