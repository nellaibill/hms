import { useQuery } from '@tanstack/react-query';
import { getInvoicesByPatientId } from '../apiBillingRepository';

export function usePatientInvoicesQuery(patientId: string | undefined) {
  return useQuery({
    queryKey: ['billings', 'by-patient', patientId],
    queryFn: () => getInvoicesByPatientId(patientId as string),
    enabled: Boolean(patientId),
  });
}
