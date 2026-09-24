import { useQuery } from '@tanstack/react-query';
import { opdConsultationApi } from '@/services/apiClient';

/** The catalog-linked investigations the doctor ordered on a visit's OPD consultation — OPD
 * Billing Entry pre-adds these as Laboratory/Radiology lines (regression report OPD-01: the
 * doctor's order previously never reached billing or the lab). */
export function useBillableInvestigationsQuery(visitId: string | undefined) {
  return useQuery({
    queryKey: ['billings', 'billable-investigations', visitId],
    queryFn: () => opdConsultationApi.listBillableInvestigations(visitId as string),
    enabled: Boolean(visitId),
  });
}
