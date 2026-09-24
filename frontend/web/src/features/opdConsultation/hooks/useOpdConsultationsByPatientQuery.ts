import { useQuery } from '@tanstack/react-query';
import { opdConsultationApi } from '@/services/apiClient';

/** Every consultation note already on file for one patient (newest first), each with its
 * header — read-only, so unlike useOpdConsultationQuery it never auto-creates a note. Backs
 * Patient Details' Medical Information tab. */
export function useOpdConsultationsByPatientQuery(patientId: string | undefined, enabled = true) {
  return useQuery({
    queryKey: ['opd-consultation', 'by-patient', patientId],
    queryFn: () => opdConsultationApi.listByPatient(patientId as string),
    enabled: enabled && Boolean(patientId),
  });
}
