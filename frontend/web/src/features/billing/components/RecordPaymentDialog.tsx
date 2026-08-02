import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { formatCurrency } from '../billingCalculations';

interface RecordPaymentDialogProps {
  serviceLabel: string;
  amount: number;
  isSaving: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function RecordPaymentDialog({ serviceLabel, amount, isSaving, onConfirm, onCancel }: RecordPaymentDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="record-payment-title">
        <DialogHeader>
          <DialogTitle id="record-payment-title">Record payment?</DialogTitle>
          <DialogDescription>
            Mark <strong className="text-foreground">{serviceLabel}</strong> — {formatCurrency(amount)} — as paid.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isSaving}>
            Cancel
          </Button>
          <Button onClick={onConfirm} disabled={isSaving}>
            {isSaving ? 'Recording…' : 'Record Payment'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
