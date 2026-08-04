import type { SwapRequest } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface DeleteSwapRequestDialogProps {
  request: SwapRequest;
  isDeleting: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function DeleteSwapRequestDialog({ request, isDeleting, onConfirm, onCancel }: DeleteSwapRequestDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="delete-swap-request-title">
        <DialogHeader>
          <DialogTitle id="delete-swap-request-title">Delete shift swap request?</DialogTitle>
          <DialogDescription>
            This will remove this {request.status.toLowerCase()} swap request from active lists. The record is retained
            (soft delete).
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
