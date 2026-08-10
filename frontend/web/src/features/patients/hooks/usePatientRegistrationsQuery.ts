import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { patientsApi } from '../../../services/apiClient';

/** Full visit history for a patient — falls back to an empty list (rather than the mock
 * store, which only ever tracks one currentRegistration) when the API is unreachable. */
export function usePatientRegistrationsQuery(id: string | undefined) {
  return useQuery({
    queryKey: ['patients', 'detail', id, 'registrations'],
    queryFn: async () => {
      try {
        return await patientsApi.getRegistrations(id as string);
      } catch (err) {
        if (err instanceof NetworkError) {
          return [];
        }
        throw err;
      }
    },
    enabled: Boolean(id),
  });
}
