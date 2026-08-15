import type { CreateHospitalRequest } from '@hms/shared';
import { useMutation } from '@tanstack/react-query';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useCreateHospitalMutation() {
  return useMutation({
    mutationFn: (request: CreateHospitalRequest) => platformHospitalsApi.createHospital(request),
  });
}
