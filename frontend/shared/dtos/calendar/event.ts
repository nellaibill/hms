import type { EventType } from '../../enums';

/** Mirrors HMS.Modules.Calendar.Contracts.EventResponse. StartDate/EndDate are full ISO
 * datetimes on the wire even though the UI only edits a calendar date — see
 * frontend/web/src/features/calendarEvents/eventsAdapter.ts for the date-only <-> datetime
 * conversion at the form boundary. */
export interface Event {
  id: string;
  title: string;
  description?: string | null;
  eventType: EventType;
  startDate: string;
  endDate: string;
  isAllDay: boolean;
  departmentId?: string | null;
  createdBy?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Calendar.Contracts.CreateEventRequest. */
export interface CreateEventRequest {
  title: string;
  description?: string | null;
  eventType: EventType;
  startDate: string;
  endDate: string;
  isAllDay: boolean;
  departmentId?: string | null;
}

/** Mirrors HMS.Modules.Calendar.Contracts.UpdateEventRequest — same shape as create; Event
 * has no natural-key field to protect. */
export type UpdateEventRequest = CreateEventRequest;

/** Mirrors HMS.Modules.Calendar.Contracts.EventListQuery. */
export interface EventListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  eventType?: EventType;
  departmentId?: string;
}

/** Mirrors HMS.Modules.Calendar.Contracts.MonthlyEventQuery. */
export interface MonthlyEventQuery {
  year: number;
  month: number;
  page?: number;
  pageSize?: number;
  eventType?: EventType;
  departmentId?: string;
}
