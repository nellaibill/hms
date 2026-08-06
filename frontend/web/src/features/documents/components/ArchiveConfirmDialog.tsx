import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import type { HmsDocument } from '../types';

interface ArchiveConfirmDialogProps {
  doc: HmsDocument;
  isArchiving?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function ArchiveConfirmDialog({ doc, isArchiving, onConfirm, onCancel }: ArchiveConfirmDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="archive-document-title">
        <DialogHeader>
          <DialogTitle id="archive-document-title">Archive Document</DialogTitle>
          <DialogDescription>
            <strong className="text-foreground">{doc.originalFileName}</strong> will be hidden from active searches. You can still
            find it later by filtering for archived documents.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isArchiving}>
            Cancel
          </Button>
          <Button onClick={onConfirm} disabled={isArchiving}>
            {isArchiving ? 'Archiving…' : 'Archive'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
