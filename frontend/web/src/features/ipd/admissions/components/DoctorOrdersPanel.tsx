import { ApiError, createDoctorOrderSchema, DOCTOR_ORDER_TYPES, type DoctorOrderFormValues, type DoctorOrderStatus } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQuery } from '@tanstack/react-query';
import { ArrowRight, Loader2, Plus, X } from 'lucide-react';
import { Controller, useForm } from 'react-hook-form';
import { Badge, type BadgeProps } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { SearchableSelect, type SearchableSelectOption } from '@/components/ui/searchable-select';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { formatCurrency } from '@/features/billing/billingCalculations';
import { useDiagnosticTestServices } from '@/features/billing/hooks/useDiagnosticTestServices';
import { useDiagnosticServices } from '@/features/diagnostics';
import { consultationTypesApi } from '@/services/apiClient';
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
  catalogItemId: '',
  orderedAt: '',
};

/** Only these three DoctorOrderTypes have a real priced Masters catalog today — see
 * ADR-065. Diet/Nursing/Blood/Referral get no catalog picker, exactly as before this slice. */
const PRICED_ORDER_TYPES = new Set(['Radiology', 'Procedure', 'Consultation']);

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
    setValue,
    watch,
    formState: { errors },
  } = useForm<DoctorOrderFormValues>({
    resolver: zodResolver(createDoctorOrderSchema),
    defaultValues,
  });

  const orderType = watch('orderType');
  const isPricedType = PRICED_ORDER_TYPES.has(orderType);

  const { services: radiologyServices, isLoading: isLoadingRadiology } = useDiagnosticServices('Radiology');
  const { services: procedureServices, isLoading: isLoadingProcedure } = useDiagnosticTestServices('Procedure');
  const consultationTypesQuery = useQuery({
    queryKey: ['consultationTypes', 'select-list'],
    queryFn: () => consultationTypesApi.getConsultationTypes({ pageSize: 100, isActive: true }),
    enabled: orderType === 'Consultation',
  });

  const catalogOptions: SearchableSelectOption[] =
    orderType === 'Radiology'
      ? radiologyServices.map((s) => ({ value: s.id, label: `${s.name} — ${formatCurrency(s.price)}`, keywords: s.name }))
      : orderType === 'Procedure'
        ? procedureServices.map((s) => ({ value: s.id, label: `${s.name} — ${formatCurrency(s.price)}`, keywords: s.name }))
        : orderType === 'Consultation'
          ? (consultationTypesQuery.data?.items ?? []).map((t) => ({
              value: t.id,
              label: t.amount != null ? `${t.name} — ${formatCurrency(t.amount)}` : `${t.name} — no fixed fee`,
              keywords: t.name,
            }))
          : [];

  function findCatalogItemName(itemId: string): string | undefined {
    if (orderType === 'Radiology') return radiologyServices.find((s) => s.id === itemId)?.name;
    if (orderType === 'Procedure') return procedureServices.find((s) => s.id === itemId)?.name;
    if (orderType === 'Consultation') return consultationTypesQuery.data?.items.find((t) => t.id === itemId)?.name;
    return undefined;
  }

  const isLoadingCatalog =
    orderType === 'Radiology' ? isLoadingRadiology : orderType === 'Procedure' ? isLoadingProcedure : consultationTypesQuery.isPending;

  function handleOrderTypeChange(nextOrderType: string) {
    setValue('orderType', nextOrderType as DoctorOrderFormValues['orderType']);
    // A catalog item picked for the previous type has no meaning under a different type (or
    // none at all, for an unpriced type) — clear it rather than silently carrying it over.
    setValue('catalogItemId', '');
  }

  function handleCatalogItemChange(itemId: string) {
    setValue('catalogItemId', itemId);
    const name = findCatalogItemName(itemId);
    if (name) {
      // Convenience default, matching ConsultationBillingCard's own "default then let them
      // override" pattern — Description stays freely editable afterward.
      setValue('description', name);
    }
  }

  function onSubmit(values: DoctorOrderFormValues) {
    createMutation.mutate(
      {
        orderType: values.orderType,
        description: values.description,
        instructions: values.instructions || null,
        catalogItemId: values.catalogItemId || null,
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
                <Select value={field.value} onValueChange={handleOrderTypeChange}>
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

        {isPricedType && (
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="catalogItem">Catalog item (optional — auto-posts a charge)</Label>
            <Controller
              control={control}
              name="catalogItemId"
              render={({ field }) => (
                <SearchableSelect
                  id="catalogItem"
                  ariaLabel="Catalog item"
                  value={field.value || ''}
                  onValueChange={handleCatalogItemChange}
                  options={catalogOptions}
                  placeholder={isLoadingCatalog ? 'Loading items…' : 'Select a catalog item (optional)'}
                  searchPlaceholder="Search…"
                  disabled={isLoadingCatalog}
                />
              )}
            />
          </div>
        )}

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
