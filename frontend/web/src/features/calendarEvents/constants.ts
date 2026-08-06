import type { EventType } from './types';

export interface EventTypeMeta {
  label: string;
  /** Badge/pill background + border + text, used in list rows, chips, and the details drawer. */
  chipClass: string;
  /** Compact month-grid event pill styling. */
  pillClass: string;
  /** Small solid dot used in filter checkboxes and the legend. */
  dotClass: string;
}

// Palette per the design brief: Holiday=soft red, Hospital Event=green, Doctor Leave=orange,
// Meeting=blue, Training=purple, Maintenance=gray, Other=cyan. Hospital Event/Meeting reuse
// the app's existing success/info semantic tokens; the rest use stock Tailwind hues since the
// design system's semantic palette only covers 5 status colors, not this module's 7 categories.
export const EVENT_TYPE_META: Record<EventType, EventTypeMeta> = {
  Holiday: {
    label: 'Holiday',
    chipClass: 'bg-rose-50 text-rose-700 border-rose-200 dark:bg-rose-500/10 dark:text-rose-300 dark:border-rose-500/30',
    pillClass: 'bg-rose-100 text-rose-800 dark:bg-rose-500/20 dark:text-rose-200',
    dotClass: 'bg-rose-500',
  },
  HospitalEvent: {
    label: 'Hospital Event',
    chipClass: 'bg-success/10 text-success border-success/25',
    pillClass: 'bg-success/15 text-success',
    dotClass: 'bg-success',
  },
  DoctorLeave: {
    label: 'Doctor Leave',
    chipClass: 'bg-orange-50 text-orange-700 border-orange-200 dark:bg-orange-500/10 dark:text-orange-300 dark:border-orange-500/30',
    pillClass: 'bg-orange-100 text-orange-800 dark:bg-orange-500/20 dark:text-orange-200',
    dotClass: 'bg-orange-500',
  },
  Meeting: {
    label: 'Meeting',
    chipClass: 'bg-info/10 text-info border-info/25',
    pillClass: 'bg-info/15 text-info',
    dotClass: 'bg-info',
  },
  Training: {
    label: 'Training',
    chipClass: 'bg-violet-50 text-violet-700 border-violet-200 dark:bg-violet-500/10 dark:text-violet-300 dark:border-violet-500/30',
    pillClass: 'bg-violet-100 text-violet-800 dark:bg-violet-500/20 dark:text-violet-200',
    dotClass: 'bg-violet-500',
  },
  Maintenance: {
    label: 'Maintenance',
    chipClass: 'bg-muted text-muted-foreground border-border',
    pillClass: 'bg-muted text-muted-foreground',
    dotClass: 'bg-muted-foreground',
  },
  Other: {
    label: 'Other',
    chipClass: 'bg-cyan-50 text-cyan-700 border-cyan-200 dark:bg-cyan-500/10 dark:text-cyan-300 dark:border-cyan-500/30',
    pillClass: 'bg-cyan-100 text-cyan-800 dark:bg-cyan-500/20 dark:text-cyan-200',
    dotClass: 'bg-cyan-500',
  },
};

/** Reserved-for-later event categories, shown disabled in the Create/Edit type list per the MVP scope note. */
export const FUTURE_EVENT_FEATURES = [
  'Birthday Reminders',
  'Anniversary Reminders',
  'Public / Private Events',
  'Recurring Events',
  'OT Blocking',
] as const;
