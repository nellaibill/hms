import { Lock } from 'lucide-react';
import { formatCurrency } from '../billingCalculations';

interface ChargeDisplayProps {
  id: string;
  amount: number;
  label?: string;
}

/** Read-only stand-in for an input — dashed border + muted fill + lock icon make "you can't type here" obvious at a glance, per the receptionist never manually entering charges. */
export function ChargeDisplay({ id, amount, label = 'Charge' }: ChargeDisplayProps) {
  return (
    <div className="flex w-full flex-col gap-1 sm:w-40">
      <label htmlFor={id} className="text-sm font-medium leading-none text-foreground">
        {label}
      </label>
      <div
        id={id}
        role="textbox"
        aria-readonly="true"
        className="flex h-10 items-center gap-1.5 rounded-md border border-dashed border-input bg-muted px-3 text-sm font-semibold text-foreground"
      >
        <Lock className="h-3.5 w-3.5 shrink-0 text-muted-foreground" aria-hidden="true" />
        {formatCurrency(amount)}
      </div>
    </div>
  );
}
