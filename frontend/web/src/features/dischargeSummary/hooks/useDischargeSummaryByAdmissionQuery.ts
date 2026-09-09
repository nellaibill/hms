import { useQuery } from '@tanstack/react-query';
import { dischargeSummaryApi } from '@/services/apiClient';

/**
 * Loads the discharge summary for an admission, if one exists yet. A 404 (surfaced as an
 * ApiError by the shared HTTP client) means none has been created — callers distinguish that
 * from a real failure via `isError` + the caught ApiError's status, so `retry: false` keeps a
 * genuine 404 from spinning through retries first.
 */
export function useDischargeSummaryByAdmissionQuery(admissionId: string | undefined, enabled = true) {
  return useQuery({
    queryKey: ['discharge-summary', 'by-admission', admissionId],
    queryFn: () => dischargeSummaryApi.getByAdmissionId(admissionId as string),
    enabled: Boolean(admissionId) && enabled,
    retry: false,
  });
}
