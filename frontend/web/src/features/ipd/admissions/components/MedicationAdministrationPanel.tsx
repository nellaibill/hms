import { ApiError, createMedicationAdministrationSchema, type MedicationAdministrationFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2, Plus } from 'lucide-react';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useMedicationOrdersQuery } from '../hooks/useMedicationOrders';
import { useMedicationAdministrationsQuery, usePostMedicationAdministrationMutation } from '../hooks/useMedicationAdministrations';

interface MedicationAdministrationPanelProps {
  admissionId: string;
}

const defaultValues: MedicationAdministrationFormValues = {
  scheduledTime: '',
  wasGiven: 'given',
  administeredAt: '',
  reason: '',
  remarks: '',
};

export function MedicationAdministrationPanel({ admissionId }: MedicationAdministrationPanelProps) {
  const { data: orders, isPending: ordersPending } = useMedicationOrdersQuery(admissionId);
  const [selectedOrderId, setSelectedOrderId] = useState<string | undefined>(undefined);
  const { data: administrations, isPending, isError } = useMedicationAdministrationsQuery(admissionId, selectedOrderId);
  const createMutation = usePostMedicationAdministrationMutation(admissionId, selectedOrderId ?? '');

  const {
    control,
    register,
    handleSubmit,
    watch,
    reset,
    formState: { errors },
  } = useForm<MedicationAdministrationFormValues>({
    resolver: zodResolver(createMedicationAdministrationSchema),
    defaultValues,
  });

  const wasGiven = watch('wasGiven');

  function onSubmit(values: MedicationAdministrationFormValues) {
    createMutation.mutate(
      {
        scheduledTime: new Date(values.scheduledTime).toISOString(),
        wasGiven: values.wasGiven === 'given',
        administeredAt: values.wasGiven === 'given' && values.administeredAt ? new Date(values.administeredAt).toISOString() : null,
        reason: values.reason || null,
        remarks: values.remarks || null,
      },
      { onSuccess: () => reset(defaultValues) },
    );
  }

  const apiError = createMutation.error instanceof ApiError ? createMutation.error : null;
  const selectedOrder = orders?.find((o) => o.id === selectedOrderId);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="orderPicker">Medication order</Label>
        <Select value={selectedOrderId} onValueChange={setSelectedOrderId} disabled={ordersPending || (orders ?? []).length === 0}>
          <SelectTrigger id="orderPicker" className="w-full sm:w-96" aria-label="Medication order">
            <SelectValue placeholder={ordersPending ? 'Loading orders…' : (orders ?? []).length === 0 ? 'No medication orders yet' : 'Select a medication order…'} />
          </SelectTrigger>
          <SelectContent>
            {orders?.map((order) => (
              <SelectItem key={order.id} value={order.id}>
                {order.drugName} — {order.dose} {order.route} ({order.frequency})
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {!selectedOrderId && <p className="text-sm text-muted-foreground">Select a medication order above to view or record its administration history.</p>}

      {selectedOrderId && (
        <>
          {isPending && (
            <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading administration history…
            </div>
          )}

          {isError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              Failed to load administration history.
            </p>
          )}

          {!isPending && !isError && (
            <div className="overflow-hidden rounded-lg border border-border">
              <table className="w-full text-sm">
                <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  <tr>
                    <th className="px-3 py-2.5">Scheduled</th>
                    <th className="px-3 py-2.5">Given</th>
                    <th className="px-3 py-2.5">Administered At</th>
                    <th className="px-3 py-2.5">Reason</th>
                    <th className="px-3 py-2.5">Remarks</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {(administrations ?? []).length === 0 && (
                    <tr>
                      <td colSpan={5} className="px-4 py-6 text-center text-sm text-muted-foreground">
                        No administrations recorded yet for {selectedOrder?.drugName ?? 'this order'}.
                      </td>
                    </tr>
                  )}
                  {administrations?.map((administration) => (
                    <tr key={administration.id}>
                      <td className="px-3 py-3 text-sm text-muted-foreground">{new Date(administration.scheduledTime).toLocaleString('en-IN')}</td>
                      <td className="px-3 py-3 text-sm">
                        <Badge variant={administration.wasGiven ? 'success' : 'destructive'}>{administration.wasGiven ? 'Given' : 'Not Given'}</Badge>
                      </td>
                      <td className="px-3 py-3 text-sm text-muted-foreground">
                        {administration.administeredAt ? new Date(administration.administeredAt).toLocaleString('en-IN') : '—'}
                      </td>
                      <td className="px-3 py-3 text-sm text-foreground">{administration.reason || '—'}</td>
                      <td className="px-3 py-3 text-sm text-foreground">{administration.remarks || '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-3 rounded-lg border border-dashed border-border p-4">
            {apiError && !apiError.validationErrors && (
              <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {apiError.message}
              </p>
            )}

            <div className="flex flex-wrap items-end gap-3">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="scheduledTime">Scheduled time</Label>
                <Input id="scheduledTime" type="datetime-local" className="w-64" {...register('scheduledTime')} />
                {errors.scheduledTime && <p className="text-xs text-destructive">{errors.scheduledTime.message}</p>}
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="wasGiven">Given?</Label>
                <Controller
                  control={control}
                  name="wasGiven"
                  render={({ field }) => (
                    <Select value={field.value} onValueChange={field.onChange}>
                      <SelectTrigger id="wasGiven" className="w-40" aria-label="Given?">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="given">Given</SelectItem>
                        <SelectItem value="not-given">Not Given</SelectItem>
                      </SelectContent>
                    </Select>
                  )}
                />
              </div>

              {wasGiven === 'given' && (
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="administeredAt">Administered at</Label>
                  <Input id="administeredAt" type="datetime-local" className="w-64" {...register('administeredAt')} />
                </div>
              )}
            </div>

            {wasGiven === 'not-given' && (
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="reason">Reason</Label>
                <Input id="reason" placeholder="e.g. Patient refused, NPO for procedure" {...register('reason')} />
              </div>
            )}

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="remarks">Remarks (optional)</Label>
              <Input id="remarks" {...register('remarks')} />
            </div>

            <Button type="submit" disabled={createMutation.isPending} className="w-fit gap-1.5">
              <Plus className="h-4 w-4" />
              {createMutation.isPending ? 'Recording…' : 'Record Administration'}
            </Button>
          </form>
        </>
      )}
    </div>
  );
}
