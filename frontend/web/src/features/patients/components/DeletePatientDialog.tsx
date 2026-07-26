import type { Patient } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface DeletePatientDialogProps {
  patient: Patient;
  isDeleting: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function DeletePatientDialog({ patient, isDeleting, onConfirm, onCancel }: DeletePatientDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="delete-patient-title">
        <DialogHeader>
          <DialogTitle id="delete-patient-title">Delete patient?</DialogTitle>
          <DialogDescription>
            This will remove{' '}
            <strong className="text-foreground">
              {patient.firstName} {patient.lastName}
            </strong>{' '}
            (UHID {patient.uhid}) from active lists. The record is retained (soft delete).
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isDeleting}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={onConfirm} disabled={isDeleting}>
            {isDeleting ? 'Deleting…' : 'Delete'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
