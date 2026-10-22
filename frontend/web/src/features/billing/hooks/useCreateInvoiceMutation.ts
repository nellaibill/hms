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
    // Paying a consultation can give the visit its OP No (ADR-103), so the patient's visits
    // are refreshed too — otherwise the patient page keeps showing the visit as unpaid.
    onSuccess: () =>
      Promise.all([queryClient.invalidateQueries({ queryKey: ['billings'] }), queryClient.invalidateQueries({ queryKey: ['patient-visits'] })]),
  });
}
