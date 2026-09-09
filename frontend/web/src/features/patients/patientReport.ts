import type { Patient, PatientVisit } from '@hms/shared';

export interface ReportDateRange {
  from: string;
  to: string;
}

export interface CountBreakdownRow {
  label: string;
  count: number;
}

function countBy<T>(items: T[], keyFn: (item: T) => string | null | undefined): CountBreakdownRow[] {
  const counts = new Map<string, number>();
  for (const item of items) {
    const key = keyFn(item);
    if (!key) continue;
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }
  return Array.from(counts, ([label, count]) => ({ label, count })).sort((a, b) => b.count - a.count);
}

export function getPatientsByGender(patients: Patient[]): CountBreakdownRow[] {
  return countBy(patients, (patient) => patient.gender);
}

export function getPatientsByMaritalStatus(patients: Patient[]): CountBreakdownRow[] {
  return countBy(patients, (patient) => patient.maritalStatus);
}

export function getPatientsByBloodGroup(patients: Patient[]): CountBreakdownRow[] {
  return countBy(patients, (patient) => patient.bloodGroup);
}

/** Mode of Arrival Source — the closest thing to a "referral source" that exists on Patient
 * today (see patientArrivalSourceLabel.ts for why "DoctorReferral" isn't a specific doctor). */
export function getPatientsByArrivalSource(patients: Patient[]): CountBreakdownRow[] {
  return countBy(patients, (patient) => patient.modeOfArrivalSource);
}

/** One count per allergy *type* a patient has recorded, not per patient — a patient with two
 * different allergy types contributes to both buckets, matching how Discharge Summary/PDF
 * export already treats the allergies array (every entry shown, not just the first). */
export function getAllergyPrevalence(patients: Patient[]): CountBreakdownRow[] {
  const counts = new Map<string, number>();
  for (const patient of patients) {
    for (const allergy of patient.allergies) {
      counts.set(allergy.allergyType, (counts.get(allergy.allergyType) ?? 0) + 1);
    }
  }
  return Array.from(counts, ([label, count]) => ({ label, count })).sort((a, b) => b.count - a.count);
}

/** Registrations per month, oldest first — for the trend chart. Deliberately not restricted to
 * the selected date range's own bucket boundaries; the range filter already narrows `patients`
 * before this runs (see PatientReportsPage), so this just groups whatever's left. */
export function getRegistrationsOverTime(patients: Patient[]): { month: string; count: number }[] {
  const counts = new Map<string, number>();
  for (const patient of patients) {
    const month = patient.createdAt.slice(0, 7);
    counts.set(month, (counts.get(month) ?? 0) + 1);
  }
  return Array.from(counts, ([month, count]) => ({ month, count })).sort((a, b) => a.month.localeCompare(b.month));
}

export function getVisitsByType(visits: PatientVisit[]): CountBreakdownRow[] {
  return countBy(visits, (visit) => visit.visitType);
}

/** One count per consultation *line*, not per visit — a visit with 3 consultants across 2
 * departments contributes 3 lines total, split across whichever departments/consultants they
 * actually name (see PatientVisitConsultation's own shape — department/consultant live on the
 * consultation, not the visit itself, since a visit can span more than one). */
export function getVisitsByDepartment(visits: PatientVisit[]): CountBreakdownRow[] {
  const counts = new Map<string, number>();
  for (const visit of visits) {
    for (const consultation of visit.consultations) {
      counts.set(consultation.departmentId, (counts.get(consultation.departmentId) ?? 0) + 1);
    }
  }
  return Array.from(counts, ([label, count]) => ({ label, count })).sort((a, b) => b.count - a.count);
}

export function getVisitsByConsultant(visits: PatientVisit[]): CountBreakdownRow[] {
  const counts = new Map<string, number>();
  for (const visit of visits) {
    for (const consultation of visit.consultations) {
      counts.set(consultation.consultantId, (counts.get(consultation.consultantId) ?? 0) + 1);
    }
  }
  return Array.from(counts, ([label, count]) => ({ label, count })).sort((a, b) => b.count - a.count);
}
