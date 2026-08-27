import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createInvoice, type PatientDisplaySnapshot } from '../apiBillingRepository';
import type { BillingFormValues } from '../billingValidation';

export function useCreateInvoiceMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      patientId,
      visitId,
      values,
      patient,
    }: {
      patientId: string;
      visitId: string;
      values: BillingFormValues;
      patient: PatientDisplaySnapshot;
    }) => createInvoice(patientId, visitId, values, patient),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['billings'] }),
  });
}
