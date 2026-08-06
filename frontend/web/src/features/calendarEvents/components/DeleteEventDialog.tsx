import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import type { CalendarEvent } from '../types';

interface DeleteEventDialogProps {
  event: CalendarEvent;
  isDeleting?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function DeleteEventDialog({ event, isDeleting, onConfirm, onCancel }: DeleteEventDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="delete-event-title">
        <DialogHeader>
          <DialogTitle id="delete-event-title">Delete event?</DialogTitle>
          <DialogDescription>
            This action cannot be undone. <strong className="text-foreground">{event.title}</strong> will be permanently removed
            from the calendar.
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
