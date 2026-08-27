import { useQuery } from '@tanstack/react-query';
import { patientsApi } from '../../../services/apiClient';

/** All recorded visits for a patient, newest first (see PatientsApi.getVisits) — backs the
 * Overview tab's At-a-Glance stats and Recent Visits table, and the Visits tab's full list. */
export function usePatientVisitsQuery(patientId: string | undefined) {
  return useQuery({
    queryKey: ['patient-visits', patientId],
    queryFn: () => patientsApi.getVisits(patientId as string),
    enabled: Boolean(patientId),
  });
}
