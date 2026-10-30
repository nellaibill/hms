import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import type { OpdVisitPaymentStatusDisplay } from '../hooks/useVisitPaymentStatusesQuery';

/** The statuses that stop "Consult" — Paid goes straight through, Loading keeps the button disabled. */
export type OpdBlockedPaymentStatus = Exclude<OpdVisitPaymentStatusDisplay, 'Paid' | 'Loading'>;

const REASONS: Record<OpdBlockedPaymentStatus, (patientName: string) => string> = {
  NotBilled: (patientName) => `${patientName}'s visit hasn't been billed yet.`,
  Pending: (patientName) => `${patientName}'s bill for this visit still has an unpaid amount.`,
  Unavailable: (patientName) => `${patientName}'s billing for this visit couldn't be loaded, so the payment can't be confirmed.`,
};

interface OpdBillingRequiredDialogProps {
  patientName: string;
  status: OpdBlockedPaymentStatus;
  /** False for a user without billing access (e.g. a consultant) — there's nowhere to send them. */
  canBill: boolean;
  onGoToBilling: () => void;
  onCancel: () => void;
}

/** Shown when "Consult" is clicked on an OPD row whose Payment Status isn't Paid. */
export function OpdBillingRequiredDialog({ patientName, status, canBill, onGoToBilling, onCancel }: OpdBillingRequiredDialogProps) {
  return (
    <Dialog open onOpenChange={(open) => !open && onCancel()}>
      <DialogContent aria-labelledby="opd-billing-required-title">
        <DialogHeader>
          <DialogTitle id="opd-billing-required-title">Payment required before consultation</DialogTitle>
          <DialogDescription>
            {REASONS[status](patientName)} Only visits with Payment Status <span className="font-medium text-foreground">Paid</span> can
            be consulted.{' '}
            {canBill
              ? 'Click OK to open billing — once the visit is paid you can continue to the consultation from there.'
              : 'Ask the billing counter to collect the payment, then try again.'}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel}>
            {canBill ? 'Cancel' : 'Close'}
          </Button>
          {canBill && <Button onClick={onGoToBilling}>OK</Button>}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
