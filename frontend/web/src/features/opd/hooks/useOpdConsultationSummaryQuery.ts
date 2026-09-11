import type { OpdConsultationSummaryQuery } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { opdApi } from '@/services/apiClient';

export const opdConsultationSummaryQueryKey = (query: OpdConsultationSummaryQuery) => ['opd', 'consultations', 'summary', query] as const;

/** OPD Consultation List tab — one row per consultant/department with any activity in the
 * given date range. Not paginated (mirrors the backend's own plain-list response). */
export function useOpdConsultationSummaryQuery(query: OpdConsultationSummaryQuery) {
  return useQuery({
    queryKey: opdConsultationSummaryQueryKey(query),
    queryFn: () => opdApi.getConsultationSummary(query),
    placeholderData: (previous) => previous,
  });
}
