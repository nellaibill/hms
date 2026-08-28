import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface VoidInvoiceDialogProps {
  invoiceLabel: string;
  isSaving: boolean;
  onConfirm: (reason: string) => void;
  onCancel: () => void;
}

export function VoidInvoiceDialog({ invoiceLabel, isSaving, onConfirm, onCancel }: VoidInvoiceDialogProps) {
  const [reason, setReason] = useState('');
  const trimmedReason = reason.trim();

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="void-invoice-title">
        <DialogHeader>
          <DialogTitle id="void-invoice-title">Void this invoice?</DialogTitle>
          <DialogDescription>
            <strong className="text-foreground">{invoiceLabel}</strong> will be marked voided and excluded from active collections. This
            cannot be undone.
          </DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="void-reason">Reason for voiding</Label>
          <Input
            id="void-reason"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            placeholder="e.g. Duplicate entry, wrong patient billed"
            autoFocus
          />
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isSaving}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={() => onConfirm(trimmedReason)} disabled={isSaving || trimmedReason.length === 0}>
            {isSaving ? 'Voiding…' : 'Void Invoice'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
