import type { EmploymentStatus } from '@hms/shared';
import { Badge, type BadgeProps } from '@/components/ui/badge';

const VARIANT_BY_STATUS: Record<EmploymentStatus, NonNullable<BadgeProps['variant']>> = {
  Active: 'success',
  OnLeave: 'warning',
  Terminated: 'destructive',
  Resigned: 'secondary',
};

const LABEL_BY_STATUS: Record<EmploymentStatus, string> = {
  Active: 'Active',
  OnLeave: 'On Leave',
  Terminated: 'Terminated',
  Resigned: 'Resigned',
};

interface EmploymentStatusBadgeProps {
  status: EmploymentStatus;
}

/** EmploymentStatus is a richer HR-domain lifecycle, independent of Employee.isActive (the
 * generic Activate/Deactivate toggle) — see EmployeeResponse's own doc comment. */
export function EmploymentStatusBadge({ status }: EmploymentStatusBadgeProps) {
  return <Badge variant={VARIANT_BY_STATUS[status]}>{LABEL_BY_STATUS[status]}</Badge>;
}
