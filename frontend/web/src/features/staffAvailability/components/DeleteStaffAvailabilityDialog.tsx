import type { StaffAvailability } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface DeleteStaffAvailabilityDialogProps {
  record: StaffAvailability;
  isDeleting: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function DeleteStaffAvailabilityDialog({ record, isDeleting, onConfirm, onCancel }: DeleteStaffAvailabilityDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="delete-staff-availability-title">
        <DialogHeader>
          <DialogTitle id="delete-staff-availability-title">Delete availability record?</DialogTitle>
          <DialogDescription>
            This will remove this {record.availabilityStatus.toLowerCase()} record ({record.startDate} to {record.endDate}) from active
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
