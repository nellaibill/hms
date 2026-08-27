import { ApiError, createLeaveRequestSchema, type LeaveRequestFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQuery } from '@tanstack/react-query';
import { Controller, useForm } from 'react-hook-form';
import { EmployeeSelect } from '@/components/EmployeeSelect';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { leaveTypesApi } from '@/services/apiClient';
import { useCreateLeaveRequestMutation } from '../hooks/useLeaveRequestMutations';

interface NewLeaveRequestDialogProps {
  /** Pre-selects (and locks) the employee — used from the Employee Profile page's own
   * "Request Leave" action. Left undefined for the Leave Requests list's own "New Leave
   * Request" action, where any employee can be picked. */
  defaultEmployeeId?: string;
  onClose: () => void;
}

export function NewLeaveRequestDialog({ defaultEmployeeId, onClose }: NewLeaveRequestDialogProps) {
  const mutation = useCreateLeaveRequestMutation();

  const leaveTypesQuery = useQuery({
    queryKey: ['leaveTypes', 'select-list'],
    queryFn: () => leaveTypesApi.getLeaveTypes({ pageSize: 100, isActive: true }),
  });
  const leaveTypeOptions = (leaveTypesQuery.data?.items ?? []).map((leaveType) => ({
    value: leaveType.id,
    label: `${leaveType.name} (${leaveType.code})`,
    keywords: leaveType.code,
  }));

  const {
    control,
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LeaveRequestFormValues>({
    resolver: zodResolver(createLeaveRequestSchema),
    defaultValues: { employeeId: defaultEmployeeId ?? '', leaveTypeId: '', startDate: '', endDate: '', reason: '' },
  });

  function onSubmit(values: LeaveRequestFormValues) {
    mutation.mutate(values, { onSuccess: onClose });
  }

  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="new-leave-request-title">
        <DialogHeader>
          <DialogTitle id="new-leave-request-title">New Leave Request</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
          {apiError && !apiError.validationErrors && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {apiError.message}
            </p>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="lr-employeeId">Employee</Label>
            <Controller
              control={control}
              name="employeeId"
              render={({ field }) => (
                <EmployeeSelect id="lr-employeeId" value={field.value} onValueChange={field.onChange} disabled={Boolean(defaultEmployeeId)} />
              )}
            />
            {errors.employeeId && <p className="text-sm text-destructive">{errors.employeeId.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="lr-leaveTypeId">Leave Type</Label>
            <Controller
              control={control}
              name="leaveTypeId"
              render={({ field }) => (
                <SearchableSelect
                  id="lr-leaveTypeId"
                  value={field.value}
                  onValueChange={field.onChange}
                  options={leaveTypeOptions}
                  placeholder="Select leave type…"
                  searchPlaceholder="Search by name or code…"
                  ariaLabel="Leave type"
                />
              )}
            />
            {errors.leaveTypeId && <p className="text-sm text-destructive">{errors.leaveTypeId.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="lr-startDate">Start date</Label>
              <Input id="lr-startDate" type="date" {...register('startDate')} />
              {errors.startDate && <p className="text-sm text-destructive">{errors.startDate.message}</p>}
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="lr-endDate">End date</Label>
              <Input id="lr-endDate" type="date" {...register('endDate')} />
              {errors.endDate && <p className="text-sm text-destructive">{errors.endDate.message}</p>}
            </div>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="lr-reason">Reason</Label>
            <textarea
              id="lr-reason"
              rows={3}
              className="flex w-full resize-none rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm transition-colors placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:cursor-not-allowed disabled:opacity-50"
              {...register('reason')}
            />
            {errors.reason && <p className="text-sm text-destructive">{errors.reason.message}</p>}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={mutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? 'Submitting…' : 'Submit Request'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
