import { ApiError, approveLeaveRequestSchema, type ApproveLeaveRequestFormValues, type LeaveRequestResponse } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { useApproveLeaveRequestMutation } from '../hooks/useLeaveRequestMutations';

interface ApproveLeaveRequestDialogProps {
  leaveRequest: LeaveRequestResponse;
  onClose: () => void;
}

export function ApproveLeaveRequestDialog({ leaveRequest, onClose }: ApproveLeaveRequestDialogProps) {
  const mutation = useApproveLeaveRequestMutation();
  const { register, handleSubmit } = useForm<ApproveLeaveRequestFormValues>({
    resolver: zodResolver(approveLeaveRequestSchema),
    defaultValues: { notes: '' },
  });

  function onSubmit(values: ApproveLeaveRequestFormValues) {
    mutation.mutate({ id: leaveRequest.id, request: { notes: values.notes || null } }, { onSuccess: onClose });
  }

  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="approve-leave-request-title">
        <DialogHeader>
          <DialogTitle id="approve-leave-request-title">Approve leave request?</DialogTitle>
          <DialogDescription>
            {leaveRequest.employeeName}'s {leaveRequest.leaveTypeName} request ({leaveRequest.startDate} – {leaveRequest.endDate}).
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-4">
          {apiError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {apiError.message}
            </p>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="approve-notes">Notes (optional)</Label>
            <textarea
              id="approve-notes"
              rows={2}
              className="flex w-full resize-none rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm transition-colors placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
              {...register('notes')}
            />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={mutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? 'Approving…' : 'Approve'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
