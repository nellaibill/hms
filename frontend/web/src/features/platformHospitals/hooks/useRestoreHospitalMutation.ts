import { useMutation, useQueryClient } from '@tanstack/react-query';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useRestoreHospitalMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => platformHospitalsApi.restoreHospital(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['platformHospitals'] });
      queryClient.invalidateQueries({ queryKey: ['platformHospitalStats'] });
      queryClient.invalidateQueries({ queryKey: ['platformDeletedHospitals'] });
    },
  });
}
