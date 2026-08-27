import type { LeaveRequestStatus } from '@hms/shared';
import { Badge, type BadgeProps } from '@/components/ui/badge';

const VARIANT_BY_STATUS: Record<LeaveRequestStatus, NonNullable<BadgeProps['variant']>> = {
  Pending: 'warning',
  Approved: 'success',
  Rejected: 'destructive',
  Cancelled: 'secondary',
};

interface LeaveRequestStatusBadgeProps {
  status: LeaveRequestStatus;
}

export function LeaveRequestStatusBadge({ status }: LeaveRequestStatusBadgeProps) {
  return <Badge variant={VARIANT_BY_STATUS[status]}>{status}</Badge>;
}
