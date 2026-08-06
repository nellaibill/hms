import { useQuery } from '@tanstack/react-query';
import { listMockEvents, type CalendarEventListQuery } from '../mockEventsStore';

export const calendarEventsQueryKey = (query: CalendarEventListQuery) => ['calendar-events', 'list', query] as const;

/** No real network here (UI-only mock store), but a query wrapper keeps the loading/empty-state
 * plumbing identical to every other module's list page (RolesListPage, InvoiceLedgerPage, …). */
export function useCalendarEventsQuery(query: CalendarEventListQuery) {
  return useQuery({
    queryKey: calendarEventsQueryKey(query),
    queryFn: () => listMockEvents(query),
    placeholderData: (previous) => previous,
  });
}
