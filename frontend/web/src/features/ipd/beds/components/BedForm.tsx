import { ApiError, BED_STATUSES, BED_TYPES, createBedSchema, type BedFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { WardSelect } from '@/components/WardSelect';

const bedStatusLabels: Record<(typeof BED_STATUSES)[number], string> = {
  Available: 'Available',
  Occupied: 'Occupied',
  Maintenance: 'Maintenance',
};

const bedTypeLabels: Record<(typeof BED_TYPES)[number], string> = {
  Standard: 'Standard',
  Electric: 'Electric',
  ICU: 'ICU',
  SemiICU: 'Semi-ICU',
  Deluxe: 'Deluxe',
};

interface BedFormProps {
  mode: 'create' | 'edit';
  defaultValues?: Partial<BedFormValues>;
  onSubmit: (values: BedFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

export function BedForm({ mode, defaultValues, onSubmit, isSubmitting, submitLabel, apiError }: BedFormProps) {
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<BedFormValues>({
    resolver: zodResolver(createBedSchema),
    defaultValues: {
      wardId: '',
      bedNumber: '',
      bedType: 'Standard',
      status: 'Available',
      isActive: true,
      dailyCharge: 0,
      ...defaultValues,
    },
  });

  // Server-side validation failures (docs/ApiStandards.md §5) are mapped onto the same
  // field-level display client validation uses, per docs/FrontendArchitecture.md §9.
  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }

    for (const issue of apiError.validationErrors) {
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof BedFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  // Occupied is driven exclusively by the admission workflow (admit/transfer/discharge) —
  // this generic edit form must never be able to move a bed into or out of that state, or
  // it desyncs from the real patient. Occupied only ever appears as a selectable option when
  // it's already the bed's current status, so the field still renders correctly; the whole
  // control is locked in that case.
  const isOccupied = mode === 'edit' && defaultValues?.status === 'Occupied';
  const selectableStatuses = BED_STATUSES.filter((status) => status !== 'Occupied' || isOccupied);

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex max-w-lg flex-col gap-4">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="wardId">Ward</Label>
        <Controller
          control={control}
          name="wardId"
          render={({ field }) => <WardSelect id="wardId" value={field.value} onValueChange={field.onChange} disabled={mode === 'edit'} />}
        />
        {mode === 'edit' && <p className="text-xs text-muted-foreground">A bed can't be moved to another ward here — use Bed Transfer on an admission.</p>}
        {errors.wardId && <p className="text-sm text-destructive">{errors.wardId.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="bedNumber">Bed number</Label>
        <Input id="bedNumber" disabled={mode === 'edit'} {...register('bedNumber')} />
        {mode === 'edit' && <p className="text-xs text-muted-foreground">Bed number can't be changed after a bed is created.</p>}
        {errors.bedNumber && <p className="text-sm text-destructive">{errors.bedNumber.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="bedType">Bed type</Label>
        <Controller
          control={control}
          name="bedType"
          render={({ field }) => (
            <Select value={field.value} onValueChange={field.onChange}>
              <SelectTrigger id="bedType" aria-label="Bed type">
                <SelectValue placeholder="Select bed type…" />
              </SelectTrigger>
              <SelectContent>
                {BED_TYPES.map((type) => (
                  <SelectItem key={type} value={type}>
                    {bedTypeLabels[type]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        />
        {errors.bedType && <p className="text-sm text-destructive">{errors.bedType.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="dailyCharge">Daily charge (₹)</Label>
        <Input id="dailyCharge" type="number" min="0" step="0.01" {...register('dailyCharge')} />
        {errors.dailyCharge && <p className="text-sm text-destructive">{errors.dailyCharge.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="status">Status</Label>
        <Controller
          control={control}
          name="status"
          render={({ field }) => (
            <Select value={field.value} onValueChange={field.onChange} disabled={isOccupied}>
              <SelectTrigger id="status" aria-label="Status">
                <SelectValue placeholder="Select status…" />
              </SelectTrigger>
              <SelectContent>
                {selectableStatuses.map((status) => (
                  <SelectItem key={status} value={status}>
                    {bedStatusLabels[status]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        />
        {isOccupied && (
          <p className="text-xs text-muted-foreground">
            This bed is occupied by an admitted patient — discharge or transfer them to change its status.
          </p>
        )}
        {errors.status && <p className="text-sm text-destructive">{errors.status.message}</p>}
      </div>

      <div className="flex items-center justify-between rounded-md border border-border px-3 py-2.5">
        <Label htmlFor="isActive" className="cursor-pointer">
          Active
        </Label>
        <Controller
          control={control}
          name="isActive"
          render={({ field }) => <Switch id="isActive" checked={field.value} onCheckedChange={field.onChange} />}
        />
      </div>

      <Button type="submit" disabled={isSubmitting} className="mt-2 self-start">
        {isSubmitting ? 'Saving…' : submitLabel}
      </Button>
    </form>
  );
}
