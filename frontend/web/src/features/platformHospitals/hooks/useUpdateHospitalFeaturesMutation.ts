import type { UpdateTenantFeaturesRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useUpdateHospitalFeaturesMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateTenantFeaturesRequest }) =>
      platformHospitalsApi.updateFeatures(id, request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['platformHospitalFeatures', variables.id] });
      queryClient.invalidateQueries({ queryKey: ['platformHospitals'] });
    },
  });
}
