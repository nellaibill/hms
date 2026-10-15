import type { Patient } from '@hms/shared';
import { CalendarClock, Loader2, Plus } from 'lucide-react';
import { useState } from 'react';
import { ConsultantName } from '@/components/ConsultantName';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { todayDateInputValue } from '@/lib/reportDateRange';
import { formatAppointmentDate, formatSlotTime } from '../format';
import { useAppointmentsQuery } from '../hooks';
import { AppointmentStatusBadge } from './AppointmentStatusBadge';
import { BookAppointmentDialog } from './BookAppointmentDialog';

/** The patient's upcoming bookings on their Overview tab, with a shortcut to book another.
 * Renders nothing when the hospital doesn't have the Appointments feature. */
export function PatientAppointmentsCard({ patient }: { patient: Patient }) {
  const { hasFeature, hasPermission } = useAuth();
  const { toast } = useToast();
  const [isBooking, setIsBooking] = useState(false);
  const today = todayDateInputValue();
  const enabled = hasFeature('appointments') && (hasPermission('patient-management.view') || hasPermission('clinical-care.view'));

  const { data, isPending } = useAppointmentsQuery({ patientId: patient.id, fromDate: today, status: 'Booked', pageSize: 5 }, { enabled });

  if (!enabled) return null;

  return (
    <div className="flex flex-col overflow-hidden rounded-lg border border-foreground/15 bg-card">
      <div className="flex items-center justify-between gap-1.5 bg-sidebar-accent px-3 py-2 text-sidebar-foreground">
        <h2 className="flex items-center gap-1.5 text-sm font-semibold">
          <CalendarClock className="h-4 w-4 shrink-0" />
          Upcoming Appointments
        </h2>
        {hasPermission('patient-management.create') && (
          <Button type="button" size="sm" variant="outline" className="h-7 gap-1 bg-background" onClick={() => setIsBooking(true)}>
            <Plus className="h-3.5 w-3.5" />
            Book
          </Button>
        )}
      </div>
      <div className="px-3 py-2.5">
        {isPending ? (
          <div className="flex items-center gap-2 py-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading appointments…
          </div>
        ) : !data || data.items.length === 0 ? (
          <p className="py-1 text-sm text-muted-foreground">No upcoming appointments.</p>
        ) : (
          <ul className="divide-y divide-border">
            {data.items.map((appointment) => (
              <li key={appointment.id} className="flex flex-wrap items-center justify-between gap-2 py-2 text-sm">
                <span className="flex items-center gap-2">
                  <span className="inline-flex h-6 min-w-6 items-center justify-center rounded bg-primary/10 px-1 text-xs font-semibold text-primary">
                    {appointment.tokenNumber}
                  </span>
                  <span className="font-medium text-foreground">
                    {formatAppointmentDate(appointment.appointmentDate)}, {formatSlotTime(appointment.startTime)}
                  </span>
                  <span className="text-muted-foreground">
                    · <ConsultantName consultantId={appointment.consultantId} />
                  </span>
                </span>
                <AppointmentStatusBadge status={appointment.status} />
              </li>
            ))}
          </ul>
        )}
      </div>

      {isBooking && (
        <BookAppointmentDialog
          defaults={{ date: today, patient, departmentId: patient.lastVisitDepartmentId ?? undefined, consultantId: patient.lastVisitConsultantId ?? undefined }}
          onClose={() => setIsBooking(false)}
          onBooked={(appointment) => {
            setIsBooking(false);
            toast({
              title: `Appointment booked — token ${appointment.tokenNumber}`,
              description: `${formatAppointmentDate(appointment.appointmentDate)} at ${formatSlotTime(appointment.startTime)} (${appointment.appointmentNumber}).`,
              variant: 'success',
            });
          }}
        />
      )}
    </div>
  );
}
