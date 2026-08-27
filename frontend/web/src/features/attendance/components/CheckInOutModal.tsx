import { ApiError, checkInOutSchema, type CheckInOutFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { EmployeeSelect } from '@/components/EmployeeSelect';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useCheckInMutation, useCheckOutMutation } from '../hooks/useAttendanceMutations';

interface CheckInOutModalProps {
  mode: 'check-in' | 'check-out';
  /** Pre-selects (and locks) the employee — used when opened from an existing today's
   * attendance row that's already checked in. Left undefined for the toolbar's "Check In"
   * action, where any employee can be picked. */
  employeeId?: string;
  onClose: () => void;
}

/** A small modal for recording today's check-in/check-out — the employee searchable-select
 * (unless fixed via `employeeId`) plus an optional time override; time defaults to the
 * server's current UTC time when left blank, matching CheckInRequest/CheckOutRequest's own
 * doc comments. */
export function CheckInOutModal({ mode, employeeId, onClose }: CheckInOutModalProps) {
  const checkInMutation = useCheckInMutation();
  const checkOutMutation = useCheckOutMutation();
  const mutation = mode === 'check-in' ? checkInMutation : checkOutMutation;

  const {
    control,
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CheckInOutFormValues>({
    resolver: zodResolver(checkInOutSchema),
    defaultValues: { employeeId: employeeId ?? '', time: '' },
  });

  function onSubmit(values: CheckInOutFormValues) {
    const isoTime = values.time ? new Date(values.time).toISOString() : undefined;
    if (mode === 'check-in') {
      checkInMutation.mutate(
        { employeeId: values.employeeId, checkInTime: isoTime },
        { onSuccess: onClose },
      );
    } else {
      checkOutMutation.mutate(
        { employeeId: values.employeeId, checkOutTime: isoTime },
        { onSuccess: onClose },
      );
    }
  }

  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="check-in-out-title">
        <DialogHeader>
          <DialogTitle id="check-in-out-title">{mode === 'check-in' ? 'Check In' : 'Check Out'}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
          {apiError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {apiError.message}
            </p>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ci-employeeId">Employee</Label>
            <Controller
              control={control}
              name="employeeId"
              render={({ field }) => (
                <EmployeeSelect id="ci-employeeId" value={field.value} onValueChange={field.onChange} disabled={Boolean(employeeId)} />
              )}
            />
            {errors.employeeId && <p className="text-sm text-destructive">{errors.employeeId.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ci-time">{mode === 'check-in' ? 'Check-in time' : 'Check-out time'} (optional)</Label>
            <Input id="ci-time" type="datetime-local" {...register('time')} />
            <p className="text-xs text-muted-foreground">Defaults to the current time when left blank.</p>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={mutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? 'Saving…' : mode === 'check-in' ? 'Check In' : 'Check Out'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
