import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { patientsApi } from '../../../services/apiClient';
import { getMockPatientById } from '../mockPatientsStore';

export function usePatientQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['patients', 'detail', id],
    queryFn: async () => {
      try {
        return await patientsApi.getPatientById(id as string);
      } catch (err) {
        if (err instanceof NetworkError) {
          const mock = getMockPatientById(id as string);
          if (mock) {
            return mock;
          }
        }
        throw err;
      }
    },
    enabled: Boolean(id),
  });
}
