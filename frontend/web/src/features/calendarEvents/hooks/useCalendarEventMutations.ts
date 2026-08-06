import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createMockEvent, deleteMockEvent, updateMockEvent } from '../mockEventsStore';
import type { CalendarEventFormValues } from '../types';

export function useCreateCalendarEventMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (values: CalendarEventFormValues) => Promise.resolve(createMockEvent(values)),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['calendar-events'] }),
  });
}

export function useUpdateCalendarEventMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, values }: { id: string; values: CalendarEventFormValues }) =>
      Promise.resolve(updateMockEvent(id, values)),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['calendar-events'] }),
  });
}

export function useDeleteCalendarEventMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => Promise.resolve(deleteMockEvent(id)),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['calendar-events'] }),
  });
}
