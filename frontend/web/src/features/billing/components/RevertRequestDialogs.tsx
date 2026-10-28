import type { InvoiceRevertRequestResponse } from '@hms/shared';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { formatCurrency } from '../billingCalculations';

const textareaClassName =
  'flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2';

function ErrorLine({ error }: { error: unknown }) {
  if (!error) return null;
  return (
    <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
      {error instanceof Error ? error.message : 'Something went wrong. Please try again.'}
    </p>
  );
}

interface RequestRevertDialogProps {
  invoiceLabel: string;
  netAmount: number;
  paidAmount: number;
  isSaving: boolean;
  error: unknown;
  onConfirm: (reason: string) => void;
  onCancel: () => void;
}

/** Raises an OP bill revert request (ADR-106) — the reason is required, and the request goes to
 * the Super Admin for approval; the bill itself is unchanged until then. */
export function RequestRevertDialog({ invoiceLabel, netAmount, paidAmount, isSaving, error, onConfirm, onCancel }: RequestRevertDialogProps) {
  const [reason, setReason] = useState('');
  const trimmedReason = reason.trim();

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="request-revert-title">
        <DialogHeader>
          <DialogTitle id="request-revert-title">Request bill revert</DialogTitle>
          <DialogDescription>
            <strong className="text-foreground">{invoiceLabel}</strong> ({formatCurrency(netAmount)}
            {paidAmount > 0 ? `, ${formatCurrency(paidAmount)} paid` : ', not paid'}) will be sent to the Super Admin for approval. The bill
            stays as it is until they approve it.
          </DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="revert-reason">
            Reason for revert <span className="font-normal text-muted-foreground">(required)</span>
          </Label>
          <textarea
            id="revert-reason"
            rows={3}
            maxLength={500}
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            className={textareaClassName}
            placeholder="e.g. Wrong consultant billed, patient left before consultation"
            autoFocus
          />
        </div>
        <ErrorLine error={error} />
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isSaving}>
            Cancel
          </Button>
          <Button onClick={() => onConfirm(trimmedReason)} disabled={isSaving || trimmedReason.length === 0}>
            {isSaving ? 'Sending…' : 'Send for Approval'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

interface RevertDecisionDialogProps {
  decision: 'approve' | 'reject';
  request: InvoiceRevertRequestResponse;
  isSaving: boolean;
  error: unknown;
  onConfirm: (notes: string) => void;
  onCancel: () => void;
}

/** A Super Admin's approve/reject of a revert request. Approving reverts the bill; rejecting
 * needs a reason so the requester knows why. */
export function RevertDecisionDialog({ decision, request, isSaving, error, onConfirm, onCancel }: RevertDecisionDialogProps) {
  const [notes, setNotes] = useState('');
  const trimmedNotes = notes.trim();
  const isApprove = decision === 'approve';

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="revert-decision-title">
        <DialogHeader>
          <DialogTitle id="revert-decision-title">{isApprove ? 'Approve bill revert?' : 'Reject revert request?'}</DialogTitle>
          <DialogDescription>
            {isApprove ? (
              <>
                <strong className="text-foreground">Invoice {request.invoiceNumber}</strong> for {request.patientName} (
                {formatCurrency(request.netAmount)}) will be reverted and dropped from collections and reports. This cannot be undone.
                {request.paidAmount > 0 && <> Refund {formatCurrency(request.paidAmount)} to the patient at the counter.</>}
              </>
            ) : (
              <>
                <strong className="text-foreground">Invoice {request.invoiceNumber}</strong> stays as it is.
              </>
            )}
          </DialogDescription>
        </DialogHeader>
        <div className="rounded-md border border-border bg-muted/30 px-3 py-2 text-sm">
          <span className="text-muted-foreground">Reason given: </span>
          <span className="text-foreground">{request.reason}</span>
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="revert-decision-notes">
            {isApprove ? 'Notes' : 'Reason for rejecting'}{' '}
            <span className="font-normal text-muted-foreground">{isApprove ? '(optional)' : '(required)'}</span>
          </Label>
          <textarea
            id="revert-decision-notes"
            rows={2}
            maxLength={500}
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            className={textareaClassName}
            autoFocus
          />
        </div>
        <ErrorLine error={error} />
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isSaving}>
            Cancel
          </Button>
          <Button
            variant={isApprove ? 'destructive' : 'default'}
            onClick={() => onConfirm(trimmedNotes)}
            disabled={isSaving || (!isApprove && trimmedNotes.length === 0)}
          >
            {isSaving ? 'Saving…' : isApprove ? 'Approve & Revert' : 'Reject Request'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
