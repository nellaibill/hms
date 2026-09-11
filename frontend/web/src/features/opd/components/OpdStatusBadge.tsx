import type { OpdConsultationStatus } from '@hms/shared';
import { Badge, type BadgeProps } from '@/components/ui/badge';
import { humanize } from '@/features/patients/humanize';

// CheckedIn doesn't have a dedicated Badge variant (the shared set stops at
// default/secondary/destructive/success/warning/outline) — teal is the OPD nav leaf's own
// icon color (config/navigation.ts), so reusing Tailwind's standard teal scale here keeps this
// one status visually distinct from Completed's green without introducing a new ad hoc hue.
const CHECKED_IN_CLASSNAME = 'border-transparent bg-teal-100 text-teal-800 dark:bg-teal-900/40 dark:text-teal-300';

const STATUS_VARIANTS: Record<OpdConsultationStatus, BadgeProps['variant']> = {
  Waiting: 'warning',
  CheckedIn: 'outline',
  InConsultation: 'default',
  Completed: 'success',
  Cancelled: 'destructive',
  NoShow: 'secondary',
};

interface OpdStatusBadgeProps {
  status: OpdConsultationStatus;
  className?: string;
}

/** One OpdConsultationStatus value in, a consistently-colored, human-readable Badge out —
 * matches LabStatusBadge's exact role for the Laboratory module. */
export function OpdStatusBadge({ status, className }: OpdStatusBadgeProps) {
  return (
    <Badge variant={STATUS_VARIANTS[status]} className={status === 'CheckedIn' ? `${CHECKED_IN_CLASSNAME} ${className ?? ''}` : className}>
      {humanize(status)}
    </Badge>
  );
}
