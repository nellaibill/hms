import type { CalendarEventFormValues } from './types';

export interface EventFormErrors {
  title?: string;
  eventType?: string;
  startDate?: string;
  endDate?: string;
}

// Business rules that need to see every event (Holiday-date uniqueness, Doctor Leave
// overlap) are no longer checked here — the mock store's in-memory array could see every
// event because it *was* every event; the real backend paginates, so a client-side check
// against only the current page would be incomplete and misleading. The backend enforces
// Holiday-date uniqueness itself (CALENDAR.DUPLICATE_HOLIDAY) and its own doc comments
// explain why Doctor Leave overlap isn't enforced anywhere yet — both surface as a normal
// submitError from the mutation instead of a pre-submit warning here.
export function validateEventForm(values: CalendarEventFormValues): EventFormErrors {
  const errors: EventFormErrors = {};

  if (!values.title.trim()) {
    errors.title = 'Title is required.';
  }
  if (!values.eventType) {
    errors.eventType = 'Event type is required.';
  }
  if (!values.startDate) {
    errors.startDate = 'Start date is required.';
  }
  if (!values.endDate) {
    errors.endDate = 'End date is required.';
  }
  if (values.startDate && values.endDate && values.startDate > values.endDate) {
    errors.endDate = 'End date cannot be before start date.';
  }

  return errors;
}

export function isEventFormValid(errors: EventFormErrors): boolean {
  return Object.keys(errors).length === 0;
}
