import type { ReactNode } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface ConfirmDeleteDialogProps {
  title: string;
  children: ReactNode;
  isPending: boolean;
  errorMessage?: string | null;
  onConfirm: () => void;
  onCancel: () => void;
}

/** "Delete …?" confirmation for a lab test, radiology service or package (ADR-140). The record is
 * soft-deleted and drops out of every list and lookup — the caller's text points to Inactive as
 * the way to stop billing something but keep it on record. */
export function ConfirmDeleteDialog({ title, children, isPending, errorMessage, onConfirm, onCancel }: ConfirmDeleteDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="confirm-delete-title">
        <DialogHeader>
          <DialogTitle id="confirm-delete-title">{title}</DialogTitle>
          <DialogDescription>{children}</DialogDescription>
        </DialogHeader>
        {errorMessage && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {errorMessage}
          </p>
        )}
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isPending}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={onConfirm} disabled={isPending}>
            {isPending ? 'Deleting…' : 'Delete'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
