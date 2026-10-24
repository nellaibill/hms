import type { MarkedPatientQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { opdConsultationApi } from '@/services/apiClient';

export const opdMarkedPatientsQueryKey = (query: MarkedPatientQuery) => ['opd', 'marked-patients', query] as const;

/** OPD Marked Patients tab — every consultation whose patient a doctor marked, newest mark first,
 * filterable by department/consultant/search. Under the ['opd'] prefix, so the page's Refresh
 * button reloads it like every other OPD tab. */
export function useOpdMarkedPatientsQuery(query: MarkedPatientQuery) {
  return useQuery({
    queryKey: opdMarkedPatientsQueryKey(query),
    queryFn: () => opdConsultationApi.listMarkedPatients(query),
    placeholderData: (previous) => previous,
  });
}
