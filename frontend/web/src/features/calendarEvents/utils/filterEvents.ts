import { isoDateRangesOverlap } from './date';
import type { CalendarEvent, CalendarEventFilters } from '../types';

export interface EventSearchQuery extends CalendarEventFilters {
  search?: string;
}

/** Client-side filtering over the full event list fetched from the real backend. The
 * backend's EventListQuery only supports a single EventType/DepartmentId, but this sidebar
 * lets a user select several event types at once and filters by department display name —
 * neither maps cleanly onto one server-side query param, so filtering (like the mock store
 * it replaces) stays client-side against the already-fetched page. */
export function filterEvents(events: CalendarEvent[], query: EventSearchQuery = { types: [] }): CalendarEvent[] {
  let items = events;

  if (query.types.length > 0) {
    items = items.filter((event) => query.types.includes(event.eventType));
  }
  if (query.department) {
    items = items.filter((event) => event.department === query.department);
  }
  if (query.dateFrom || query.dateTo) {
    const from = query.dateFrom ?? '0000-01-01';
    const to = query.dateTo ?? '9999-12-31';
    items = items.filter((event) => isoDateRangesOverlap(event.startDate, event.endDate, from, to));
  }
  const search = query.search?.trim().toLowerCase();
  if (search) {
    items = items.filter((event) =>
      [event.title, event.description, event.eventType, event.department ?? ''].some((field) =>
        field.toLowerCase().includes(search),
      ),
    );
  }

  return [...items].sort((a, b) => a.startDate.localeCompare(b.startDate));
}

export function getUpcomingEvents(events: CalendarEvent[], fromIso: string, limit: number): CalendarEvent[] {
  return events
    .filter((event) => event.endDate >= fromIso)
    .sort((a, b) => a.startDate.localeCompare(b.startDate))
    .slice(0, limit);
}
