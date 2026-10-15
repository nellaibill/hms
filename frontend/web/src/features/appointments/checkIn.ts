import type { Appointment, CreatePatientVisitRequest } from '@hms/shared';
import { useSearchParams } from 'react-router-dom';
import { useToast } from '@/components/ui/toast-context';
import { slotStartIso, splitPatientName } from './format';
import { useAppointmentQuery, useMarkArrivedMutation } from './hooks';

export const APPOINTMENT_ID_PARAM = 'appointmentId';

/** Where "Check in" goes: Add Visit for a registered patient, New Patient Registration for a
 * first-time caller. Either page reads ?appointmentId=, pre-fills itself from the booking and
 * marks the appointment Arrived once the visit is saved (useAppointmentCheckIn). */
export function checkInPath(appointment: Appointment): string {
  return appointment.patientId
    ? `/patients/registration/${appointment.patientId}/visits/new?${APPOINTMENT_ID_PARAM}=${appointment.id}`
    : `/patients/registration/new?${APPOINTMENT_ID_PARAM}=${appointment.id}`;
}

/** New Patient Registration pre-fill for a booked first-time caller: their name (first word,
 * then the rest), phone, and the doctor they're booked with. */
export function registrationPrefill(appointment: Appointment) {
  return {
    ...splitPatientName(appointment.patientName),
    primaryPhone: { number: appointment.phoneNumber },
    registration: {
      encounterType: 'OP' as const,
      departmentId: appointment.departmentId,
      consultantId: appointment.consultantId,
      appointmentTypeId: appointment.appointmentTypeId ?? '',
    },
  };
}

/**
 * The check-in half of a registration page. The visit is recorded the normal way first; the
 * appointment is then linked to it. If that second step fails, the visit stays saved (it is the
 * record that matters at the desk) and reception is told to mark the appointment from the
 * Appointments page instead.
 */
export function useAppointmentCheckIn() {
  const [params] = useSearchParams();
  const appointmentId = params.get(APPOINTMENT_ID_PARAM);
  const query = useAppointmentQuery(appointmentId);
  const arrive = useMarkArrivedMutation();
  const { toast } = useToast();

  const appointment = query.data && query.data.status === 'Booked' ? query.data : null;

  /** Books the visit at the slot's start time for the consultant the appointment is with, so
   * the OPD queue keeps the booked order instead of arrival order. */
  function applyToVisitRequest(request: CreatePatientVisitRequest): CreatePatientVisitRequest {
    if (!appointment) return request;
    const appointmentTime = slotStartIso(appointment.appointmentDate, appointment.startTime);
    return {
      ...request,
      consultations: request.consultations.map((consultation) =>
        consultation.consultantId === appointment.consultantId ? { ...consultation, appointmentTime } : consultation,
      ),
    };
  }

  async function complete(patientId: string, visitId: string): Promise<void> {
    if (!appointment) return;
    try {
      const updated = await arrive.mutateAsync({ id: appointment.id, request: { patientId, visitId } });
      toast({
        title: `Checked in — token ${updated.tokenNumber}`,
        description: `${updated.patientName}'s appointment ${updated.appointmentNumber} is marked as arrived.`,
        variant: 'success',
      });
    } catch (error) {
      toast({
        title: 'Visit saved, but the appointment was not updated',
        description: `${error instanceof Error ? error.message : 'Something went wrong.'} Check the patient in from the Appointments page.`,
        variant: 'warning',
      });
    }
  }

  return {
    appointmentId,
    appointment,
    /** Set when ?appointmentId= points at a booking that can no longer be checked in. */
    staleAppointment: query.data && query.data.status !== 'Booked' ? query.data : null,
    isLoading: Boolean(appointmentId) && query.isPending,
    applyToVisitRequest,
    complete,
  };
}
