import { ApiError, createAdmissionAdvanceSchema, PAYMENT_METHODS, type AdmissionAdvanceFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2, Plus } from 'lucide-react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { PAYMENT_METHOD_LABELS } from '@/features/billing/types';
import { useAdmissionAdvancesQuery, usePostAdmissionAdvanceMutation } from '../hooks/useAdmissionAdvances';

interface AdvancePanelProps {
  admissionId: string;
}

/**
 * Records money collected from the patient's family as an advance/deposit during the stay —
 * IPD's own ledger, deliberately not reconciled into the Final Bill Invoice (see
 * FinalBillCard.tsx's display-only Balance-due computation and ADR-067).
 */
export function AdvancePanel({ admissionId }: AdvancePanelProps) {
  const { data: advances, isPending, isError } = useAdmissionAdvancesQuery(admissionId);
  const mutation = usePostAdmissionAdvanceMutation(admissionId);

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<AdmissionAdvanceFormValues>({
    resolver: zodResolver(createAdmissionAdvanceSchema),
    defaultValues: { amount: 0, method: 'Cash', referenceNumber: '', remarks: '' },
  });

  function onSubmit(values: AdmissionAdvanceFormValues) {
    mutation.mutate(
      { amount: values.amount, method: values.method, referenceNumber: values.referenceNumber || null, remarks: values.remarks || null },
      { onSuccess: () => reset({ amount: 0, method: 'Cash', referenceNumber: '', remarks: '' }) },
    );
  }

  const total = (advances ?? []).reduce((sum, advance) => sum + advance.amount, 0);
  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <div className="flex flex-col gap-4">
      {isPending && (
        <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading advances…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Failed to load advances.
        </p>
      )}

      {!isPending && !isError && (
        <div className="overflow-x-auto rounded-lg border border-border">
          <table className="w-full text-sm">
            <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
              <tr>
                <th className="px-4 py-2.5">Method</th>
                <th className="px-4 py-2.5">Reference</th>
                <th className="px-4 py-2.5">Remarks</th>
                <th className="px-4 py-2.5">Posted</th>
                <th className="px-4 py-2.5 text-right">Amount</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {(advances ?? []).length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-sm text-muted-foreground">
                    No advances collected yet.
                  </td>
                </tr>
              )}
              {advances?.map((advance) => (
                <tr key={advance.id}>
                  <td className="px-4 py-3 text-sm text-foreground">{PAYMENT_METHOD_LABELS[advance.method]}</td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">{advance.referenceNumber || '—'}</td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">{advance.remarks || '—'}</td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">{new Date(advance.createdAt).toLocaleString('en-IN')}</td>
                  <td className="px-4 py-3 text-right font-mono text-sm text-foreground">₹{advance.amount.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
            {(advances ?? []).length > 0 && (
              <tfoot>
                <tr className="border-t border-border bg-muted/30">
                  <td colSpan={4} className="px-4 py-2.5 text-right text-sm font-medium text-foreground">
                    Total
                  </td>
                  <td className="px-4 py-2.5 text-right font-mono text-sm font-semibold text-foreground">₹{total.toFixed(2)}</td>
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-wrap items-end gap-3 rounded-lg border border-dashed border-border p-4">
        {apiError && !apiError.validationErrors && (
          <p role="alert" className="w-full rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {apiError.message}
          </p>
        )}

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="advanceAmount">Amount</Label>
          <Input id="advanceAmount" type="number" step="0.01" min="0" className="w-32" {...register('amount')} />
          {errors.amount && <p className="text-sm text-destructive">{errors.amount.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="advanceMethod">Method</Label>
          <Controller
            control={control}
            name="method"
            render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange}>
                <SelectTrigger id="advanceMethod" className="w-40" aria-label="Payment method">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {PAYMENT_METHODS.map((method) => (
                    <SelectItem key={method} value={method}>
                      {PAYMENT_METHOD_LABELS[method]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="advanceReference">Reference (optional)</Label>
          <Input id="advanceReference" className="w-40" {...register('referenceNumber')} />
        </div>

        {/* min-w so this wraps to its own line instead of flex-1 shrinking it to a sliver
            beside the other fields when the panel is narrow. */}
        <div className="flex min-w-[12rem] flex-1 flex-col gap-1.5">
          <Label htmlFor="advanceRemarks">Remarks (optional)</Label>
          <Input id="advanceRemarks" {...register('remarks')} />
        </div>

        <Button type="submit" disabled={mutation.isPending} className="gap-1.5">
          <Plus className="h-4 w-4" />
          {mutation.isPending ? 'Posting…' : 'Record Advance'}
        </Button>
      </form>
    </div>
  );
}
