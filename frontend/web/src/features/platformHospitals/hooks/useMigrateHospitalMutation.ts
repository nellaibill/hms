import { useMutation, useQueryClient } from '@tanstack/react-query';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useMigrateHospitalMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => platformHospitalsApi.migrateHospital(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['platformHospitals'] });
    },
  });
}
