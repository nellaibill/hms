import { useQuery } from '@tanstack/react-query';
import { departmentsApi, eventsApi } from '@/services/apiClient';
import { mapEventToCalendarEvent } from '../eventsAdapter';
import type { CalendarEvent } from '../types';

export const calendarEventsQueryKey = ['calendar-events', 'list'] as const;

// The backend clamps PageSize to PagedRequest.MaxPageSize (100) — fine at a hospital
// calendar's current scale. Every filter (event type, department, search, date range) is
// applied client-side against this one fetched page — see utils/filterEvents.ts — so a
// future iteration adding real pagination or the /events/month endpoint would need to move
// filtering server-side too.
const EVENTS_PAGE_SIZE = 100;

/** Fetches the full event list plus the department directory needed to resolve each
 * event's DepartmentId to a display name, and combines them into the CalendarEvent shape
 * every component in this feature already renders. */
export function useCalendarEventsQuery() {
  const departmentsQuery = useQuery({
    queryKey: ['departments', 'select-list'],
    queryFn: () => departmentsApi.getDepartments({ pageSize: 100, isActive: true }),
  });

  const eventsQuery = useQuery({
    queryKey: calendarEventsQueryKey,
    queryFn: () => eventsApi.getEvents({ pageSize: EVENTS_PAGE_SIZE, sort: 'startDate' }),
  });

  const departmentNameById = new Map((departmentsQuery.data?.items ?? []).map((department) => [department.id, department.name]));

  const data: CalendarEvent[] | undefined = eventsQuery.data?.items.map((event) =>
    mapEventToCalendarEvent(event, departmentNameById),
  );

  return {
    data,
    isPending: eventsQuery.isPending || departmentsQuery.isPending,
    isFetching: eventsQuery.isFetching || departmentsQuery.isFetching,
    refetch: () => {
      void eventsQuery.refetch();
      void departmentsQuery.refetch();
    },
  };
}
