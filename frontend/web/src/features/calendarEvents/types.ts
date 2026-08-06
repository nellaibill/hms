export const EVENT_TYPES = [
  'Holiday',
  'HospitalEvent',
  'DoctorLeave',
  'Meeting',
  'Training',
  'Maintenance',
  'Other',
] as const;

export type EventType = (typeof EVENT_TYPES)[number];

export const REMINDER_TYPES = ['Notification', 'Email', 'SMS'] as const;
export type ReminderType = (typeof REMINDER_TYPES)[number];

export interface CalendarEvent {
  id: string;
  title: string;
  description: string;
  eventType: EventType;
  /** Master department name, or undefined for hospital-wide events. */
  department?: string;
  /** ISO calendar date (YYYY-MM-DD), inclusive. */
  startDate: string;
  /** ISO calendar date (YYYY-MM-DD), inclusive. */
  endDate: string;
  allDay: boolean;
  reminderEnabled: boolean;
  reminderType?: ReminderType;
  /** ISO datetime — only meaningful when reminderEnabled is true. */
  reminderAt?: string;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface CalendarEventFormValues {
  title: string;
  description: string;
  eventType: EventType | '';
  department?: string;
  startDate: string;
  endDate: string;
  allDay: boolean;
  reminderEnabled: boolean;
  reminderType?: ReminderType;
  reminderAt?: string;
}

export interface CalendarEventFilters {
  types: EventType[];
  department?: string;
  /** Inclusive ISO date range for the Filter Panel's explicit date-range filter. */
  dateFrom?: string;
  dateTo?: string;
}

export function createEmptyFilters(): CalendarEventFilters {
  return { types: [] };
}

export function isFiltersEmpty(filters: CalendarEventFilters): boolean {
  return filters.types.length === 0 && !filters.department && !filters.dateFrom && !filters.dateTo;
}
