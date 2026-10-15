import type { AppointmentStatus } from '@hms/shared';
import { Badge, type BadgeProps } from '@/components/ui/badge';
import { appointmentStatusLabel } from '../format';

const STATUS_VARIANTS: Record<AppointmentStatus, BadgeProps['variant']> = {
  Booked: 'warning',
  Arrived: 'success',
  Cancelled: 'destructive',
  NoShow: 'secondary',
};

export function AppointmentStatusBadge({ status, className }: { status: AppointmentStatus; className?: string }) {
  return (
    <Badge variant={STATUS_VARIANTS[status]} className={className}>
      {appointmentStatusLabel(status)}
    </Badge>
  );
}
