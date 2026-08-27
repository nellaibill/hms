import { ApiError, rejectLeaveRequestSchema, type LeaveRequestResponse, type RejectLeaveRequestFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { useRejectLeaveRequestMutation } from '../hooks/useLeaveRequestMutations';

interface RejectLeaveRequestDialogProps {
  leaveRequest: LeaveRequestResponse;
  onClose: () => void;
}

/** A rejection reason is required — mirrors the backend's own RejectLeaveRequestRequestValidator,
 * enforced here client-side too so the form doesn't round-trip on a guaranteed 400. */
export function RejectLeaveRequestDialog({ leaveRequest, onClose }: RejectLeaveRequestDialogProps) {
  const mutation = useRejectLeaveRequestMutation();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RejectLeaveRequestFormValues>({
    resolver: zodResolver(rejectLeaveRequestSchema),
    defaultValues: { reason: '' },
  });

  function onSubmit(values: RejectLeaveRequestFormValues) {
    mutation.mutate({ id: leaveRequest.id, request: { reason: values.reason } }, { onSuccess: onClose });
  }

  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="reject-leave-request-title">
        <DialogHeader>
          <DialogTitle id="reject-leave-request-title">Reject leave request?</DialogTitle>
          <DialogDescription>
            {leaveRequest.employeeName}'s {leaveRequest.leaveTypeName} request ({leaveRequest.startDate} – {leaveRequest.endDate}).
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
          {apiError && !apiError.validationErrors && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {apiError.message}
            </p>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="reject-reason">Reason</Label>
            <textarea
              id="reject-reason"
              rows={2}
              className="flex w-full resize-none rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm transition-colors placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
              {...register('reason')}
            />
            {errors.reason && <p className="text-sm text-destructive">{errors.reason.message}</p>}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={mutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" variant="destructive" disabled={mutation.isPending}>
              {mutation.isPending ? 'Rejecting…' : 'Reject'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
