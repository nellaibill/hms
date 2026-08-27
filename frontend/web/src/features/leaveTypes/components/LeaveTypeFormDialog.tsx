import { ApiError, createLeaveTypeSchema, updateLeaveTypeSchema, type LeaveTypeFormValues, type LeaveTypeResponse } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { useCreateLeaveTypeMutation, useUpdateLeaveTypeMutation } from '../hooks/useLeaveTypeMutations';

interface LeaveTypeFormDialogProps {
  mode: 'create' | 'edit';
  /** Required when mode is 'edit'. */
  leaveType?: LeaveTypeResponse;
  onClose: () => void;
}

/** LeaveType is tiny (4 real fields) — a single list page with this create/edit modal is
 * enough, rather than dedicated create/edit/view route pages. */
export function LeaveTypeFormDialog({ mode, leaveType, onClose }: LeaveTypeFormDialogProps) {
  const createMutation = useCreateLeaveTypeMutation();
  const updateMutation = useUpdateLeaveTypeMutation();
  const mutation = mode === 'create' ? createMutation : updateMutation;

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<LeaveTypeFormValues>({
    resolver: zodResolver(mode === 'create' ? createLeaveTypeSchema : updateLeaveTypeSchema),
    defaultValues: {
      code: leaveType?.code ?? '',
      name: leaveType?.name ?? '',
      maxDaysPerYear: leaveType?.maxDaysPerYear ?? null,
      isPaid: leaveType?.isPaid ?? true,
      isActive: leaveType?.isActive ?? true,
    },
  });

  function onSubmit(values: LeaveTypeFormValues) {
    const maxDaysPerYear = values.maxDaysPerYear ?? null;

    if (mode === 'create') {
      createMutation.mutate(
        { code: values.code, name: values.name, maxDaysPerYear, isPaid: values.isPaid, isActive: values.isActive },
        { onSuccess: onClose },
      );
    } else if (leaveType) {
      updateMutation.mutate(
        { id: leaveType.id, request: { name: values.name, maxDaysPerYear, isPaid: values.isPaid, isActive: values.isActive } },
        { onSuccess: onClose },
      );
    }
  }

  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="leave-type-form-title">
        <DialogHeader>
          <DialogTitle id="leave-type-form-title">{mode === 'create' ? 'New Leave Type' : 'Edit Leave Type'}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
          {apiError && !apiError.validationErrors && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {apiError.message}
            </p>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="lt-code">Code</Label>
            <Input id="lt-code" disabled={mode === 'edit'} {...register('code')} />
            {mode === 'edit' && <p className="text-xs text-muted-foreground">Code can't be changed after creation.</p>}
            {errors.code && <p className="text-sm text-destructive">{errors.code.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="lt-name">Name</Label>
            <Input id="lt-name" {...register('name')} />
            {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="lt-maxDaysPerYear">Max days/year (blank = unlimited)</Label>
            <Input id="lt-maxDaysPerYear" type="number" min={1} {...register('maxDaysPerYear')} />
            {errors.maxDaysPerYear && <p className="text-sm text-destructive">{errors.maxDaysPerYear.message}</p>}
          </div>

          <div className="flex items-center justify-between rounded-md border border-border px-3 py-2.5">
            <Label htmlFor="lt-isPaid" className="cursor-pointer">Paid leave</Label>
            <Controller control={control} name="isPaid" render={({ field }) => <Switch id="lt-isPaid" checked={field.value} onCheckedChange={field.onChange} />} />
          </div>

          <div className="flex items-center justify-between rounded-md border border-border px-3 py-2.5">
            <Label htmlFor="lt-isActive" className="cursor-pointer">Active</Label>
            <Controller control={control} name="isActive" render={({ field }) => <Switch id="lt-isActive" checked={field.value} onCheckedChange={field.onChange} />} />
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
