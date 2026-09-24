import type { Patient } from '@hms/shared';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface VerifyPatientDialogProps {
  patient: Patient;
  onCancel: () => void;
}

/** Blocks "Add Visit" for a patient still flagged Patient.requiresDataVerification (placeholder
 * data from bulk import) — recording a visit against unverified demographics/contact details
 * would carry that bad data straight into billing/OPD/IPD, so this is a hard stop rather than
 * just a warning banner: the only way out is Edit Patient (which clears the flag on save) or
 * Cancel. See PatientSummaryCard's Add Visit button, the only entry point into this flow. */
export function VerifyPatientDialog({ patient, onCancel }: VerifyPatientDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="verify-patient-title">
        <DialogHeader>
          <DialogTitle id="verify-patient-title">Verify patient details first</DialogTitle>
          <DialogDescription>
            <strong className="text-foreground">
              {patient.firstName} {patient.lastName}
            </strong>{' '}
            (UHID {patient.uhid}) still has placeholder data from import. Please edit and verify the patient's
            details before adding a visit.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel}>
            Cancel
          </Button>
          <Button asChild>
            <Link to={`/patients/registration/${patient.id}/edit`}>Edit Patient</Link>
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
