import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface UnsavedChangesDialogProps {
  open: boolean;
  onConfirmDiscard: () => void;
  onCancel: () => void;
  title?: string;
  description?: string;
}

/** Confirmation dialog paired with useUnsavedChangesGuard — same shape as
 * InvoiceCreatePage.tsx's original ad-hoc dialog, extracted so every form using the guard
 * doesn't redeclare the same three-paragraph JSX. */
export function UnsavedChangesDialog({
  open,
  onConfirmDiscard,
  onCancel,
  title = 'Discard unsaved changes?',
  description = "You've entered details that haven't been saved yet. Leaving now will lose everything entered on this page.",
}: UnsavedChangesDialogProps) {
  return (
    <Dialog open={open} onOpenChange={(nextOpen) => !nextOpen && onCancel()}>
      <DialogContent aria-labelledby="discard-changes-title">
        <DialogHeader>
          <DialogTitle id="discard-changes-title">{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel}>
            Keep editing
          </Button>
          <Button variant="destructive" onClick={onConfirmDiscard}>
            Discard and leave
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
