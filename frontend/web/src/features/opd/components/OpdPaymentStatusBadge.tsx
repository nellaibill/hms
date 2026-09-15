import { Badge } from '@/components/ui/badge';
import type { OpdVisitPaymentStatus } from '../hooks/useVisitPaymentStatusesQuery';

const LABELS: Record<OpdVisitPaymentStatus, string> = {
  NotBilled: 'Not Billed',
  Pending: 'Pending',
  Paid: 'Paid',
};

const VARIANTS: Record<OpdVisitPaymentStatus, 'secondary' | 'warning' | 'success'> = {
  NotBilled: 'secondary',
  Pending: 'warning',
  Paid: 'success',
};

export function OpdPaymentStatusBadge({ status }: { status: OpdVisitPaymentStatus }) {
  return <Badge variant={VARIANTS[status]}>{LABELS[status]}</Badge>;
}
