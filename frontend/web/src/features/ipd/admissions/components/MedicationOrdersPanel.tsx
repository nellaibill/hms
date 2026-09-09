import { ApiError, createMedicationOrderSchema, type MedicationOrderFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2, Plus, X } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useDiscontinueMedicationOrderMutation, useMedicationOrdersQuery, usePostMedicationOrderMutation } from '../hooks/useMedicationOrders';

interface MedicationOrdersPanelProps {
  admissionId: string;
}

const defaultValues: MedicationOrderFormValues = {
  drugName: '',
  dose: '',
  route: '',
  frequency: '',
  startDate: '',
  endDate: '',
  instructions: '',
  orderedAt: '',
};

export function MedicationOrdersPanel({ admissionId }: MedicationOrdersPanelProps) {
  const { data: orders, isPending, isError } = useMedicationOrdersQuery(admissionId);
  const createMutation = usePostMedicationOrderMutation(admissionId);
  const discontinueMutation = useDiscontinueMedicationOrderMutation(admissionId);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<MedicationOrderFormValues>({
    resolver: zodResolver(createMedicationOrderSchema),
    defaultValues,
  });

  function onSubmit(values: MedicationOrderFormValues) {
    createMutation.mutate(
      {
        drugName: values.drugName,
        dose: values.dose,
        route: values.route,
        frequency: values.frequency,
        startDate: new Date(values.startDate).toISOString(),
        endDate: values.endDate ? new Date(values.endDate).toISOString() : null,
        instructions: values.instructions || null,
        orderedAt: new Date(values.orderedAt).toISOString(),
      },
      { onSuccess: () => reset(defaultValues) },
    );
  }

  const apiError = createMutation.error instanceof ApiError ? createMutation.error : null;

  return (
    <div className="flex flex-col gap-4">
      {isPending && (
        <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading medication orders…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Failed to load medication orders.
        </p>
      )}

      {!isPending && !isError && (
        <div className="overflow-hidden rounded-lg border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-3 py-2.5">Drug</th>
                <th className="px-3 py-2.5">Dose</th>
                <th className="px-3 py-2.5">Route</th>
                <th className="px-3 py-2.5">Frequency</th>
                <th className="px-3 py-2.5">Start</th>
                <th className="px-3 py-2.5">End</th>
                <th className="px-3 py-2.5">Status</th>
                <th className="px-3 py-2.5 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {(orders ?? []).length === 0 && (
                <tr>
                  <td colSpan={8} className="px-4 py-6 text-center text-sm text-muted-foreground">
                    No medication orders placed yet.
                  </td>
                </tr>
              )}
              {orders?.map((order) => {
                const isEnded = Boolean(order.endDate) && new Date(order.endDate as string) < new Date();
                const isDiscontinuing = discontinueMutation.isPending && discontinueMutation.variables?.orderId === order.id;

                return (
                  <tr key={order.id}>
                    <td className="px-3 py-3 text-sm text-foreground">{order.drugName}</td>
                    <td className="px-3 py-3 text-sm text-foreground">{order.dose}</td>
                    <td className="px-3 py-3 text-sm text-foreground">{order.route}</td>
                    <td className="px-3 py-3 text-sm text-foreground">{order.frequency}</td>
                    <td className="px-3 py-3 text-sm text-muted-foreground">{new Date(order.startDate).toLocaleDateString('en-IN')}</td>
                    <td className="px-3 py-3 text-sm text-muted-foreground">{order.endDate ? new Date(order.endDate).toLocaleDateString('en-IN') : '—'}</td>
                    <td className="px-3 py-3 text-sm">
                      <Badge variant={order.status === 'Discontinued' ? 'destructive' : isEnded ? 'secondary' : 'success'}>
                        {order.status === 'Discontinued' ? 'Discontinued' : isEnded ? 'Ended' : 'Active'}
                      </Badge>
                    </td>
                    <td className="px-3 py-3 text-right">
                      {order.status === 'Active' && (
                        <Button
                          type="button"
                          size="sm"
                          variant="ghost"
                          className="gap-1 text-destructive hover:text-destructive"
                          disabled={isDiscontinuing}
                          onClick={() => discontinueMutation.mutate({ orderId: order.id })}
                        >
                          <X className="h-3.5 w-3.5" />
                          Discontinue
                        </Button>
                      )}
                    </td>
                  </tr>
                );
              })}
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

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="drugName">Drug name</Label>
            <Input id="drugName" {...register('drugName')} />
            {errors.drugName && <p className="text-xs text-destructive">{errors.drugName.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="dose">Dose</Label>
            <Input id="dose" {...register('dose')} />
            {errors.dose && <p className="text-xs text-destructive">{errors.dose.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="route">Route</Label>
            <Input id="route" {...register('route')} />
            {errors.route && <p className="text-xs text-destructive">{errors.route.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="frequency">Frequency</Label>
            <Input id="frequency" placeholder="e.g. BD" {...register('frequency')} />
            {errors.frequency && <p className="text-xs text-destructive">{errors.frequency.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="orderedAt">Ordered at</Label>
            <Input id="orderedAt" type="datetime-local" {...register('orderedAt')} />
            {errors.orderedAt && <p className="text-xs text-destructive">{errors.orderedAt.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="startDate">Start date</Label>
            <Input id="startDate" type="datetime-local" {...register('startDate')} />
            {errors.startDate && <p className="text-xs text-destructive">{errors.startDate.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="endDate">End date (optional)</Label>
            <Input id="endDate" type="datetime-local" {...register('endDate')} />
            {errors.endDate && <p className="text-xs text-destructive">{errors.endDate.message}</p>}
          </div>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="instructions">Instructions (optional)</Label>
          <Input id="instructions" {...register('instructions')} />
        </div>

        <Button type="submit" disabled={createMutation.isPending} className="w-fit gap-1.5">
          <Plus className="h-4 w-4" />
          {createMutation.isPending ? 'Placing…' : 'Place Medication Order'}
        </Button>
      </form>
    </div>
  );
}
