import { useQuery } from '@tanstack/react-query';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useHospitalFeaturesQuery(id: string | null) {
  return useQuery({
    queryKey: ['platformHospitalFeatures', id],
    queryFn: () => platformHospitalsApi.getFeatures(id!),
    enabled: id !== null,
  });
}
