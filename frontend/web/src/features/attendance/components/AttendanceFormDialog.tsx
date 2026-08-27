import {
  ATTENDANCE_STATUSES,
  ApiError,
  createAttendanceSchema,
  updateAttendanceSchema,
  type AttendanceFormValues,
  type AttendanceResponse,
} from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { EmployeeSelect } from '@/components/EmployeeSelect';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useCreateAttendanceMutation, useUpdateAttendanceMutation } from '../hooks/useAttendanceMutations';

interface AttendanceFormDialogProps {
  mode: 'create' | 'edit';
  /** Required when mode is 'edit'. */
  attendance?: AttendanceResponse;
  onClose: () => void;
}

/** Manual attendance entry/correction — the same form covers both a fresh record (Absent/
 * OnLeave marked without ever checking in) and editing an existing one. EmployeeId/
 * AttendanceDate are the natural key (see UpdateAttendanceRequest's own doc comment) and are
 * locked once a record exists. */
export function AttendanceFormDialog({ mode, attendance, onClose }: AttendanceFormDialogProps) {
  const createMutation = useCreateAttendanceMutation();
  const updateMutation = useUpdateAttendanceMutation();
  const mutation = mode === 'create' ? createMutation : updateMutation;

  const {
    control,
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<AttendanceFormValues>({
    resolver: zodResolver(mode === 'create' ? createAttendanceSchema : updateAttendanceSchema),
    defaultValues: {
      employeeId: attendance?.employeeId ?? '',
      attendanceDate: attendance?.attendanceDate ?? new Date().toISOString().slice(0, 10),
      checkInTime: attendance?.checkInTime ? attendance.checkInTime.slice(0, 16) : '',
      checkOutTime: attendance?.checkOutTime ? attendance.checkOutTime.slice(0, 16) : '',
      status: attendance?.status ?? 'Present',
      remarks: attendance?.remarks ?? '',
    },
  });

  function onSubmit(values: AttendanceFormValues) {
    const checkInTime = values.checkInTime ? new Date(values.checkInTime).toISOString() : null;
    const checkOutTime = values.checkOutTime ? new Date(values.checkOutTime).toISOString() : null;
    const remarks = values.remarks || null;

    if (mode === 'create') {
      createMutation.mutate(
        { employeeId: values.employeeId, attendanceDate: values.attendanceDate, checkInTime, checkOutTime, status: values.status, remarks },
        { onSuccess: onClose },
      );
    } else if (attendance) {
      updateMutation.mutate(
        { id: attendance.id, request: { checkInTime, checkOutTime, status: values.status, remarks } },
        { onSuccess: onClose },
      );
    }
  }

  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="attendance-form-title">
        <DialogHeader>
          <DialogTitle id="attendance-form-title">{mode === 'create' ? 'New Attendance Record' : 'Edit Attendance Record'}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
          {apiError && !apiError.validationErrors && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {apiError.message}
            </p>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="att-employeeId">Employee</Label>
            <Controller
              control={control}
              name="employeeId"
              render={({ field }) => <EmployeeSelect id="att-employeeId" value={field.value} onValueChange={field.onChange} disabled={mode === 'edit'} />}
            />
            {errors.employeeId && <p className="text-sm text-destructive">{errors.employeeId.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="att-attendanceDate">Attendance date</Label>
            <Input id="att-attendanceDate" type="date" disabled={mode === 'edit'} {...register('attendanceDate')} />
            {errors.attendanceDate && <p className="text-sm text-destructive">{errors.attendanceDate.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="att-checkInTime">Check-in time (optional)</Label>
              <Input id="att-checkInTime" type="datetime-local" {...register('checkInTime')} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="att-checkOutTime">Check-out time (optional)</Label>
              <Input id="att-checkOutTime" type="datetime-local" {...register('checkOutTime')} />
            </div>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="att-status">Status</Label>
            <Controller
              control={control}
              name="status"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="att-status" aria-label="Status">
                    <SelectValue placeholder="Select status" />
                  </SelectTrigger>
                  <SelectContent>
                    {ATTENDANCE_STATUSES.map((status) => (
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

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="att-remarks">Remarks (optional)</Label>
            <Input id="att-remarks" {...register('remarks')} />
            {errors.remarks && <p className="text-sm text-destructive">{errors.remarks.message}</p>}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={mutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? 'Saving…' : mode === 'create' ? 'Create' : 'Save Changes'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
