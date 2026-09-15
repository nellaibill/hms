import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface ReopenConsultationDialogProps {
  isSaving: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

/** Confirms the "Reopen" action on a Completed OPD consultation before calling it — reopening
 * moves the note back to Draft (editable again) and reverts the OPD queue's own status from
 * Completed back to InConsultation, so it's worth a deliberate confirm step, same as
 * VoidInvoiceDialog's pattern for a comparable state-reverting action. */
export function ReopenConsultationDialog({ isSaving, onConfirm, onCancel }: ReopenConsultationDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="reopen-consultation-title">
        <DialogHeader>
          <DialogTitle id="reopen-consultation-title">Reopen this consultation?</DialogTitle>
          <DialogDescription>
            The consultation will move back to <strong className="text-foreground">In Consultation</strong> and become editable again. It
            will need to be completed again once changes are made.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isSaving}>
            Cancel
          </Button>
          <Button onClick={onConfirm} disabled={isSaving}>
            {isSaving ? 'Reopening…' : 'Reopen Consultation'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
