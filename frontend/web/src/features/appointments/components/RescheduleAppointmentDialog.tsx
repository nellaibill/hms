import type { Appointment } from '@hms/shared';
import { useState } from 'react';
import { ConsultantName } from '@/components/ConsultantName';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { toDisplayError } from '@/features/patients/apiErrorDisplay';
import { todayDateInputValue } from '@/lib/reportDateRange';
import { formatAppointmentDate, formatSlotTime } from '../format';
import { useRescheduleAppointmentMutation } from '../hooks';
import { SlotPicker } from './SlotPicker';

interface RescheduleAppointmentDialogProps {
  appointment: Appointment;
  onClose: () => void;
  onRescheduled: (appointment: Appointment) => void;
}

/** Moves a Booked appointment to another slot of the same consultant. To change the doctor,
 * cancel and book again. */
export function RescheduleAppointmentDialog({ appointment, onClose, onRescheduled }: RescheduleAppointmentDialogProps) {
  const mutation = useRescheduleAppointmentMutation();
  const today = todayDateInputValue();
  const [date, setDate] = useState(appointment.appointmentDate < today ? today : appointment.appointmentDate);
  const [startTime, setStartTime] = useState('');
  const apiError = toDisplayError(mutation.error);

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!startTime) return;
    mutation.mutate({ id: appointment.id, request: { appointmentDate: date, startTime } }, { onSuccess: onRescheduled });
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="reschedule-title" className="max-h-[92vh] max-w-2xl overflow-y-auto">
        <DialogHeader>
          <DialogTitle id="reschedule-title">Reschedule {appointment.patientName}</DialogTitle>
          <DialogDescription>
            Now: {formatAppointmentDate(appointment.appointmentDate)} at {formatSlotTime(appointment.startTime)} with{' '}
            <ConsultantName consultantId={appointment.consultantId} /> (token {appointment.tokenNumber}).
          </DialogDescription>
        </DialogHeader>

        {apiError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {apiError.message}
          </p>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex max-w-xs flex-col gap-1.5">
            <Label htmlFor="reschedule-date">New date</Label>
            <Input
              id="reschedule-date"
              type="date"
              min={today}
              value={date}
              onChange={(event) => {
                setDate(event.target.value);
                setStartTime('');
              }}
            />
          </div>
          <SlotPicker
            consultantId={appointment.consultantId}
            date={date}
            value={startTime}
            onChange={setStartTime}
            currentStartTime={date === appointment.appointmentDate ? appointment.startTime : undefined}
          />
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={mutation.isPending}>
              Keep current slot
            </Button>
            <Button type="submit" disabled={!startTime || mutation.isPending}>
              {mutation.isPending ? 'Moving…' : startTime ? `Move to ${formatSlotTime(startTime)}` : 'Pick a slot'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
