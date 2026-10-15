import type { Appointment } from '@hms/shared';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { toDisplayError } from '@/features/patients/apiErrorDisplay';
import { formatAppointmentDate, formatSlotTime } from '../format';
import { useCancelAppointmentMutation } from '../hooks';

const QUICK_REASONS = ['Patient asked to cancel', 'Doctor not available', 'Booked by mistake'] as const;

interface CancelAppointmentDialogProps {
  appointment: Appointment;
  onClose: () => void;
  onCancelled: (appointment: Appointment) => void;
}

export function CancelAppointmentDialog({ appointment, onClose, onCancelled }: CancelAppointmentDialogProps) {
  const mutation = useCancelAppointmentMutation();
  const [reason, setReason] = useState('');
  const apiError = toDisplayError(mutation.error);

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!reason.trim()) return;
    mutation.mutate({ id: appointment.id, request: { reason: reason.trim() } }, { onSuccess: onCancelled });
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="cancel-appointment-title" className="max-w-md">
        <DialogHeader>
          <DialogTitle id="cancel-appointment-title">Cancel appointment</DialogTitle>
          <DialogDescription>
            {appointment.patientName} · {formatAppointmentDate(appointment.appointmentDate)} at {formatSlotTime(appointment.startTime)}. The slot opens up
            for someone else.
          </DialogDescription>
        </DialogHeader>

        {apiError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {apiError.message}
          </p>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-3">
          <div className="flex flex-wrap gap-1.5">
            {QUICK_REASONS.map((quick) => (
              <Button key={quick} type="button" size="sm" variant={reason === quick ? 'default' : 'outline'} onClick={() => setReason(quick)}>
                {quick}
              </Button>
            ))}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="cancel-reason">Reason</Label>
            <textarea
              id="cancel-reason"
              rows={2}
              maxLength={300}
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              placeholder="Why is it being cancelled?"
            />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={mutation.isPending}>
              Keep appointment
            </Button>
            <Button type="submit" variant="destructive" disabled={!reason.trim() || mutation.isPending}>
              {mutation.isPending ? 'Cancelling…' : 'Cancel appointment'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
