import { useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import type { LabOrderItem } from '../types';

interface RejectForCorrectionDialogProps {
  testName: string;
  isSaving: boolean;
  onConfirm: (reason: string) => void;
  onCancel: () => void;
}

function RejectForCorrectionDialog({ testName, isSaving, onConfirm, onCancel }: RejectForCorrectionDialogProps) {
  const [reason, setReason] = useState('');
  const trimmedReason = reason.trim();

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="reject-correction-title">
        <DialogHeader>
          <DialogTitle id="reject-correction-title">Send back for correction?</DialogTitle>
          <DialogDescription>
            <strong className="text-foreground">{testName}</strong>&apos;s result will be sent back to the technician for correction.
          </DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="correction-reason">Reason</Label>
          <textarea
            id="correction-reason"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            placeholder="e.g. Value out of plausible range, re-check dilution"
            rows={3}
            autoFocus
            className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm transition-colors placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:cursor-not-allowed disabled:opacity-50"
          />
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isSaving}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={() => onConfirm(trimmedReason)} disabled={isSaving || trimmedReason.length === 0}>
            {isSaving ? 'Sending…' : 'Send Back for Correction'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

interface VerificationPanelProps {
  item: LabOrderItem;
  isVerifying: boolean;
  isRejecting: boolean;
  onVerify: () => void;
  onRejectForCorrection: (reason: string) => void;
}

/** Shown to a diagnostics.edit-permission user on an item that's PendingVerification —
 * read-only parameter display plus Verify / Reject for Correction. */
export function VerificationPanel({ item, isVerifying, isRejecting, onVerify, onRejectForCorrection }: VerificationPanelProps) {
  const [showRejectDialog, setShowRejectDialog] = useState(false);

  return (
    <div className="flex flex-col gap-3">
      {item.parameters.length === 0 ? (
        <p className="text-sm text-muted-foreground">No result parameters recorded.</p>
      ) : (
        <div className="overflow-x-auto rounded-md border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-3 py-2">Parameter</th>
                <th className="px-3 py-2">Result</th>
                <th className="px-3 py-2">Unit</th>
                <th className="px-3 py-2">Reference Range</th>
                <th className="px-3 py-2">Flag</th>
                <th className="px-3 py-2">Remarks</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {item.parameters.map((parameter) => (
                <tr key={parameter.id}>
                  <td className="px-3 py-2 font-medium text-foreground">{parameter.parameterName}</td>
                  <td className="px-3 py-2 text-foreground">{parameter.resultValue}</td>
                  <td className="px-3 py-2 text-muted-foreground">{parameter.unit ?? '—'}</td>
                  <td className="px-3 py-2 text-muted-foreground">{parameter.referenceRange ?? '—'}</td>
                  <td className="px-3 py-2">
                    {parameter.flag ? (
                      <Badge variant={parameter.flag === 'Normal' ? 'secondary' : parameter.flag === 'Critical' ? 'destructive' : 'warning'}>
                        {parameter.flag}
                      </Badge>
                    ) : (
                      <span className="text-muted-foreground">—</span>
                    )}
                  </td>
                  <td className="px-3 py-2 text-muted-foreground">{parameter.remarks ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" size="sm" disabled={isVerifying || isRejecting} onClick={() => setShowRejectDialog(true)}>
          Reject for Correction
        </Button>
        <Button type="button" size="sm" disabled={isVerifying || isRejecting} onClick={onVerify}>
          {isVerifying ? 'Verifying…' : 'Verify'}
        </Button>
      </div>

      {showRejectDialog && (
        <RejectForCorrectionDialog
          testName={item.testName}
          isSaving={isRejecting}
          onConfirm={(reason) => {
            onRejectForCorrection(reason);
            setShowRejectDialog(false);
          }}
          onCancel={() => setShowRejectDialog(false)}
        />
      )}
    </div>
  );
}
