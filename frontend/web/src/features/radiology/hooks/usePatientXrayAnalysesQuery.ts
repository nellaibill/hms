import type { XrayAiAnalysisResponse } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { radiologyApi } from '../../../services/apiClient';

export const patientXrayAnalysesQueryKey = (patientId: string) => ['radiology', 'analyses', patientId] as const;

/** Every saved AI analysis of a patient's images (newest first) — what makes a result outlive the
 * page: it's read back from the patient's record, not held in component state. */
export function usePatientXrayAnalysesQuery(patientId: string | undefined) {
  return useQuery({
    queryKey: patientXrayAnalysesQueryKey(patientId ?? ''),
    enabled: Boolean(patientId),
    queryFn: (): Promise<XrayAiAnalysisResponse[]> => radiologyApi.getPatientAnalyses(patientId!),
  });
}
