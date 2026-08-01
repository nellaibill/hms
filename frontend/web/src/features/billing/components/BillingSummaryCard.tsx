import { Receipt } from 'lucide-react';
import { useFormContext } from 'react-hook-form';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { formatCurrency, summarizeBilling } from '../billingCalculations';
import type { BillingFormValues } from '../billingValidation';

/** Read-only — always visible, recomputed live from the four category cards. Sticky on large screens so it stays in view while a long bill is being built up. */
export function BillingSummaryCard() {
  const { watch } = useFormContext<BillingFormValues>();
  const values = watch();
  const summary = summarizeBilling(values);
  const activeLines = summary.lines.filter((line) => line.active);

  return (
    <Card className="lg:sticky lg:top-20">
      <CardHeader className="flex-row items-center gap-3 space-y-0">
        <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-accent text-accent-foreground">
          <Receipt className="h-5 w-5" />
        </span>
        <div className="flex flex-col gap-1">
          <CardTitle className="text-lg">Billing Summary</CardTitle>
          <CardDescription>Updates automatically as billing items change.</CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-3 pt-0">
        {activeLines.length === 0 ? (
          <p className="text-sm text-muted-foreground">No billing items added yet.</p>
        ) : (
          <div className="flex flex-col gap-2 text-sm">
            {activeLines.map((line) => (
              <div key={line.billingType} className="flex items-center justify-between gap-3">
                <span className="text-muted-foreground">
                  {line.label}
                  {line.count > 1 ? ` (${line.count})` : ''}
                </span>
                <span className="font-medium text-foreground">{formatCurrency(line.charge)}</span>
              </div>
            ))}
          </div>
        )}

        <Separator />

        <div className="flex flex-col gap-1.5 text-sm">
          <div className="flex items-center justify-between gap-3">
            <span className="text-muted-foreground">Gross total</span>
            <span className="font-medium text-foreground">{formatCurrency(summary.grossTotal)}</span>
          </div>
          <div className="flex items-center justify-between gap-3">
            <span className="text-muted-foreground">Discount</span>
            <span className="font-medium text-destructive">
              {summary.discountTotal > 0 ? `- ${formatCurrency(summary.discountTotal)}` : formatCurrency(0)}
            </span>
          </div>
          <div className="mt-1 flex items-center justify-between gap-3 border-t border-border pt-2">
            <span className="text-base font-semibold text-foreground">Net amount</span>
            <span className="text-base font-bold text-primary">{formatCurrency(summary.netTotal)}</span>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
