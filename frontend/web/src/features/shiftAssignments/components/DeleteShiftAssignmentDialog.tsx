import type { ShiftAssignment } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface DeleteShiftAssignmentDialogProps {
  assignment: ShiftAssignment;
  isDeleting: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function DeleteShiftAssignmentDialog({ assignment, isDeleting, onConfirm, onCancel }: DeleteShiftAssignmentDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="delete-shift-assignment-title">
        <DialogHeader>
          <DialogTitle id="delete-shift-assignment-title">Delete shift assignment?</DialogTitle>
          <DialogDescription>
            This will remove the assignment for <strong className="text-foreground">{assignment.rosterDate}</strong> from active
            lists. The record is retained (soft delete).
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
