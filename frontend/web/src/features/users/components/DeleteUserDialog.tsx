import type { User } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface DeleteUserDialogProps {
  user: User;
  isDeleting: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function DeleteUserDialog({ user, isDeleting, onConfirm, onCancel }: DeleteUserDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="delete-user-title">
        <DialogHeader>
          <DialogTitle id="delete-user-title">Delete user?</DialogTitle>
          <DialogDescription>
            This will remove <strong className="text-foreground">{user.firstName} {user.lastName}</strong> ({user.email}
            ) from active lists. The record is retained (soft delete) — see docs/DatabaseArchitecture.md §6.
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
