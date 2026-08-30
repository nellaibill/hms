import { Receipt } from 'lucide-react';
import { useEffect, useRef } from 'react';
import { Controller, useFormContext } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { useDiagnosticServices, usePrimeDiagnosticPackageCache } from '@/features/diagnostics';
import { Field } from '@/features/patients/components/FormSection';
import { useMasterOptionsQuery } from '@/features/masters';
import { describeBillingItem, formatCurrency, summarizeBilling, toBillingItems } from '../billingCalculations';
import type { BillingFormValues } from '../billingValidation';
import { PAYMENT_METHODS, PAYMENT_METHOD_LABELS } from '../types';

/**
 * Recomputed live from the four category cards, plus a whole-bill "Collect Payment" block
 * (Amount Received / Payment Mode / Reference No.) — a visit is settled in a single
 * transaction at the counter, not per category, so this is one shared block rather than four
 * separate per-card controls. Sticky on large screens so it stays in view while a long bill is
 * being built up.
 *
 * Payment is mandatory: Amount Received auto-fills to Net Payable the moment there's something
 * to bill (still editable, e.g. for a cash-tendered amount that computes change), and the form
 * schema (billingValidation.ts) rejects a save unless the full amount and a payment mode are
 * present — there's no "save as Pending, collect later" path from this screen.
 *
 * Lines itemize by service (not just "Laboratory (2)") — reuses describeBillingItem, the
 * same resolver InvoiceDetailCard and the patient detail Billing section use, so a service
 * name reads identically everywhere it's shown.
 *
 * The save action itself (onSave/isSaving/saveError) also renders here, inside this sticky
 * card, rather than below the four billing category cards — this card's height doesn't change
 * when a category expands, so an action anchored here stays put instead of jumping down the
 * page every time a section opens. Optional: InvoiceCreatePage is the only caller that wires
 * these up today (BillingStep's own doc comment has the details); omitting onSave renders the
 * summary with no save action at all.
 */
interface BillingSummaryCardProps {
  onSave?: () => void;
  isSaving?: boolean;
  saveError?: string | null;
  saveErrorDetails?: string[];
}

export function BillingSummaryCard({ onSave, isSaving, saveError, saveErrorDetails }: BillingSummaryCardProps) {
  const {
    watch,
    control,
    setValue,
    formState: { errors },
  } = useFormContext<BillingFormValues>();
  const values = watch();
  // Primes the Masters reference cache describeBillingItem reads from below, so Consultation
  // line items resolve to real department/consultant/type names — Consultation Billing uses
  // the dedicated department/consultant/consultationType typed API clients (via
  // DepartmentSelect/ConsultantSelect/ConsultationTypeSelect), which don't share the generic
  // Masters engine's cache the way useMasterOptionsQuery does.
  useMasterOptionsQuery('department');
  useMasterOptionsQuery('consultant');
  useMasterOptionsQuery('consultationType');
  // Radiology/Laboratory now read the new typed DiagnosticService/DiagnosticPackage catalogs
  // (see billingCalculations.ts's describeBillingItem) — these prime that cache the same way.
  useDiagnosticServices('Radiology');
  useDiagnosticServices('Laboratory');
  usePrimeDiagnosticPackageCache();
  const summary = summarizeBilling(values);
  const items = toBillingItems(values);

  // Payment is mandatory before this invoice can be saved (the patient isn't sent through to
  // the consultant until billing is settled at the counter), so Amount Received defaults to the
  // exact Net Payable as soon as there's something to bill — staff only need to type over it for
  // a cash-tendered amount that differs (to compute change), not to enter the number from
  // scratch every time. Stops re-asserting itself the moment the field is manually edited, so it
  // never fights typing.
  const amountManuallyEditedRef = useRef(false);
  useEffect(() => {
    if (!amountManuallyEditedRef.current) {
      setValue('amountReceived', summary.netTotal, { shouldValidate: true });
    }
  }, [summary.netTotal, setValue]);

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
          {summary.lines
            .filter((line) => line.active)
            .map((line) => (
              <div key={line.billingType} className="flex items-center justify-between gap-3">
                <span className="text-muted-foreground">{line.label} Total</span>
                <span className="font-medium text-foreground">{formatCurrency(line.net)}</span>
              </div>
            ))}
          <div className="mt-1 flex items-center justify-between gap-3 border-t border-border pt-2">
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
            <span className="text-base font-semibold text-foreground">Net Payable</span>
            <span className="text-base font-bold text-primary">{formatCurrency(summary.netTotal)}</span>
          </div>
        </div>

        <Separator />

        <div className="flex flex-col gap-3">
          <span className="text-sm font-semibold text-foreground">Collect Payment</span>
          {items.length > 0 && (
            <p className="text-xs text-muted-foreground">
              Full payment is required before this invoice can be saved — the patient proceeds to the consultant only once billing is settled here.
            </p>
          )}

          <Field label="Amount Received (₹)" htmlFor="billing-amount-received" error={errors.amountReceived?.message} className="flex flex-col gap-1">
            <Controller
              name="amountReceived"
              control={control}
              render={({ field }) => (
                <Input
                  id="billing-amount-received"
                  type="number"
                  min={0}
                  inputMode="decimal"
                  // Showing literal 0 here would make it uneditable — deleting it just
                  // re-renders back to "0" every keystroke (field.value is never truly empty),
                  // so a receptionist could never clear it to type a real amount. Blank reads
                  // the same as 0 to every calculation/validation already (netTotal check,
                  // Change display, the create request), so hiding it costs nothing.
                  value={field.value === 0 ? '' : field.value}
                  onChange={(e) => {
                    amountManuallyEditedRef.current = true;
                    field.onChange(e.target.value === '' ? 0 : Number(e.target.value));
                  }}
                />
              )}
            />
          </Field>

          {items.length > 0 && (
            <>
              <Field label="Payment Mode" htmlFor="billing-payment-mode" error={errors.paymentMode?.message} className="flex flex-col gap-1">
                <Controller
                  name="paymentMode"
                  control={control}
                  render={({ field }) => (
                    <Select value={field.value ?? ''} onValueChange={field.onChange}>
                      <SelectTrigger id="billing-payment-mode">
                        <SelectValue placeholder="Select payment mode" />
                      </SelectTrigger>
                      <SelectContent>
                        {PAYMENT_METHODS.map((option) => (
                          <SelectItem key={option} value={option}>
                            {PAYMENT_METHOD_LABELS[option]}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </Field>

              <Field label="Reference / Transaction No. (Optional)" htmlFor="billing-reference-number" className="flex flex-col gap-1">
                <Controller
                  name="referenceNumber"
                  control={control}
                  render={({ field }) => (
                    <Input id="billing-reference-number" placeholder="Enter reference or transaction number" value={field.value} onChange={field.onChange} />
                  )}
                />
              </Field>

              {values.amountReceived > summary.netTotal && (
                <div className="flex items-center justify-between gap-3 text-sm">
                  <span className="font-medium text-success">Change (to be returned)</span>
                  <span className="font-bold text-success">{formatCurrency(values.amountReceived - summary.netTotal)}</span>
                </div>
              )}
            </>
          )}
        </div>

        {onSave && (
          <>
            <Separator />

            {(saveError || (saveErrorDetails && saveErrorDetails.length > 0)) && (
              <div role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {saveError && <p>{saveError}</p>}
                {saveErrorDetails && saveErrorDetails.length > 0 && (
                  <ul className="list-inside list-disc">
                    {saveErrorDetails.map((message) => (
                      <li key={message}>{message}</li>
                    ))}
                  </ul>
                )}
              </div>
            )}

            <Button onClick={onSave} disabled={isSaving} className="w-full">
              {isSaving ? 'Collecting…' : 'Collect Payment'}
            </Button>
          </>
        )}
      </CardContent>
    </Card>
  );
}
