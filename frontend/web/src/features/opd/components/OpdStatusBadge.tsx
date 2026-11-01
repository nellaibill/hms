import type { OpdConsultationStatus } from '@hms/shared';
import { Badge, type BadgeProps } from '@/components/ui/badge';
import { opdStatusLabel } from '../opdStatusLabel';

// CheckedIn doesn't have a dedicated Badge variant (the shared set stops at
// default/secondary/destructive/success/warning/outline) — teal is the OPD nav leaf's own
// icon color (config/navigation.ts), so reusing Tailwind's standard teal scale here keeps this
// one status visually distinct from Completed's green without introducing a new ad hoc hue.
const CHECKED_IN_CLASSNAME = 'border-transparent bg-teal-100 text-teal-800 dark:bg-teal-900/40 dark:text-teal-300';

// Same reasoning for AutoCompleted: a neutral slate keeps it apart from NoShow's secondary grey
// and from Completed's green — the system closed it, nobody finished it.
const AUTO_COMPLETED_CLASSNAME = 'border-transparent bg-slate-200 text-slate-700 dark:bg-slate-700/60 dark:text-slate-200';

const STATUS_VARIANTS: Record<OpdConsultationStatus, BadgeProps['variant']> = {
  Waiting: 'warning',
  CheckedIn: 'outline',
  InConsultation: 'default',
  Completed: 'success',
  Cancelled: 'destructive',
  NoShow: 'secondary',
  AutoCompleted: 'outline',
};

const STATUS_CLASSNAMES: Partial<Record<OpdConsultationStatus, string>> = {
  CheckedIn: CHECKED_IN_CLASSNAME,
  AutoCompleted: AUTO_COMPLETED_CLASSNAME,
};

interface OpdStatusBadgeProps {
  status: OpdConsultationStatus;
  className?: string;
}

/** One OpdConsultationStatus value in, a consistently-colored, human-readable Badge out —
 * matches LabStatusBadge's exact role for the Laboratory module. */
export function OpdStatusBadge({ status, className }: OpdStatusBadgeProps) {
  const statusClassName = STATUS_CLASSNAMES[status];
  return (
    <Badge variant={STATUS_VARIANTS[status]} className={statusClassName ? `${statusClassName} ${className ?? ''}` : className}>
      {opdStatusLabel(status)}
    </Badge>
  );
}
