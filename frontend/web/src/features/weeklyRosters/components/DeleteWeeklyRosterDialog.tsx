import type { WeeklyRoster } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface DeleteWeeklyRosterDialogProps {
  roster: WeeklyRoster;
  isDeleting: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function DeleteWeeklyRosterDialog({ roster, isDeleting, onConfirm, onCancel }: DeleteWeeklyRosterDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="delete-weekly-roster-title">
        <DialogHeader>
          <DialogTitle id="delete-weekly-roster-title">Delete weekly roster?</DialogTitle>
          <DialogDescription>
            This will remove the roster for week <strong className="text-foreground">{roster.weekStartDate}</strong> from active
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
