import { EVENT_TYPES, type EventType } from '@hms/shared';

export { EVENT_TYPES };
export type { EventType };

export interface CalendarEvent {
  id: string;
  title: string;
  description: string;
  eventType: EventType;
  /** Real department GUID (HMS.Modules.HR's Department), or undefined for hospital-wide events. */
  departmentId?: string;
  /** Resolved department display name for departmentId, joined client-side against the
   * real department directory — undefined while that directory is still loading or for
   * hospital-wide events. */
  department?: string;
  /** ISO calendar date (YYYY-MM-DD), inclusive. */
  startDate: string;
  /** ISO calendar date (YYYY-MM-DD), inclusive. */
  endDate: string;
  allDay: boolean;
  /** Backend user id, or undefined — this module has no authentication wired up yet, so
   * every event's CreatedBy is currently null (see HMS.Modules.Calendar.Endpoints.EventsController). */
  createdBy?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CalendarEventFormValues {
  title: string;
  description: string;
  eventType: EventType | '';
  /** Real department GUID, or undefined for a hospital-wide event. */
  departmentId?: string;
  startDate: string;
  endDate: string;
  allDay: boolean;
}

export interface CalendarEventFilters {
  types: EventType[];
  /** Department display name — filtering happens client-side against the resolved
   * CalendarEvent.department, so this stays a name, not an id. */
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
