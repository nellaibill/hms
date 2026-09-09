import type { CreateDoctorOrderRequest } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

function doctorOrdersKey(admissionId: string | undefined) {
  return ['ipd', 'admissions', 'doctor-orders', admissionId];
}

export function useDoctorOrdersQuery(admissionId: string | undefined) {
  return useQuery({
    queryKey: doctorOrdersKey(admissionId),
    queryFn: () => admissionsApi.getDoctorOrders(admissionId as string),
    enabled: Boolean(admissionId),
  });
}

export function usePostDoctorOrderMutation(admissionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateDoctorOrderRequest) => admissionsApi.postDoctorOrder(admissionId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: doctorOrdersKey(admissionId) }),
  });
}

export function useAdvanceDoctorOrderMutation(admissionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (orderId: string) => admissionsApi.advanceDoctorOrder(admissionId, orderId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: doctorOrdersKey(admissionId) }),
  });
}

export function useCancelDoctorOrderMutation(admissionId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (orderId: string) => admissionsApi.cancelDoctorOrder(admissionId, orderId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: doctorOrdersKey(admissionId) }),
  });
}
