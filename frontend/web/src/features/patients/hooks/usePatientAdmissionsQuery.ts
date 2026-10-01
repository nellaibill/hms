import { useQuery } from '@tanstack/react-query';
import { admissionsApi } from '../../../services/apiClient';

/** One patient's IPD admissions (GET /api/v1/ipd/admissions?patientId=…) — backs the Overview
 * tab's "IP Admissions" count. An admission made through the IPD module never creates an IP
 * PatientVisit, so counting IP visits missed every real admission. Capped at the API's 100-row
 * page, far beyond any one patient's admission history. Pass enabled=false when the viewer lacks
 * the IPD feature or clinical-care.view, since the endpoint would only answer 403. */
export function usePatientAdmissionsQuery(patientId: string | undefined, enabled = true) {
  return useQuery({
    queryKey: ['ipd', 'admissions', 'by-patient', patientId],
    queryFn: async () => (await admissionsApi.getAdmissions({ patientId: patientId as string, pageSize: 100 })).items,
    enabled: enabled && Boolean(patientId),
  });
}
