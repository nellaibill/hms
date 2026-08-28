import { Receipt } from 'lucide-react';
import { useFormContext } from 'react-hook-form';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { useMasterOptionsQuery } from '@/features/masters';
import { describeBillingItem, formatCurrency, summarizeBilling, toBillingItems } from '../billingCalculations';
import type { BillingFormValues } from '../billingValidation';
import { PaymentStatusControl } from './PaymentStatusControl';

/**
 * Recomputed live from the four category cards, plus the one whole-bill Payment status
 * control — a visit is settled in a single transaction at the counter, not per category,
 * so this replaced four separate per-card Pending/Paid toggles that read as confusing.
 * Sticky on large screens so it stays in view while a long bill is being built up.
 *
 * Lines itemize by service (not just "Laboratory (2)") — reuses describeBillingItem, the
 * same resolver InvoiceDetailCard and the patient detail Billing section use, so a service
 * name reads identically everywhere it's shown.
 */
export function BillingSummaryCard() {
  const { watch, setValue } = useFormContext<BillingFormValues>();
  const values = watch();
  // Primes the Masters reference cache describeBillingItem reads from below, so Consultation
  // line items resolve to real department/consultant/type names — Consultation Billing uses
  // the dedicated department/consultant/consultationType typed API clients (via
  // DepartmentSelect/ConsultantSelect/ConsultationTypeSelect), which don't share the generic
  // Masters engine's cache the way useMasterOptionsQuery does.
  useMasterOptionsQuery('department');
  useMasterOptionsQuery('consultant');
  useMasterOptionsQuery('consultationType');
  const summary = summarizeBilling(values);
  const items = toBillingItems(values);

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
        {items.length === 0 ? (
          <p className="text-sm text-muted-foreground">No billing items added yet.</p>
        ) : (
          <div className="flex flex-col gap-2 text-sm">
            {items.map((item) => {
              const { serviceLabel } = describeBillingItem(item);
              return (
                <div key={item.id} className="flex items-center justify-between gap-3">
                  <span className="text-muted-foreground">
                    {item.billingType} — {serviceLabel}
                  </span>
                  <span className="font-medium text-foreground">{formatCurrency(item.unitPrice)}</span>
                </div>
              );
            })}
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

        <Separator />

        <PaymentStatusControl
          id="billing-payment-status"
          value={values.paymentStatus}
          onChange={(status) => setValue('paymentStatus', status, { shouldValidate: true })}
        />
      </CardContent>
    </Card>
  );
}
