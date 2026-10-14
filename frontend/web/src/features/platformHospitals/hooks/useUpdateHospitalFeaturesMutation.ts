import type { UpdateTenantFeaturesRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useUpdateHospitalFeaturesMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateTenantFeaturesRequest }) =>
      platformHospitalsApi.updateFeatures(id, request),
    onSuccess: (data, variables) => {
      // The Features page stays open after a save, so show the saved set straight away rather
      // than reading "unsaved changes" until the refetch lands.
      queryClient.setQueryData(['platformHospitalFeatures', variables.id], data);
      queryClient.invalidateQueries({ queryKey: ['platformHospitalFeatures', variables.id] });
      queryClient.invalidateQueries({ queryKey: ['platformHospitals'] });
    },
  });
}
