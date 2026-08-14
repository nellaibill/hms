import { useMutation, useQueryClient } from '@tanstack/react-query';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useUpdateHospitalStatusMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) => platformHospitalsApi.updateStatus(id, { status }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['platformHospitals'] });
      queryClient.invalidateQueries({ queryKey: ['platformHospitalStats'] });
    },
  });
}
