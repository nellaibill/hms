import type { Patient } from '@hms/shared';
import { AlertTriangle } from 'lucide-react';
import { PatientNameLink } from '@/components/PatientNameLink';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { isSameFullName } from '../samePhonePatients';

interface SamePhonePatientsListProps {
  patients: Patient[];
  /** The name being registered — a patient with the same full name is flagged "Same name". */
  firstName?: string;
  lastName?: string;
}

/** Patients already registered with the number being typed into New Patient Registration. Each
 * name opens the record in a new tab, so the registration form isn't lost while checking. */
export function SamePhonePatientsList({ patients, firstName, lastName }: SamePhonePatientsListProps) {
  return (
    <ul className="flex flex-col divide-y divide-border rounded-md border border-border bg-background">
      {patients.map((patient) => (
        <li key={patient.id} className="flex flex-wrap items-center gap-x-3 gap-y-1 px-3 py-2 text-sm">
          <PatientNameLink patientId={patient.id} newTab>
            {[patient.firstName, patient.lastName].filter(Boolean).join(' ')}
          </PatientNameLink>
          <span className="text-muted-foreground">{patient.uhid}</span>
          <span className="text-muted-foreground">
            {patient.age} Yrs · {patient.gender}
          </span>
          {patient.lastVisitAppointmentTime && (
            <span className="text-muted-foreground">Last visit {new Date(patient.lastVisitAppointmentTime).toLocaleDateString('en-IN')}</span>
          )}
          {isSameFullName(patient, firstName, lastName) && <Badge variant="warning">Same name</Badge>}
          {patient.requiresDataVerification && <Badge variant="outline">Imported — needs verification</Badge>}
        </li>
      ))}
    </ul>
  );
}

/** Shown under the phone fields as soon as the number matches someone already registered. */
export function SamePhonePatientsNotice(props: SamePhonePatientsListProps) {
  const count = props.patients.length;
  return (
    <div role="status" className="flex w-full flex-col gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2.5 text-sm text-foreground">
      <p className="flex items-start gap-2">
        <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
        <span>
          <strong>
            This phone number is already registered to {count} {count === 1 ? 'patient' : 'patients'}.
          </strong>{' '}
          If the patient is one of them, use their existing record instead of registering them again — an imported record
          is updated from Edit Patient.
        </span>
      </p>
      <SamePhonePatientsList {...props} />
    </div>
  );
}

interface SamePhonePatientsDialogProps extends SamePhonePatientsListProps {
  phone: string;
  onRegisterNew: () => void;
  onCancel: () => void;
}

/** Asked once per phone number, right before a new patient is created with a number someone
 * already has — so a returning patient (above all one imported from the old system) isn't given
 * a second UHID without the receptionist deciding to. */
export function SamePhonePatientsDialog({ phone, onRegisterNew, onCancel, ...listProps }: SamePhonePatientsDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="same-phone-patients-title" className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle id="same-phone-patients-title">Is this patient already registered?</DialogTitle>
          <DialogDescription>
            {phone} is already registered to the {listProps.patients.length === 1 ? 'patient' : 'patients'} below. If this is
            one of them, cancel and use their existing record (an imported record is updated from Edit Patient). Register a
            new patient only if this is someone else, such as a family member sharing the number.
          </DialogDescription>
        </DialogHeader>
        <SamePhonePatientsList {...listProps} />
        <DialogFooter>
          <Button variant="outline" onClick={onCancel}>
            Cancel
          </Button>
          <Button onClick={onRegisterNew}>Register as a new patient</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
