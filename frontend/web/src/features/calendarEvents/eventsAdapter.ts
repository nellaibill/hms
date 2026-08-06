import type { CreateEventRequest, Event, UpdateEventRequest } from '@hms/shared';
import type { CalendarEvent, CalendarEventFormValues } from './types';

/** Backend startDate/endDate are full ISO datetimes; the UI only ever edits a calendar
 * date (see EventFormDrawer's `type="date"` inputs), so every event is treated as running
 * midnight-to-midnight UTC — the same convention the backend's own IsAllDay-less
 * Create/UpdateEventRequest already assumes for a date-only form. */
function toDateOnly(iso: string): string {
  return iso.slice(0, 10);
}

function toApiDateTime(dateOnly: string): string {
  return `${dateOnly}T00:00:00.000Z`;
}

/** Maps a real backend Event onto the shape every calendarEvents component already renders,
 * resolving DepartmentId to a display name via the same department list DepartmentSelect/
 * DepartmentName use elsewhere in HR — see hooks/useCalendarEventsQuery.ts. */
export function mapEventToCalendarEvent(event: Event, departmentNameById: Map<string, string>): CalendarEvent {
  return {
    id: event.id,
    title: event.title,
    description: event.description ?? '',
    eventType: event.eventType,
    departmentId: event.departmentId ?? undefined,
    department: event.departmentId ? departmentNameById.get(event.departmentId) : undefined,
    startDate: toDateOnly(event.startDate),
    endDate: toDateOnly(event.endDate),
    allDay: event.isAllDay,
    createdBy: event.createdBy ?? undefined,
    createdAt: event.createdAt,
    updatedAt: event.updatedAt ?? undefined,
  };
}

export function mapFormValuesToRequest(values: CalendarEventFormValues): CreateEventRequest | UpdateEventRequest {
  if (!values.eventType) {
    throw new Error('Event type is required.');
  }
  return {
    title: values.title.trim(),
    description: values.description.trim() || null,
    eventType: values.eventType,
    startDate: toApiDateTime(values.startDate),
    endDate: toApiDateTime(values.endDate),
    isAllDay: values.allDay,
    departmentId: values.departmentId || null,
  };
}
