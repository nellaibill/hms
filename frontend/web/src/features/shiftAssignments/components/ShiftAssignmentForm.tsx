import { ASSIGNMENT_STATUSES, ApiError, shiftAssignmentSchema, type ShiftAssignmentFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { StaffSelect } from '@/components/StaffSelect';
import { ShiftSelect } from './ShiftSelect';

interface ShiftAssignmentFormProps {
  defaultValues?: Partial<ShiftAssignmentFormValues>;
  onSubmit: (values: ShiftAssignmentFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

export function ShiftAssignmentForm({ defaultValues, onSubmit, isSubmitting, submitLabel, apiError }: ShiftAssignmentFormProps) {
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ShiftAssignmentFormValues>({
    resolver: zodResolver(shiftAssignmentSchema),
    defaultValues: {
      staffId: '',
      departmentId: '',
      shiftId: '',
      rosterDate: '',
      status: 'Scheduled',
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
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof ShiftAssignmentFormValues;
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

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="shiftId">Shift</Label>
        <Controller
          control={control}
          name="shiftId"
          render={({ field }) => <ShiftSelect id="shiftId" value={field.value} onValueChange={field.onChange} />}
        />
        {errors.shiftId && <p className="text-sm text-destructive">{errors.shiftId.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="departmentId">Department ID</Label>
        <Input id="departmentId" placeholder="00000000-0000-0000-0000-000000000000" {...register('departmentId')} />
        <p className="text-xs text-muted-foreground">No department directory exists yet — enter the department's GUID directly.</p>
        {errors.departmentId && <p className="text-sm text-destructive">{errors.departmentId.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="rosterDate">Roster date</Label>
          <Input id="rosterDate" type="date" {...register('rosterDate')} />
          {errors.rosterDate && <p className="text-sm text-destructive">{errors.rosterDate.message}</p>}
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
                  {ASSIGNMENT_STATUSES.map((status) => (
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
