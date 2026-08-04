import { ApiError, SWAP_REQUEST_STATUSES, swapRequestSchema, type SwapRequestFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { StaffSelect } from '@/components/StaffSelect';
import { ShiftAssignmentSelect } from './ShiftAssignmentSelect';

interface ShiftSwapRequestFormProps {
  defaultValues?: Partial<SwapRequestFormValues>;
  onSubmit: (values: SwapRequestFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

export function ShiftSwapRequestForm({ defaultValues, onSubmit, isSubmitting, submitLabel, apiError }: ShiftSwapRequestFormProps) {
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<SwapRequestFormValues>({
    resolver: zodResolver(swapRequestSchema),
    defaultValues: {
      requestedByStaffId: '',
      requestedToStaffId: '',
      currentShiftAssignmentId: '',
      requestedShiftAssignmentId: '',
      status: 'Pending',
      requestedDate: '',
      approvedDate: '',
      approvedBy: '',
      remarks: '',
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
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof SwapRequestFormValues;
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
        <Label htmlFor="requestedByStaffId">Requested by</Label>
        <Controller
          control={control}
          name="requestedByStaffId"
          render={({ field }) => (
            <StaffSelect id="requestedByStaffId" value={field.value} onValueChange={field.onChange} ariaLabel="Requested by" />
          )}
        />
        {errors.requestedByStaffId && <p className="text-sm text-destructive">{errors.requestedByStaffId.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="requestedToStaffId">Requested to</Label>
        <Controller
          control={control}
          name="requestedToStaffId"
          render={({ field }) => (
            <StaffSelect id="requestedToStaffId" value={field.value} onValueChange={field.onChange} ariaLabel="Requested to" />
          )}
        />
        {errors.requestedToStaffId && <p className="text-sm text-destructive">{errors.requestedToStaffId.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="currentShiftAssignmentId">Current shift assignment</Label>
        <Controller
          control={control}
          name="currentShiftAssignmentId"
          render={({ field }) => (
            <ShiftAssignmentSelect id="currentShiftAssignmentId" value={field.value} onValueChange={field.onChange} ariaLabel="Current shift assignment" />
          )}
        />
        {errors.currentShiftAssignmentId && <p className="text-sm text-destructive">{errors.currentShiftAssignmentId.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="requestedShiftAssignmentId">Requested shift assignment</Label>
        <Controller
          control={control}
          name="requestedShiftAssignmentId"
          render={({ field }) => (
            <ShiftAssignmentSelect
              id="requestedShiftAssignmentId"
              value={field.value}
              onValueChange={field.onChange}
              ariaLabel="Requested shift assignment"
            />
          )}
        />
        {errors.requestedShiftAssignmentId && <p className="text-sm text-destructive">{errors.requestedShiftAssignmentId.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="requestedDate">Requested date</Label>
          <Input id="requestedDate" type="datetime-local" {...register('requestedDate')} />
          {errors.requestedDate && <p className="text-sm text-destructive">{errors.requestedDate.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="status">Status</Label>
          <Controller
            control={control}
            name="status"
            render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange}>
                <SelectTrigger id="status" aria-label="Status">
                  <SelectValue placeholder="Select status" />
                </SelectTrigger>
                <SelectContent>
                  {SWAP_REQUEST_STATUSES.map((status) => (
                    <SelectItem key={status} value={status}>
                      {status}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
          {errors.status && <p className="text-sm text-destructive">{errors.status.message}</p>}
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="approvedBy">Approved by (optional)</Label>
        <Controller
          control={control}
          name="approvedBy"
          render={({ field }) => (
            <StaffSelect id="approvedBy" value={field.value ?? ''} onValueChange={field.onChange} ariaLabel="Approved by" />
          )}
        />
        {errors.approvedBy && <p className="text-sm text-destructive">{errors.approvedBy.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="approvedDate">Approved date (optional)</Label>
        <Input id="approvedDate" type="datetime-local" {...register('approvedDate')} />
        {errors.approvedDate && <p className="text-sm text-destructive">{errors.approvedDate.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="remarks">Remarks (optional)</Label>
        <Input id="remarks" {...register('remarks')} />
        {errors.remarks && <p className="text-sm text-destructive">{errors.remarks.message}</p>}
      </div>

      <Button type="submit" disabled={isSubmitting} className="mt-2 self-start">
        {isSubmitting ? 'Saving…' : submitLabel}
      </Button>
    </form>
  );
}
