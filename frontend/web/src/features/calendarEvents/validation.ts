import { isoDateRangesOverlap } from './utils/date';
import type { CalendarEvent, CalendarEventFormValues } from './types';

export interface EventFormErrors {
  title?: string;
  eventType?: string;
  startDate?: string;
  endDate?: string;
  business?: string;
}

interface ValidateOptions {
  /** Existing events to check the Doctor Leave / Holiday business rules against. */
  existingEvents: CalendarEvent[];
  /** The event being edited, excluded from its own overlap/uniqueness checks. */
  excludeId?: string;
}

export function validateEventForm(values: CalendarEventFormValues, { existingEvents, excludeId }: ValidateOptions): EventFormErrors {
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

  // Business rules only apply once the basics are valid — no point flagging an overlap
  // against a date range that isn't even well-formed yet.
  if (!errors.startDate && !errors.endDate && values.eventType) {
    const others = existingEvents.filter((event) => event.id !== excludeId);

    if (values.eventType === 'DoctorLeave') {
      // No dedicated "doctor" field on the event yet — title is the doctor identifier
      // (e.g. "Dr. Priya — Leave"), so overlap is scoped to same title + overlapping dates.
      const conflict = others.find(
        (event) =>
          event.eventType === 'DoctorLeave' &&
          event.title.trim().toLowerCase() === values.title.trim().toLowerCase() &&
          isoDateRangesOverlap(values.startDate, values.endDate, event.startDate, event.endDate),
      );
      if (conflict) {
        errors.business = `This overlaps an existing approved leave for "${conflict.title}" (${conflict.startDate} to ${conflict.endDate}).`;
      }
    }

    if (values.eventType === 'Holiday' && !errors.business) {
      const conflict = others.find(
        (event) =>
          event.eventType === 'Holiday' &&
          isoDateRangesOverlap(values.startDate, values.endDate, event.startDate, event.endDate),
      );
      if (conflict) {
        errors.business = `"${conflict.title}" is already a holiday on an overlapping date (${conflict.startDate} to ${conflict.endDate}). Holiday dates must be unique.`;
      }
    }
  }

  return errors;
}

export function isEventFormValid(errors: EventFormErrors): boolean {
  return Object.keys(errors).length === 0;
}
