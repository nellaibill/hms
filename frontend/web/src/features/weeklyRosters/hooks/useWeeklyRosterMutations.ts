import type { CopyWeeklyRosterRequest, CreateWeeklyRosterRequest, UpdateWeeklyRosterRequest } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { weeklyRostersApi } from '../../../services/apiClient';
import {
  copyMockWeeklyRoster,
  createMockWeeklyRoster,
  deleteMockWeeklyRoster,
  publishMockWeeklyRoster,
  updateMockWeeklyRoster,
} from '../mockWeeklyRostersStore';

export function useInvalidateWeeklyRosters() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['weeklyRosters'] });
}

export function useCreateWeeklyRosterMutation() {
  const invalidate = useInvalidateWeeklyRosters();
  return useMutation({
    mutationFn: async (request: CreateWeeklyRosterRequest) => {
      try {
        return await weeklyRostersApi.createWeeklyRoster(request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return createMockWeeklyRoster(request);
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}

export function useUpdateWeeklyRosterMutation() {
  const invalidate = useInvalidateWeeklyRosters();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateWeeklyRosterRequest }) => {
      try {
        return await weeklyRostersApi.updateWeeklyRoster(id, request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return updateMockWeeklyRoster(id, request);
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}

export function useDeleteWeeklyRosterMutation() {
  const invalidate = useInvalidateWeeklyRosters();
  return useMutation({
    mutationFn: async (id: string) => {
      try {
        await weeklyRostersApi.deleteWeeklyRoster(id);
      } catch (err) {
        if (err instanceof NetworkError) {
          deleteMockWeeklyRoster(id);
          return;
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}

export function usePublishWeeklyRosterMutation() {
  const invalidate = useInvalidateWeeklyRosters();
  return useMutation({
    mutationFn: async (id: string) => {
      try {
        return await weeklyRostersApi.publishWeeklyRoster(id);
      } catch (err) {
        if (err instanceof NetworkError) {
          return publishMockWeeklyRoster(id);
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}

export function useCopyWeeklyRosterMutation() {
  const invalidate = useInvalidateWeeklyRosters();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: CopyWeeklyRosterRequest }) => {
      try {
        return await weeklyRostersApi.copyWeeklyRoster(id, request);
      } catch (err) {
        if (err instanceof NetworkError) {
          return copyMockWeeklyRoster(id, request);
        }
        throw err;
      }
    },
    onSuccess: invalidate,
  });
}
