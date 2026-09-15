import { useQuery } from '@tanstack/react-query';
import { opdConsultationApi } from '@/services/apiClient';

/** Loads (the backend auto-creates on first call) the consultation note plus its read-only
 * header, keyed by consultationId — the same id the OPD Patient List's "Consult" action
 * already navigates here with. */
export function useOpdConsultationQuery(consultationId: string | undefined) {
  return useQuery({
    queryKey: ['opd-consultation', consultationId],
    queryFn: () => opdConsultationApi.getOrCreate(consultationId as string),
    enabled: Boolean(consultationId),
  });
}
