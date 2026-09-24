import type { OpdConsultationDiagnosis, OpdConsultationPrescription } from '@hms/shared';
import { resolveRecordLabel } from '@/features/masters';

/** Diagnosis display name — the server resolves the catalog name/ICD code onto every response
 * (regression report OPD-03), so this works for roles without Masters access; the reference
 * cache is only a fallback for anything without it. */
export function formatDiagnosisLabel(diagnosis: Pick<OpdConsultationDiagnosis, 'diagnosisId' | 'diagnosisName' | 'icdCode'>): string {
  if (diagnosis.diagnosisName) {
    return diagnosis.icdCode ? `${diagnosis.diagnosisName} (${diagnosis.icdCode})` : diagnosis.diagnosisName;
  }
  return resolveRecordLabel('diagnosis', diagnosis.diagnosisId);
}

/** "1 tab · Oral · 1-0-1 · 5 days" — the optional prescription details, in reading order. */
export function formatPrescriptionDetails(prescription: OpdConsultationPrescription): string {
  return [
    prescription.dose,
    prescription.route,
    prescription.frequency,
    prescription.durationDays ? `${prescription.durationDays} day${prescription.durationDays === 1 ? '' : 's'}` : null,
  ]
    .filter(Boolean)
    .join(' · ');
}
