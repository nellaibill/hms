import { cn } from '@/lib/utils';
import { PAYMENT_STATUSES, type PaymentStatus } from '../types';

interface PaymentStatusControlProps {
  id: string;
  value: PaymentStatus | '';
  onChange: (value: PaymentStatus) => void;
  error?: string;
}

const STATUS_ACTIVE_CLASSES: Record<PaymentStatus, string> = {
  Paid: 'bg-success text-success-foreground',
  Pending: 'bg-warning text-warning-foreground',
};

/** Native radio inputs styled as a segmented control — accessible by default (a real radiogroup), no custom ARIA required. */
export function PaymentStatusControl({ id, value, onChange, error }: PaymentStatusControlProps) {
  const labelId = `${id}-label`;

  return (
    <div className="flex flex-col gap-1">
      <span id={labelId} className="text-sm font-medium leading-none text-foreground">
        Payment status
      </span>
      <div role="radiogroup" aria-labelledby={labelId} className="inline-flex w-fit rounded-md border border-input bg-background p-1">
        {PAYMENT_STATUSES.map((status) => (
          <label
            key={status}
            className={cn(
              'cursor-pointer select-none rounded-sm px-3 py-1.5 text-sm font-medium transition-colors',
              value === status ? STATUS_ACTIVE_CLASSES[status] : 'text-muted-foreground hover:text-foreground',
            )}
          >
            <input
              type="radio"
              name={id}
              value={status}
              checked={value === status}
              onChange={() => onChange(status)}
              className="sr-only"
            />
            {status}
          </label>
        ))}
      </div>
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  );
}
