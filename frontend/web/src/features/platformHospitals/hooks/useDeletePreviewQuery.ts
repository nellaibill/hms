import { useQuery } from '@tanstack/react-query';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useDeletePreviewQuery(id: string | null) {
  return useQuery({
    queryKey: ['platformHospitalDeletePreview', id],
    queryFn: () => platformHospitalsApi.getDeletePreview(id!),
    enabled: id !== null,
  });
}
