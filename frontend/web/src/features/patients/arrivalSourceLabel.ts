import type { ModeOfArrivalSource } from '@hms/shared';

/** "DoctorReferral" -> "Doctor Referral" — the raw enum values are PascalCase, not display-
 * ready like MaritalStatus's. Note this is only ever a coarse category, not a specific
 * referring doctor — there is no field linking a referral to an actual consultant/doctor
 * record anywhere in this codebase today. */
const LABELS: Record<ModeOfArrivalSource, string> = {
  DoctorReferral: 'Doctor Referral',
  PatientOrRelativeReferral: 'Patient/Relative Referral',
  OnlineAdvertisement: 'Online Advertisement',
  OfflineAdvertisement: 'Offline Advertisement',
};

export function arrivalSourceLabel(source: ModeOfArrivalSource): string {
  return LABELS[source] ?? source;
}
