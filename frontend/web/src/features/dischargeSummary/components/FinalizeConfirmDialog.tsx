import type { ApiError } from '@hms/shared';
import { AlertTriangle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface FinalizeConfirmDialogProps {
  open: boolean;
  isSubmitting: boolean;
  apiError: ApiError | null;
  onOpenChange: (open: boolean) => void;
  onConfirm: () => void;
}

/**
 * Confirms the irreversible Finalize action — once a discharge summary is Finalized it can
 * no longer be edited (PUT is rejected once Status is Finalized), so this gate exists
 * specifically because the action can't be undone from the UI. The Prepared/Checked/
 * Consultant Approved By sign-off is chosen ahead of time on the Finalization section
 * (FinalizationCard) and simply carried through by the caller on confirm.
 */
export function FinalizeConfirmDialog({ open, isSubmitting, apiError, onOpenChange, onConfirm }: FinalizeConfirmDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-warning" />
            Finalize Discharge Summary
          </DialogTitle>
          <DialogDescription>
            Once finalized, this discharge summary can no longer be edited. This action cannot be undone. Continue?
          </DialogDescription>
        </DialogHeader>

        {apiError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {apiError.message}
          </p>
        )}

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="button" variant="destructive" onClick={onConfirm} disabled={isSubmitting}>
            {isSubmitting ? 'Finalizing…' : 'Finalize'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
