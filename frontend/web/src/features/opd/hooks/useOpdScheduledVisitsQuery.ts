import type { ScheduledVisitQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { opdConsultationApi } from '@/services/apiClient';

export const opdScheduledVisitsQueryKey = (query: ScheduledVisitQuery) => ['opd', 'scheduled-visits', query] as const;

/** OPD Scheduled Visits tab — every patient's upcoming review dates from `query.from` on,
 * filterable by department/consultant/search. Under the ['opd'] prefix, so the page's Refresh
 * button reloads it like every other OPD tab. */
export function useOpdScheduledVisitsQuery(query: ScheduledVisitQuery) {
  return useQuery({
    queryKey: opdScheduledVisitsQueryKey(query),
    queryFn: () => opdConsultationApi.listScheduledVisits(query),
    placeholderData: (previous) => previous,
  });
}
