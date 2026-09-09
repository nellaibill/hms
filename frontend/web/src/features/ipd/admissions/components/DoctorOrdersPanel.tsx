import { ApiError, createDoctorOrderSchema, DOCTOR_ORDER_TYPES, type DoctorOrderFormValues, type DoctorOrderStatus } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowRight, Loader2, Plus, X } from 'lucide-react';
import { Controller, useForm } from 'react-hook-form';
import { Badge, type BadgeProps } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import {
  useAdvanceDoctorOrderMutation,
  useCancelDoctorOrderMutation,
  useDoctorOrdersQuery,
  usePostDoctorOrderMutation,
} from '../hooks/useDoctorOrders';

interface DoctorOrdersPanelProps {
  admissionId: string;
}

const defaultValues: DoctorOrderFormValues = {
  orderType: 'Radiology',
  description: '',
  instructions: '',
  orderedAt: '',
};

const statusBadgeVariant: Record<DoctorOrderStatus, BadgeProps['variant']> = {
  Ordered: 'secondary',
  Accepted: 'outline',
  InProgress: 'warning',
  Completed: 'success',
  Cancelled: 'destructive',
};

// Fixed linear sequence, mirrors HMS.Modules.IPD.Domain.DoctorOrder.Advance exactly.
const nextStatus: Partial<Record<DoctorOrderStatus, DoctorOrderStatus>> = {
  Ordered: 'Accepted',
  Accepted: 'InProgress',
  InProgress: 'Completed',
};

export function DoctorOrdersPanel({ admissionId }: DoctorOrdersPanelProps) {
  const { data: orders, isPending, isError } = useDoctorOrdersQuery(admissionId);
  const createMutation = usePostDoctorOrderMutation(admissionId);
  const advanceMutation = useAdvanceDoctorOrderMutation(admissionId);
  const cancelMutation = useCancelDoctorOrderMutation(admissionId);

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<DoctorOrderFormValues>({
    resolver: zodResolver(createDoctorOrderSchema),
    defaultValues,
  });

  function onSubmit(values: DoctorOrderFormValues) {
    createMutation.mutate(
      {
        orderType: values.orderType,
        description: values.description,
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
          Loading doctor orders…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Failed to load doctor orders.
        </p>
      )}

      {!isPending && !isError && (
        <div className="overflow-hidden rounded-lg border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-3 py-2.5">Type</th>
                <th className="px-3 py-2.5">Description</th>
                <th className="px-3 py-2.5">Ordered At</th>
                <th className="px-3 py-2.5">Status</th>
                <th className="px-3 py-2.5 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {(orders ?? []).length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-sm text-muted-foreground">
                    No doctor orders placed yet.
                  </td>
                </tr>
              )}
              {orders?.map((order) => {
                const isTerminal = order.status === 'Completed' || order.status === 'Cancelled';
                const advanceTo = nextStatus[order.status];
                const isMutatingThisRow =
                  (advanceMutation.isPending && advanceMutation.variables === order.id) ||
                  (cancelMutation.isPending && cancelMutation.variables === order.id);

                return (
                  <tr key={order.id}>
                    <td className="px-3 py-3 text-sm text-foreground">{order.orderType}</td>
                    <td className="px-3 py-3 text-sm text-foreground">
                      {order.description}
                      {order.instructions && <p className="text-xs text-muted-foreground">{order.instructions}</p>}
                      {order.status === 'Cancelled' && order.cancellationReason && (
                        <p className="text-xs text-destructive">Reason: {order.cancellationReason}</p>
                      )}
                    </td>
                    <td className="px-3 py-3 text-sm text-muted-foreground">{new Date(order.orderedAt).toLocaleString('en-IN')}</td>
                    <td className="px-3 py-3 text-sm">
                      <Badge variant={statusBadgeVariant[order.status]}>{order.status}</Badge>
                    </td>
                    <td className="px-3 py-3 text-right">
                      {!isTerminal && (
                        <div className="flex justify-end gap-1.5">
                          {advanceTo && (
                            <Button
                              type="button"
                              size="sm"
                              variant="outline"
                              className="gap-1"
                              disabled={isMutatingThisRow}
                              onClick={() => advanceMutation.mutate(order.id)}
                            >
                              <ArrowRight className="h-3.5 w-3.5" />
                              {advanceTo}
                            </Button>
                          )}
                          <Button
                            type="button"
                            size="sm"
                            variant="ghost"
                            className="gap-1 text-destructive hover:text-destructive"
                            disabled={isMutatingThisRow}
                            onClick={() => cancelMutation.mutate(order.id)}
                          >
                            <X className="h-3.5 w-3.5" />
                            Cancel
                          </Button>
                        </div>
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

        <div className="flex flex-wrap items-end gap-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="orderType">Order type</Label>
            <Controller
              control={control}
              name="orderType"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="orderType" className="w-40" aria-label="Order type">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {DOCTOR_ORDER_TYPES.map((type) => (
                      <SelectItem key={type} value={type}>
                        {type}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="orderedAt">Ordered at</Label>
            <Input id="orderedAt" type="datetime-local" className="w-64" {...register('orderedAt')} />
            {errors.orderedAt && <p className="text-xs text-destructive">{errors.orderedAt.message}</p>}
          </div>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="description">Description</Label>
          <Input id="description" {...register('description')} />
          {errors.description && <p className="text-xs text-destructive">{errors.description.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="instructions">Instructions (optional)</Label>
          <Input id="instructions" {...register('instructions')} />
        </div>

        <Button type="submit" disabled={createMutation.isPending} className="w-fit gap-1.5">
          <Plus className="h-4 w-4" />
          {createMutation.isPending ? 'Placing…' : 'Place Order'}
        </Button>
      </form>
    </div>
  );
}
