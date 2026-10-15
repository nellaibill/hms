import type { Appointment } from '@hms/shared';
import { CalendarClock, LogIn, MoreHorizontal, UserSearch, UserX, XCircle } from 'lucide-react';
import { AppointmentTypeName } from '@/components/AppointmentTypeName';
import { ConsultantName } from '@/components/ConsultantName';
import { DepartmentName } from '@/components/DepartmentName';
import { PatientNameLink } from '@/components/PatientNameLink';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { formatSlotTime } from '../format';
import { AppointmentStatusBadge } from './AppointmentStatusBadge';

export type AppointmentAction = 'check-in' | 'check-in-registered' | 'reschedule' | 'cancel' | 'no-show';

interface AppointmentsTableProps {
  appointments: Appointment[];
  today: string;
  /** Hides the action column for users who can read but not change appointments. */
  canEdit: boolean;
  /** Shows the date column — off for the single-day diary, on for wider lists. */
  showDate?: boolean;
  busyId?: string | null;
  onAction: (action: AppointmentAction, appointment: Appointment) => void;
}

/** One row per booking, in token order. Check-in is the main action on the day; the rest sit
 * in a menu so the row stays one line. */
export function AppointmentsTable({ appointments, today, canEdit, showDate, busyId, onAction }: AppointmentsTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-border">
      <table className="w-full text-sm">
        <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
          <tr>
            <th className="px-4 py-2.5">Token</th>
            {showDate && <th className="px-4 py-2.5">Date</th>}
            <th className="px-4 py-2.5">Time</th>
            <th className="px-4 py-2.5">Patient</th>
            <th className="px-4 py-2.5">Phone</th>
            <th className="px-4 py-2.5">Consultant</th>
            <th className="px-4 py-2.5">Department</th>
            <th className="px-4 py-2.5">Type</th>
            <th className="px-4 py-2.5">Status</th>
            {canEdit && <th className="px-4 py-2.5 text-right">Action</th>}
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {appointments.map((appointment) => {
            const isBooked = appointment.status === 'Booked';
            const isToday = appointment.appointmentDate === today;
            const isDue = appointment.appointmentDate <= today;
            return (
              <tr key={appointment.id} className={isBooked ? 'hover:bg-muted/30' : 'text-muted-foreground hover:bg-muted/30'}>
                <td className="px-4 py-3">
                  <span className="inline-flex h-7 min-w-7 items-center justify-center rounded-md bg-primary/10 px-1.5 font-semibold text-primary">
                    {appointment.tokenNumber}
                  </span>
                </td>
                {showDate && <td className="whitespace-nowrap px-4 py-3">{appointment.appointmentDate}</td>}
                <td className="whitespace-nowrap px-4 py-3 font-medium text-foreground">{formatSlotTime(appointment.startTime)}</td>
                <td className="px-4 py-3">
                  <div className="flex flex-col gap-0.5">
                    <span className="flex flex-wrap items-center gap-2 whitespace-nowrap">
                      {appointment.patientId ? (
                        <PatientNameLink patientId={appointment.patientId}>{appointment.patientName}</PatientNameLink>
                      ) : (
                        <span className="font-medium text-foreground">{appointment.patientName}</span>
                      )}
                      {appointment.isNewPatient ? (
                        <Badge variant="outline" className="text-[10px]">
                          New
                        </Badge>
                      ) : (
                        <span className="font-mono text-xs text-muted-foreground">{appointment.uhid}</span>
                      )}
                    </span>
                    {appointment.status === 'Cancelled' && appointment.cancelReason ? (
                      <span className="max-w-[18rem] truncate text-xs text-muted-foreground" title={appointment.cancelReason}>
                        Cancelled: {appointment.cancelReason}
                      </span>
                    ) : (
                      appointment.notes && (
                        <span className="max-w-[18rem] truncate text-xs text-muted-foreground" title={appointment.notes}>
                          {appointment.notes}
                        </span>
                      )
                    )}
                  </div>
                </td>
                <td className="whitespace-nowrap px-4 py-3">{appointment.phoneNumber}</td>
                <td className="whitespace-nowrap px-4 py-3">
                  <ConsultantName consultantId={appointment.consultantId} />
                </td>
                <td className="whitespace-nowrap px-4 py-3">
                  <DepartmentName departmentId={appointment.departmentId} />
                </td>
                <td className="whitespace-nowrap px-4 py-3">
                  {appointment.appointmentTypeId ? <AppointmentTypeName appointmentTypeId={appointment.appointmentTypeId} /> : '—'}
                </td>
                <td className="whitespace-nowrap px-4 py-3">
                  <AppointmentStatusBadge status={appointment.status} />
                  {appointment.rescheduleCount > 0 && isBooked && <span className="ml-1.5 text-xs text-muted-foreground">moved ×{appointment.rescheduleCount}</span>}
                </td>
                {canEdit && (
                  <td className="whitespace-nowrap px-4 py-3">
                    {isBooked && (
                      <div className="flex items-center justify-end gap-1.5">
                        {isToday && (
                          <Button size="sm" className="gap-1.5" disabled={busyId === appointment.id} onClick={() => onAction('check-in', appointment)}>
                            <LogIn className="h-3.5 w-3.5" />
                            Check in
                          </Button>
                        )}
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button size="sm" variant="outline" aria-label={`More actions for ${appointment.patientName}`} disabled={busyId === appointment.id}>
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            {isToday && appointment.isNewPatient && (
                              <DropdownMenuItem onSelect={() => onAction('check-in-registered', appointment)}>
                                <UserSearch className="h-4 w-4" />
                                Check in as registered patient
                              </DropdownMenuItem>
                            )}
                            <DropdownMenuItem onSelect={() => onAction('reschedule', appointment)}>
                              <CalendarClock className="h-4 w-4" />
                              Reschedule
                            </DropdownMenuItem>
                            {isDue && (
                              <DropdownMenuItem onSelect={() => onAction('no-show', appointment)}>
                                <UserX className="h-4 w-4" />
                                Mark no-show
                              </DropdownMenuItem>
                            )}
                            <DropdownMenuItem className="text-destructive focus:text-destructive" onSelect={() => onAction('cancel', appointment)}>
                              <XCircle className="h-4 w-4" />
                              Cancel
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </div>
                    )}
                  </td>
                )}
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
