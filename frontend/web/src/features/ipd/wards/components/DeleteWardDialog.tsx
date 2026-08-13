import type { Ward } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface DeleteWardDialogProps {
  ward: Ward;
  isDeleting: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function DeleteWardDialog({ ward, isDeleting, onConfirm, onCancel }: DeleteWardDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="delete-ward-title">
        <DialogHeader>
          <DialogTitle id="delete-ward-title">Delete ward?</DialogTitle>
          <DialogDescription>
            This will remove <strong className="text-foreground">{ward.name}</strong> ({ward.code}) from active lists. The
            record is retained (soft delete).
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
