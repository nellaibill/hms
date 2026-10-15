import type { Appointment } from '@hms/shared';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { APPOINTMENT_ID_PARAM } from '../checkIn';
import { PatientQuickSearch } from './PatientQuickSearch';

interface CheckInRegisteredPatientDialogProps {
  appointment: Appointment;
  onClose: () => void;
}

/** For a booking made as a "new caller" who turns out to be registered already: pick their
 * record and record the visit against it, instead of registering them a second time. */
export function CheckInRegisteredPatientDialog({ appointment, onClose }: CheckInRegisteredPatientDialogProps) {
  const navigate = useNavigate();

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="check-in-registered-title" className="max-w-xl">
        <DialogHeader>
          <DialogTitle id="check-in-registered-title">Check in as a registered patient</DialogTitle>
          <DialogDescription>
            Booked as a new caller: {appointment.patientName}, {appointment.phoneNumber}. Find their existing record — the search starts with the phone number.
          </DialogDescription>
        </DialogHeader>
        <PatientQuickSearch
          initialSearch={appointment.phoneNumber}
          onSelect={(patient) => navigate(`/patients/registration/${patient.id}/visits/new?${APPOINTMENT_ID_PARAM}=${appointment.id}`)}
        />
        <DialogFooter>
          <Button type="button" variant="outline" onClick={onClose}>
            Close
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
