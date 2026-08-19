import { useQuery } from '@tanstack/react-query';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useHospitalConfigurationQuery(id: string | null) {
  return useQuery({
    queryKey: ['platformHospitalConfiguration', id],
    queryFn: () => platformHospitalsApi.getConfiguration(id!),
    enabled: id !== null,
  });
}
