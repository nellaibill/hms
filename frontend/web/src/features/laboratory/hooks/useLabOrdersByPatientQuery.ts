import { useQuery } from '@tanstack/react-query';
import { getLabOrdersByPatientId } from '../apiLaboratoryRepository';

export const labOrdersByPatientQueryKey = (patientId: string | undefined) => ['labOrders', 'by-patient', patientId] as const;

/** Every lab order for one patient, newest first — backs LabDetailsCard's real per-test status
 * (find the one whose invoiceId matches the bill) and PatientDetails' own lab history, if any. */
export function useLabOrdersByPatientQuery(patientId: string | undefined) {
  return useQuery({
    queryKey: labOrdersByPatientQueryKey(patientId),
    queryFn: () => getLabOrdersByPatientId(patientId as string),
    enabled: Boolean(patientId),
  });
}
