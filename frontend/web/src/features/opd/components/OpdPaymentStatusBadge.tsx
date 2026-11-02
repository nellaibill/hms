import { Badge } from '@/components/ui/badge';
import type { OpdVisitPaymentStatusDisplay } from '../hooks/useVisitPaymentStatusesQuery';

const LABELS: Record<OpdVisitPaymentStatusDisplay, string> = {
  NotBilled: 'Not Billed',
  Pending: 'Pending',
  OnHold: 'On hold',
  Paid: 'Paid',
  Loading: '…',
  Unavailable: 'Unavailable',
};

const TITLES: Partial<Record<OpdVisitPaymentStatusDisplay, string>> = {
  OnHold: 'A discount on this visit’s bill is waiting for approval — payment can be collected once it’s decided',
  Unavailable: 'Could not load billing for this visit — refresh to retry',
};

const VARIANTS: Record<OpdVisitPaymentStatusDisplay, 'secondary' | 'warning' | 'success' | 'outline'> = {
  NotBilled: 'secondary',
  Pending: 'warning',
  OnHold: 'warning',
  Paid: 'success',
  Loading: 'outline',
  Unavailable: 'outline',
};

export function OpdPaymentStatusBadge({ status }: { status: OpdVisitPaymentStatusDisplay }) {
  return (
    <Badge variant={VARIANTS[status]} title={TITLES[status]}>
      {LABELS[status]}
    </Badge>
  );
}
