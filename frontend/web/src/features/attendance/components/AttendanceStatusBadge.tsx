import type { AttendanceStatus } from '@hms/shared';
import { Badge, type BadgeProps } from '@/components/ui/badge';

const VARIANT_BY_STATUS: Record<AttendanceStatus, NonNullable<BadgeProps['variant']>> = {
  Present: 'success',
  Absent: 'destructive',
  Late: 'warning',
  HalfDay: 'warning',
  OnLeave: 'secondary',
};

const LABEL_BY_STATUS: Record<AttendanceStatus, string> = {
  Present: 'Present',
  Absent: 'Absent',
  Late: 'Late',
  HalfDay: 'Half Day',
  OnLeave: 'On Leave',
};

interface AttendanceStatusBadgeProps {
  status: AttendanceStatus;
}

export function AttendanceStatusBadge({ status }: AttendanceStatusBadgeProps) {
  return <Badge variant={VARIANT_BY_STATUS[status]}>{LABEL_BY_STATUS[status]}</Badge>;
}
