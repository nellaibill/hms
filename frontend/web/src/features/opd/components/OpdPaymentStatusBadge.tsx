import { Badge } from '@/components/ui/badge';
import type { OpdVisitPaymentStatusDisplay } from '../hooks/useVisitPaymentStatusesQuery';

const LABELS: Record<OpdVisitPaymentStatusDisplay, string> = {
  NotBilled: 'Not Billed',
  Pending: 'Pending',
  Paid: 'Paid',
  Loading: '…',
  Unavailable: 'Unavailable',
};

const VARIANTS: Record<OpdVisitPaymentStatusDisplay, 'secondary' | 'warning' | 'success' | 'outline'> = {
  NotBilled: 'secondary',
  Pending: 'warning',
  Paid: 'success',
  Loading: 'outline',
  Unavailable: 'outline',
};

export function OpdPaymentStatusBadge({ status }: { status: OpdVisitPaymentStatusDisplay }) {
  return (
    <Badge variant={VARIANTS[status]} title={status === 'Unavailable' ? 'Could not load billing for this visit — refresh to retry' : undefined}>
      {LABELS[status]}
    </Badge>
  );
}
