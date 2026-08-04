import type { CreateWeeklyRosterRequest, UpdateWeeklyRosterRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { weeklyRostersApi } from '../../../services/apiClient';

export function useInvalidateWeeklyRosters() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['weeklyRosters'] });
}

export function useCreateWeeklyRosterMutation() {
  const invalidate = useInvalidateWeeklyRosters();
  return useMutation({
    mutationFn: (request: CreateWeeklyRosterRequest) => weeklyRostersApi.createWeeklyRoster(request),
    onSuccess: invalidate,
  });
}

export function useUpdateWeeklyRosterMutation() {
  const invalidate = useInvalidateWeeklyRosters();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateWeeklyRosterRequest }) => weeklyRostersApi.updateWeeklyRoster(id, request),
    onSuccess: invalidate,
  });
}

export function useDeleteWeeklyRosterMutation() {
  const invalidate = useInvalidateWeeklyRosters();
  return useMutation({
    mutationFn: (id: string) => weeklyRostersApi.deleteWeeklyRoster(id),
    onSuccess: invalidate,
  });
}
