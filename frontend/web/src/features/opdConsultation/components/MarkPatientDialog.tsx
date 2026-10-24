import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { textareaClassName } from './textareaClassName';

const MAX_REASON_LENGTH = 500;

interface MarkPatientDialogProps {
  patientName: string;
  /** The current reason when the patient is already marked — the dialog then edits it. */
  currentReason?: string | null;
  isMarked: boolean;
  isSaving: boolean;
  isRemoving: boolean;
  error?: string | null;
  onConfirm: (reason: string) => void;
  onRemove: () => void;
  onCancel: () => void;
}

/** "Mark Patient" on the OPD Consultation page — an optional reason, then Mark. When the patient
 * is already marked on this consultation, the same dialog updates the reason or removes the mark. */
export function MarkPatientDialog({ patientName, currentReason, isMarked, isSaving, isRemoving, error, onConfirm, onRemove, onCancel }: MarkPatientDialogProps) {
  const [reason, setReason] = useState(currentReason ?? '');
  const busy = isSaving || isRemoving;

  return (
    <Dialog open onOpenChange={(open) => !open && !busy && onCancel()}>
      <DialogContent aria-labelledby="mark-patient-title">
        <DialogHeader>
          <DialogTitle id="mark-patient-title">{isMarked ? 'Marked patient' : 'Mark patient'}</DialogTitle>
          <DialogDescription>
            {isMarked ? (
              <>
                <strong className="text-foreground">{patientName}</strong> is marked on this consultation. Update the reason or remove the mark.
              </>
            ) : (
              <>
                Mark <strong className="text-foreground">{patientName}</strong> on this consultation. The mark shows on the patient's Medical
                Information and in the OPD Marked Patients list.
              </>
            )}
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="mark-patient-reason">
            Reason <span className="font-normal text-muted-foreground">(optional)</span>
          </Label>
          <textarea
            id="mark-patient-reason"
            rows={3}
            maxLength={MAX_REASON_LENGTH}
            placeholder="Why is this patient being marked?"
            className={textareaClassName}
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            disabled={busy}
            autoFocus
          />
          <span className="self-end text-xs text-muted-foreground">
            {reason.length}/{MAX_REASON_LENGTH}
          </span>
        </div>

        {error && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error}
          </p>
        )}

        <DialogFooter className="gap-2 sm:justify-between">
          {isMarked ? (
            <Button variant="outline" onClick={onRemove} disabled={busy} className="text-destructive hover:text-destructive">
              {isRemoving ? 'Removing…' : 'Remove Mark'}
            </Button>
          ) : (
            <span />
          )}
          <div className="flex flex-col-reverse gap-2 sm:flex-row">
            <Button variant="outline" onClick={onCancel} disabled={busy}>
              Cancel
            </Button>
            <Button variant="success" onClick={() => onConfirm(reason)} disabled={busy}>
              {isSaving ? 'Saving…' : isMarked ? 'Update Reason' : 'Mark Patient'}
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
