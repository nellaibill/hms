import type { PagedPatients, PatientListQuery } from '@hms/shared';
import { NetworkError } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { patientsApi } from '../../../services/apiClient';
import { listMockPatients } from '../mockPatientsStore';

export const patientsQueryKey = (query: PatientListQuery) => ['patients', 'list', query] as const;

/** Result carries `source` so the UI can flag when it's showing offline demo data. */
export type PagedPatientsResult = PagedPatients & { source: 'live' | 'mock' };

export function usePatientsQuery(query: PatientListQuery) {
  return useQuery({
    queryKey: patientsQueryKey(query),
    queryFn: async (): Promise<PagedPatientsResult> => {
      try {
        const result = await patientsApi.getPatients(query);
        return { ...result, source: 'live' };
      } catch (err) {
        // Only fall back when the backend is genuinely unreachable — a real ApiError
        // (backend up, request rejected) should still surface normally.
        if (err instanceof NetworkError) {
          return { ...listMockPatients(query), source: 'mock' };
        }
        throw err;
      }
    },
    placeholderData: (previous) => previous,
  });
}
