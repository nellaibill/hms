import type { Patient } from '@hms/shared';
import { patientsApi } from '@/services/apiClient';
import type { CounterPatient } from './cart';

export function toCounterPatient(patient: Patient): CounterPatient {
  return {
    id: patient.id,
    name: `${patient.firstName} ${patient.lastName}`.trim(),
    uhid: patient.uhid,
    phone: patient.primaryPhone || undefined,
    allergies: (patient.allergies ?? []).map((allergy) => allergy.specify || allergy.allergyType).filter(Boolean),
  };
}

/** The patient a prescription or an indent is for, as the counter shows them. */
export async function loadCounterPatient(patientId: string): Promise<CounterPatient> {
  return toCounterPatient(await patientsApi.getPatientById(patientId));
}
