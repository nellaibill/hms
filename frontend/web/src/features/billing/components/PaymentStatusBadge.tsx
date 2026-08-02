import { Badge } from '@/components/ui/badge';
import type { PaymentStatus } from '../types';

interface PaymentStatusBadgeProps {
  status: PaymentStatus;
}

export function PaymentStatusBadge({ status }: PaymentStatusBadgeProps) {
  return <Badge variant={status === 'Paid' ? 'success' : 'warning'}>{status}</Badge>;
}
