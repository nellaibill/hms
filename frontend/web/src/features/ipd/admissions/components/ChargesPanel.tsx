import { ApiError, CHARGE_TYPES, createAdmissionChargeSchema, type AdmissionChargeFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { BedSingle, Loader2, Plus } from 'lucide-react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useAdmissionChargesQuery, usePostAdmissionChargeMutation } from '../hooks/useAdmissionCharges';

const chargeTypeLabels: Record<(typeof CHARGE_TYPES)[number], string> = {
  AdmissionCharge: 'Admission Charge',
  BedCharge: 'Bed Charge',
  NursingCharge: 'Nursing Charge',
};

interface ChargesPanelProps {
  admissionId: string;
}

export function ChargesPanel({ admissionId }: ChargesPanelProps) {
  const { data: charges, isPending, isError } = useAdmissionChargesQuery(admissionId);
  const mutation = usePostAdmissionChargeMutation(admissionId);

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<AdmissionChargeFormValues>({
    resolver: zodResolver(createAdmissionChargeSchema),
    defaultValues: { chargeType: 'AdmissionCharge', amount: 0, remarks: '' },
  });

  function onSubmit(values: AdmissionChargeFormValues) {
    mutation.mutate(values, { onSuccess: () => reset({ chargeType: 'AdmissionCharge', amount: 0, remarks: '' }) });
  }

  const total = (charges ?? []).reduce((sum, charge) => sum + charge.amount, 0);
  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <div className="flex flex-col gap-4">
      {isPending && (
        <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading charges…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Failed to load charges.
        </p>
      )}

      {!isPending && !isError && (
        <div className="overflow-hidden rounded-lg border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-2.5">Type</th>
                <th className="px-4 py-2.5">Remarks</th>
                <th className="px-4 py-2.5">Posted</th>
                <th className="px-4 py-2.5 text-right">Amount</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {(charges ?? []).length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-6 text-center text-sm text-muted-foreground">
                    No charges posted yet.
                  </td>
                </tr>
              )}
              {charges?.map((charge) => (
                <tr key={charge.id} className={charge.chargeType === 'BedCharge' ? 'bg-primary/5' : undefined}>
                  <td className="px-4 py-3 text-sm text-foreground">
                    <span className="inline-flex items-center gap-1.5">
                      {charge.chargeType === 'BedCharge' && <BedSingle className="h-3.5 w-3.5 text-muted-foreground" />}
                      {chargeTypeLabels[charge.chargeType]}
                      {charge.chargeType === 'BedCharge' && (
                        <span className="rounded-full bg-primary/10 px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wide text-primary">
                          Auto
                        </span>
                      )}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">{charge.remarks || '—'}</td>
                  <td className="px-4 py-3 text-sm text-muted-foreground">{new Date(charge.createdAt).toLocaleString('en-IN')}</td>
                  <td className="px-4 py-3 text-right font-mono text-sm text-foreground">₹{charge.amount.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
            {(charges ?? []).length > 0 && (
              <tfoot>
                <tr className="border-t border-border bg-muted/30">
                  <td colSpan={3} className="px-4 py-2.5 text-right text-sm font-medium text-foreground">
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
          <Label htmlFor="chargeType">Charge type</Label>
          <Controller
            control={control}
            name="chargeType"
            render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange}>
                <SelectTrigger id="chargeType" className="w-48" aria-label="Charge type">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {CHARGE_TYPES.map((type) => (
                    <SelectItem key={type} value={type}>
                      {chargeTypeLabels[type]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="amount">Amount</Label>
          <Input id="amount" type="number" step="0.01" min="0" className="w-32" {...register('amount')} />
          {errors.amount && <p className="text-sm text-destructive">{errors.amount.message}</p>}
        </div>

        <div className="flex flex-1 flex-col gap-1.5">
          <Label htmlFor="remarks">Remarks (optional)</Label>
          <Input id="remarks" {...register('remarks')} />
        </div>

        <Button type="submit" disabled={mutation.isPending} className="gap-1.5">
          <Plus className="h-4 w-4" />
          {mutation.isPending ? 'Posting…' : 'Post Charge'}
        </Button>
      </form>
    </div>
  );
}
