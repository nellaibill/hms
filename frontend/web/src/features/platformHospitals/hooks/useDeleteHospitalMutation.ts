import { useMutation, useQueryClient } from '@tanstack/react-query';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useDeleteHospitalMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, confirmHospitalCode }: { id: string; confirmHospitalCode: string }) =>
      platformHospitalsApi.deleteHospital(id, confirmHospitalCode),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['platformHospitals'] });
      queryClient.invalidateQueries({ queryKey: ['platformHospitalStats'] });
      queryClient.invalidateQueries({ queryKey: ['platformDeletedHospitals'] });
    },
  });
}
