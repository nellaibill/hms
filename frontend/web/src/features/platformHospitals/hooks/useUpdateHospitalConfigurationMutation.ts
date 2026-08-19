import type { UpdateTenantConfigurationRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useUpdateHospitalConfigurationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateTenantConfigurationRequest }) =>
      platformHospitalsApi.updateConfiguration(id, request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['platformHospitalConfiguration', variables.id] });
      queryClient.invalidateQueries({ queryKey: ['platformHospitals'] });
    },
  });
}
