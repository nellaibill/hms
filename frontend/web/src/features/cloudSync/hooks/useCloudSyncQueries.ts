import type { SyncEventListQuery, SyncResolveRequest, SyncStreamListQuery } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { syncMonitorApi } from '../../../services/apiClient';
import { CLOUD_SYNC_REFRESH_MS } from '../cloudSyncUtils';

const ROOT = ['cloud-sync'] as const;

export function useSyncSummaryQuery() {
  return useQuery({
    queryKey: [...ROOT, 'summary'] as const,
    queryFn: () => syncMonitorApi.getSummary(),
    refetchInterval: CLOUD_SYNC_REFRESH_MS,
  });
}

export function useSyncStreamsQuery(query: SyncStreamListQuery, enabled: boolean) {
  return useQuery({
    queryKey: [...ROOT, 'streams', query] as const,
    queryFn: () => syncMonitorApi.getStreams(query),
    placeholderData: (previous) => previous,
    refetchInterval: CLOUD_SYNC_REFRESH_MS,
    enabled,
  });
}

export function useSyncFailedEventsQuery(query: SyncEventListQuery, enabled: boolean) {
  return useQuery({
    queryKey: [...ROOT, 'events', query] as const,
    queryFn: () => syncMonitorApi.getEvents(query),
    placeholderData: (previous) => previous,
    refetchInterval: CLOUD_SYNC_REFRESH_MS,
    enabled,
  });
}

export function useSyncPatientHistoryQuery(patientId: string | null) {
  return useQuery({
    queryKey: [...ROOT, 'history', patientId] as const,
    queryFn: () => syncMonitorApi.getPatientHistory(patientId as string),
    enabled: patientId !== null,
    refetchInterval: CLOUD_SYNC_REFRESH_MS,
  });
}

/** Every action refreshes the whole page's data when it finishes. */
function useSyncAction<TArgs>(action: (args: TArgs) => Promise<{ message: string }>) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: action,
    onSettled: () => queryClient.invalidateQueries({ queryKey: ROOT }),
  });
}

export const useRetryEventMutation = () => useSyncAction((eventId: string) => syncMonitorApi.retryEvent(eventId));
export const useRetryAllMutation = () => useSyncAction(() => syncMonitorApi.retryAll());
export const useResolveEventMutation = () =>
  useSyncAction(({ eventId, request }: { eventId: string; request: SyncResolveRequest }) => syncMonitorApi.resolveEvent(eventId, request));
export const usePauseMutation = () => useSyncAction(() => syncMonitorApi.pause());
export const useResumeMutation = () => useSyncAction(() => syncMonitorApi.resume());
export const useTriggerMutation = () => useSyncAction(() => syncMonitorApi.trigger());
