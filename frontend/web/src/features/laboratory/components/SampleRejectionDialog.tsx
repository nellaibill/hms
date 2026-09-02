import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { humanize } from '@/features/patients/humanize';
import { LAB_SAMPLE_REJECTION_REASONS, type LabSampleRejectionReason, type RejectSampleRequest } from '../types';

interface SampleRejectionDialogProps {
  testName: string;
  isSaving: boolean;
  onConfirm: (request: RejectSampleRequest) => void;
  onCancel: () => void;
}

export function SampleRejectionDialog({ testName, isSaving, onConfirm, onCancel }: SampleRejectionDialogProps) {
  const [reason, setReason] = useState<LabSampleRejectionReason>('InsufficientSample');
  const [remarks, setRemarks] = useState('');

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="reject-sample-title">
        <DialogHeader>
          <DialogTitle id="reject-sample-title">Reject sample?</DialogTitle>
          <DialogDescription>
            Reject the collected sample for <strong className="text-foreground">{testName}</strong>. A recollection can be requested afterward.
          </DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="rejection-reason">Reason</Label>
            <Select value={reason} onValueChange={(value) => setReason(value as LabSampleRejectionReason)}>
              <SelectTrigger id="rejection-reason">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {LAB_SAMPLE_REJECTION_REASONS.map((option) => (
                  <SelectItem key={option} value={option}>
                    {humanize(option)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="rejection-remarks">Remarks</Label>
            <textarea
              id="rejection-remarks"
              value={remarks}
              onChange={(event) => setRemarks(event.target.value)}
              placeholder="Optional details"
              rows={3}
              className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm transition-colors placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:cursor-not-allowed disabled:opacity-50"
            />
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isSaving}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={() => onConfirm({ reason, remarks: remarks.trim() || null })} disabled={isSaving}>
            {isSaving ? 'Rejecting…' : 'Reject Sample'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
