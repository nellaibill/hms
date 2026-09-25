import type { Patient } from '@hms/shared';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface VerifyPatientDialogProps {
  patient: Patient;
  /** What's being blocked, completing "before ___" in the dialog copy — e.g. "adding a visit". */
  action: string;
  onCancel: () => void;
}

/** Blocks an action (Add Visit, OPD Billing) for a patient still flagged Patient.requiresDataVerification (placeholder
 * data from bulk import) — recording a visit against unverified demographics/contact details
 * would carry that bad data straight into billing/OPD/IPD, so this is a hard stop rather than
 * just a warning banner: the only way out is Edit Patient (which clears the flag on save) or
 * Cancel. Used by PatientSummaryCard's Add Visit button and InvoiceCreatePage's patient picker. */
export function VerifyPatientDialog({ patient, action, onCancel }: VerifyPatientDialogProps) {
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
            details before {action}.
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
