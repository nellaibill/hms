import type { CreateAdmissionRequest, DischargeAdmissionRequest, TransferBedRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { admissionsApi } from '../../../../services/apiClient';

function useInvalidateAdmissions() {
  const queryClient = useQueryClient();
  return (id?: string) => {
    queryClient.invalidateQueries({ queryKey: ['ipd', 'admissions'] });
    // Bed status flips on admit/transfer/discharge, so bed lists (and the ward-grouped
    // occupancy view built from them) go stale at the same time.
    queryClient.invalidateQueries({ queryKey: ['ipd', 'beds'] });
    queryClient.invalidateQueries({ queryKey: ['ipd', 'dashboard'] });
    if (id) {
      queryClient.invalidateQueries({ queryKey: ['ipd', 'admissions', 'detail', id] });
      queryClient.invalidateQueries({ queryKey: ['ipd', 'admissions', 'transfer-history', id] });
    }
  };
}

export function useCreateAdmissionMutation() {
  const invalidate = useInvalidateAdmissions();
  return useMutation({
    mutationFn: (request: CreateAdmissionRequest) => admissionsApi.createAdmission(request),
    onSuccess: () => invalidate(),
  });
}

export function useTransferBedMutation() {
  const invalidate = useInvalidateAdmissions();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: TransferBedRequest }) => admissionsApi.transferBed(id, request),
    onSuccess: (_data, variables) => invalidate(variables.id),
  });
}

export function useDischargeMutation() {
  const invalidate = useInvalidateAdmissions();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: DischargeAdmissionRequest }) => admissionsApi.discharge(id, request),
    onSuccess: (_data, variables) => invalidate(variables.id),
  });
}
