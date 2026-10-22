import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';

interface IssueIpNumberDialogProps {
  patientName: string;
  isIssuing: boolean;
  error?: string | null;
  onConfirm: () => void;
  onCancel: () => void;
}

/** Confirms "Issue IP No without advance" (ADR-104) — a supervisor waiving the deposit, e.g.
 * cashless insurance. The number can't be taken back once issued, so it asks first. */
export function IssueIpNumberDialog({ patientName, isIssuing, error, onConfirm, onCancel }: IssueIpNumberDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent role="alertdialog" aria-labelledby="issue-ip-number-title">
        <DialogHeader>
          <DialogTitle id="issue-ip-number-title">Issue IP No without advance?</DialogTitle>
          <DialogDescription>
            <strong className="text-foreground">{patientName}</strong> has not paid an advance. The IP No is issued now, can't be
            taken back, and is recorded in the activity log under your name.
          </DialogDescription>
        </DialogHeader>
        {error && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error}
          </p>
        )}
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isIssuing}>
            Cancel
          </Button>
          <Button onClick={onConfirm} disabled={isIssuing}>
            {isIssuing ? 'Issuing…' : 'Issue IP No'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
