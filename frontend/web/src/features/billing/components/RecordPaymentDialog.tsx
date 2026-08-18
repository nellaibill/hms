import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { formatCurrency } from '../billingCalculations';
import { PAYMENT_METHODS, PAYMENT_METHOD_LABELS, type PaymentMethod } from '../types';

interface RecordPaymentDialogProps {
  serviceLabel: string;
  amount: number;
  isSaving: boolean;
  onConfirm: (method: PaymentMethod) => void;
  onCancel: () => void;
}

export function RecordPaymentDialog({ serviceLabel, amount, isSaving, onConfirm, onCancel }: RecordPaymentDialogProps) {
  const [method, setMethod] = useState<PaymentMethod>('Cash');

  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="record-payment-title">
        <DialogHeader>
          <DialogTitle id="record-payment-title">Record payment?</DialogTitle>
          <DialogDescription>
            Mark <strong className="text-foreground">{serviceLabel}</strong> — {formatCurrency(amount)} — as paid.
          </DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="payment-method">Payment method</Label>
          <Select value={method} onValueChange={(value) => setMethod(value as PaymentMethod)}>
            <SelectTrigger id="payment-method">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {PAYMENT_METHODS.map((option) => (
                <SelectItem key={option} value={option}>
                  {PAYMENT_METHOD_LABELS[option]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isSaving}>
            Cancel
          </Button>
          <Button onClick={() => onConfirm(method)} disabled={isSaving}>
            {isSaving ? 'Recording…' : 'Record Payment'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
