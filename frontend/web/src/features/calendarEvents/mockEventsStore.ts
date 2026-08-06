import { buildMockEvents } from './mockEvents';
import type { CalendarEvent, CalendarEventFilters, CalendarEventFormValues } from './types';
import { isoDateRangesOverlap } from './utils/date';
import { isEventFormValid, validateEventForm } from './validation';

/**
 * UI-only mock data layer — Calendar has no backend module yet (mirrors
 * features/roles/mockRolesStore.ts and features/billing/mockBillingStore.ts). Persisted to
 * localStorage so the demo survives page refreshes.
 */
const STORAGE_KEY = 'hms-mock-calendar-events';

function loadEvents(): CalendarEvent[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as CalendarEvent[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed;
      }
    }
  } catch {
    // Corrupt/unavailable storage — fall through to seed data.
  }
  return buildMockEvents();
}

function persist() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(events));
  } catch {
    // Storage unavailable (e.g. private browsing quota) — demo still works for this tab session.
  }
}

let events: CalendarEvent[] = loadEvents();
let nextSeq = events.reduce((max, e) => Math.max(max, Number(e.id.replace('evt-', '')) || 0), 0) + 1;

export interface CalendarEventListQuery extends CalendarEventFilters {
  search?: string;
}

export function listMockEvents(query: CalendarEventListQuery = { types: [] }): CalendarEvent[] {
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

export function getMockEventById(id: string): CalendarEvent | undefined {
  return events.find((e) => e.id === id);
}

export function getUpcomingMockEvents(fromIso: string, limit: number): CalendarEvent[] {
  return events
    .filter((event) => event.endDate >= fromIso)
    .sort((a, b) => a.startDate.localeCompare(b.startDate))
    .slice(0, limit);
}

export class EventValidationError extends Error {}

export function createMockEvent(values: CalendarEventFormValues): CalendarEvent {
  const errors = validateEventForm(values, { existingEvents: events });
  if (!isEventFormValid(errors)) {
    throw new EventValidationError(errors.business ?? Object.values(errors)[0] ?? 'Invalid event.');
  }

  const seq = nextSeq++;
  const now = new Date().toISOString();
  const event: CalendarEvent = {
    id: `evt-${String(seq).padStart(3, '0')}`,
    title: values.title.trim(),
    description: values.description.trim(),
    eventType: values.eventType || 'Other',
    department: values.department || undefined,
    startDate: values.startDate,
    endDate: values.endDate,
    allDay: values.allDay,
    reminderEnabled: values.reminderEnabled,
    reminderType: values.reminderEnabled ? values.reminderType : undefined,
    reminderAt: values.reminderEnabled ? values.reminderAt : undefined,
    createdBy: 'Admin User',
    createdAt: now,
    updatedAt: now,
  };
  events = [event, ...events];
  persist();
  return event;
}

export function updateMockEvent(id: string, values: CalendarEventFormValues): CalendarEvent {
  const existing = getMockEventById(id);
  if (!existing) {
    throw new Error(`Mock event ${id} not found.`);
  }

  const errors = validateEventForm(values, { existingEvents: events, excludeId: id });
  if (!isEventFormValid(errors)) {
    throw new EventValidationError(errors.business ?? Object.values(errors)[0] ?? 'Invalid event.');
  }

  const updated: CalendarEvent = {
    ...existing,
    title: values.title.trim(),
    description: values.description.trim(),
    eventType: values.eventType || existing.eventType,
    department: values.department || undefined,
    startDate: values.startDate,
    endDate: values.endDate,
    allDay: values.allDay,
    reminderEnabled: values.reminderEnabled,
    reminderType: values.reminderEnabled ? values.reminderType : undefined,
    reminderAt: values.reminderEnabled ? values.reminderAt : undefined,
    updatedAt: new Date().toISOString(),
  };
  events = events.map((e) => (e.id === id ? updated : e));
  persist();
  return updated;
}

export function deleteMockEvent(id: string): void {
  events = events.filter((e) => e.id !== id);
  persist();
}
