import type {
  AppointmentListQuery,
  BookAppointmentRequest,
  CancelAppointmentRequest,
  ConsultantBlockListQuery,
  CreateConsultantBlockRequest,
  MarkAppointmentArrivedRequest,
  RescheduleAppointmentRequest,
} from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { appointmentsApi } from '@/services/apiClient';

/** Every appointments query lives under this key, so one invalidation refreshes the day list,
 * the slot grid and the counts together after any change. */
export const APPOINTMENTS_KEY = ['appointments'] as const;

export function useAppointmentsQuery(query: AppointmentListQuery, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: [...APPOINTMENTS_KEY, 'list', query],
    queryFn: () => appointmentsApi.getAppointments(query),
    enabled: options?.enabled ?? true,
    placeholderData: (previous) => previous,
  });
}

export function useAppointmentQuery(id: string | null | undefined) {
  return useQuery({
    queryKey: [...APPOINTMENTS_KEY, 'detail', id],
    queryFn: () => appointmentsApi.getAppointmentById(id as string),
    enabled: Boolean(id),
  });
}

export function useAppointmentSlotsQuery(consultantId: string | undefined, date: string | undefined) {
  return useQuery({
    queryKey: [...APPOINTMENTS_KEY, 'slots', consultantId, date],
    queryFn: () => appointmentsApi.getSlots(consultantId as string, date as string),
    enabled: Boolean(consultantId && date),
  });
}

export function useAppointmentSummaryQuery(date: string, filters: { consultantId?: string; departmentId?: string } = {}) {
  return useQuery({
    queryKey: [...APPOINTMENTS_KEY, 'summary', date, filters],
    queryFn: () => appointmentsApi.getSummary(date, filters),
    enabled: Boolean(date),
  });
}

export function useConsultantBlocksQuery(query: ConsultantBlockListQuery, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: [...APPOINTMENTS_KEY, 'blocks', query],
    queryFn: () => appointmentsApi.getBlocks(query),
    enabled: options?.enabled ?? true,
  });
}

function useAppointmentsMutation<TVariables, TResult>(mutationFn: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: APPOINTMENTS_KEY }),
  });
}

export function useBookAppointmentMutation() {
  return useAppointmentsMutation((request: BookAppointmentRequest) => appointmentsApi.book(request));
}

export function useRescheduleAppointmentMutation() {
  return useAppointmentsMutation(({ id, request }: { id: string; request: RescheduleAppointmentRequest }) => appointmentsApi.reschedule(id, request));
}

export function useCancelAppointmentMutation() {
  return useAppointmentsMutation(({ id, request }: { id: string; request: CancelAppointmentRequest }) => appointmentsApi.cancel(id, request));
}

export function useMarkNoShowMutation() {
  return useAppointmentsMutation((id: string) => appointmentsApi.markNoShow(id));
}

export function useMarkArrivedMutation() {
  return useAppointmentsMutation(({ id, request }: { id: string; request: MarkAppointmentArrivedRequest }) => appointmentsApi.markArrived(id, request));
}

export function useCreateConsultantBlockMutation() {
  return useAppointmentsMutation((request: CreateConsultantBlockRequest) => appointmentsApi.createBlock(request));
}

export function useRemoveConsultantBlockMutation() {
  return useAppointmentsMutation((id: string) => appointmentsApi.removeBlock(id));
}
