import { AVAILABILITY_STATUSES, ApiError, staffAvailabilitySchema, type StaffAvailabilityFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { StaffSelect } from '@/components/StaffSelect';

interface StaffAvailabilityFormProps {
  defaultValues?: Partial<StaffAvailabilityFormValues>;
  onSubmit: (values: StaffAvailabilityFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

export function StaffAvailabilityForm({ defaultValues, onSubmit, isSubmitting, submitLabel, apiError }: StaffAvailabilityFormProps) {
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<StaffAvailabilityFormValues>({
    resolver: zodResolver(staffAvailabilitySchema),
    defaultValues: {
      staffId: '',
      startDate: '',
      endDate: '',
      availabilityStatus: 'Available',
      reason: '',
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
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof StaffAvailabilityFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex max-w-lg flex-col gap-4">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="staffId">Staff</Label>
        <Controller
          control={control}
          name="staffId"
          render={({ field }) => <StaffSelect id="staffId" value={field.value} onValueChange={field.onChange} />}
        />
        {errors.staffId && <p className="text-sm text-destructive">{errors.staffId.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="startDate">Start date</Label>
          <Input id="startDate" type="date" {...register('startDate')} />
          {errors.startDate && <p className="text-sm text-destructive">{errors.startDate.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="endDate">End date</Label>
          <Input id="endDate" type="date" {...register('endDate')} />
          {errors.endDate && <p className="text-sm text-destructive">{errors.endDate.message}</p>}
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="availabilityStatus">Availability status</Label>
        <Controller
          control={control}
          name="availabilityStatus"
          render={({ field }) => (
            <Select value={field.value} onValueChange={field.onChange}>
              <SelectTrigger id="availabilityStatus" aria-label="Availability status">
                <SelectValue placeholder="Select status" />
              </SelectTrigger>
              <SelectContent>
                {AVAILABILITY_STATUSES.map((status) => (
                  <SelectItem key={status} value={status}>
                    {status}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        />
        {errors.availabilityStatus && <p className="text-sm text-destructive">{errors.availabilityStatus.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="reason">Reason (optional)</Label>
        <Input id="reason" placeholder="e.g. Conference, Training, Medical Leave" {...register('reason')} />
        {errors.reason && <p className="text-sm text-destructive">{errors.reason.message}</p>}
      </div>

      <Button type="submit" disabled={isSubmitting} className="mt-2 self-start">
        {isSubmitting ? 'Saving…' : submitLabel}
      </Button>
    </form>
  );
}
