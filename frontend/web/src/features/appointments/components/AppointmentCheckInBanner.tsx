import type { Appointment } from '@hms/shared';
import { CalendarCheck, TriangleAlert } from 'lucide-react';
import { ConsultantName } from '@/components/ConsultantName';
import { appointmentStatusLabel, formatAppointmentDate, formatSlotTime } from '../format';

interface AppointmentCheckInBannerProps {
  appointment: Appointment | null;
  staleAppointment?: Appointment | null;
}

/** Tells reception, on Add Visit / New Patient Registration, that saving will also check in a
 * booked appointment — or that the booking in the link can no longer be checked in. */
export function AppointmentCheckInBanner({ appointment, staleAppointment }: AppointmentCheckInBannerProps) {
  if (appointment) {
    return (
      <div className="flex items-start gap-2 rounded-md border border-primary/30 bg-primary/5 px-3 py-2.5 text-sm text-foreground" role="status">
        <CalendarCheck className="mt-0.5 h-4 w-4 shrink-0 text-primary" />
        <p>
          Checking in appointment <span className="font-medium">{appointment.appointmentNumber}</span> · token{' '}
          <span className="font-semibold">{appointment.tokenNumber}</span> · {formatAppointmentDate(appointment.appointmentDate)}{' '}
          {formatSlotTime(appointment.startTime)} with <ConsultantName consultantId={appointment.consultantId} />. The doctor is pre-filled; saving the
          visit marks the appointment as arrived.
        </p>
      </div>
    );
  }

  if (staleAppointment) {
    return (
      <div className="flex items-start gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2.5 text-sm text-foreground" role="status">
        <TriangleAlert className="mt-0.5 h-4 w-4 shrink-0" />
        <p>
          Appointment {staleAppointment.appointmentNumber} is already {appointmentStatusLabel(staleAppointment.status).toLowerCase()}, so this visit will be
          recorded on its own.
        </p>
      </div>
    );
  }

  return null;
}
