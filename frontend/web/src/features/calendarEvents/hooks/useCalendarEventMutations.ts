import { useMutation, useQueryClient } from '@tanstack/react-query';
import { eventsApi } from '@/services/apiClient';
import { mapFormValuesToRequest } from '../eventsAdapter';
import { calendarEventsQueryKey } from './useCalendarEventsQuery';
import type { CalendarEventFormValues } from '../types';

export function useCreateCalendarEventMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (values: CalendarEventFormValues) => eventsApi.createEvent(mapFormValuesToRequest(values)),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: calendarEventsQueryKey }),
  });
}

export function useUpdateCalendarEventMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, values }: { id: string; values: CalendarEventFormValues }) =>
      eventsApi.updateEvent(id, mapFormValuesToRequest(values)),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: calendarEventsQueryKey }),
  });
}

export function useDeleteCalendarEventMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => eventsApi.deleteEvent(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: calendarEventsQueryKey }),
  });
}
