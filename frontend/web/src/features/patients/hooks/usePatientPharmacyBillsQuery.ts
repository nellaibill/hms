import { useQuery } from '@tanstack/react-query';
import { pharmacyApi } from '../../../services/apiClient';

/** A patient's pharmacy bills with their lines, and the returns against them, newest first —
 * backs the pharmacy cards on the Billing tab and the pharmacy events on the Timeline. Keyed
 * under 'pharmacy' so a bill or return made at the counter refreshes it. Pass enabled=false
 * for a viewer with neither pharmacy.view nor finance-billing.view (the API would refuse). */
export function usePatientPharmacyBillsQuery(patientId: string | undefined, enabled: boolean) {
  return useQuery({
    queryKey: ['pharmacy', 'patient-bills', patientId],
    queryFn: () => pharmacyApi.getPatientPharmacyBills(patientId as string),
    enabled: enabled && Boolean(patientId),
  });
}
