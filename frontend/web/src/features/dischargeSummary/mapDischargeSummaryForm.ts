import type { DischargeSummary, DischargeSummaryFormValues, UpdateDischargeSummaryRequest } from '@hms/shared';

function toStringValue(value: string | null | undefined): string {
  return value ?? '';
}

function toOptionalNumber(value: number | null | undefined): number | undefined {
  return value === null || value === undefined ? undefined : value;
}

/** "YYYY-MM-DDTHH:mm" for an <input type="datetime-local"> value, local time (mirrors how
 * DischargeForm/AdmissionForm already handle datetime-local elsewhere in this codebase). */
function toDateTimeLocalValue(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '';
  const pad = (num: number) => String(num).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

/** Seeds the edit form from a freshly-loaded DischargeSummary — null/undefined API values
 * become '' (strings) or undefined (numbers) so react-hook-form's controlled inputs never
 * flip from uncontrolled to controlled. */
export function toFormValues(summary: DischargeSummary): DischargeSummaryFormValues {
  return {
    finalDiagnosis: toStringValue(summary.finalDiagnosis),

    chiefComplaints: toStringValue(summary.chiefComplaints),
    historyOfPresentingIllness: toStringValue(summary.historyOfPresentingIllness),
    pastMedicalHistory: toStringValue(summary.pastMedicalHistory),
    pastSurgicalHistory: toStringValue(summary.pastSurgicalHistory),
    familyHistory: toStringValue(summary.familyHistory),
    personalHistory: toStringValue(summary.personalHistory),

    generalExamination: toStringValue(summary.generalExamination),
    cvsFindings: toStringValue(summary.cvsFindings),
    rsFindings: toStringValue(summary.rsFindings),
    paFindings: toStringValue(summary.paFindings),
    cnsFindings: toStringValue(summary.cnsFindings),
    localExamination: toStringValue(summary.localExamination),
    gait: toStringValue(summary.gait),

    heightCm: toOptionalNumber(summary.heightCm),
    weightKg: toOptionalNumber(summary.weightKg),
    pulseRate: toOptionalNumber(summary.pulseRate),
    respiratoryRate: toOptionalNumber(summary.respiratoryRate),
    temperatureF: toOptionalNumber(summary.temperatureF),
    spO2Percent: toOptionalNumber(summary.spO2Percent),
    bloodPressure: toStringValue(summary.bloodPressure),

    courseInHospital: toStringValue(summary.courseInHospital),

    procedureName: toStringValue(summary.procedureName),
    procedureDateTime: summary.procedureDateTime ? toDateTimeLocalValue(summary.procedureDateTime) : '',
    primarySurgeon: toStringValue(summary.primarySurgeon),
    assistantSurgeons: toStringValue(summary.assistantSurgeons),
    anaesthetist: toStringValue(summary.anaesthetist),
    anaesthesia: toStringValue(summary.anaesthesia),
    surgicalPosition: toStringValue(summary.surgicalPosition),
    intraOperativeFindings: toStringValue(summary.intraOperativeFindings),
    operativeNotes: toStringValue(summary.operativeNotes),

    diet: toStringValue(summary.diet),
    woundCare: toStringValue(summary.woundCare),
    activity: toStringValue(summary.activity),
    physiotherapy: toStringValue(summary.physiotherapy),
    reviewInstructions: toStringValue(summary.reviewInstructions),
    emergencyInstructions: toStringValue(summary.emergencyInstructions),
    conditionAtDischarge: toStringValue(summary.conditionAtDischarge),

    medications: summary.medications.map((medication) => ({
      sortOrder: medication.sortOrder,
      drugName: medication.drugName,
      dose: medication.dose,
      route: medication.route,
      morningQty: medication.morningQty,
      noonQty: medication.noonQty,
      eveningQty: medication.eveningQty,
      nightQty: medication.nightQty,
      durationDays: medication.durationDays,
      foodInstruction: medication.foodInstruction,
    })),
  };
}

/** Converts the parsed (post-zod) form values back into the PUT request shape — '' becomes
 * null for optional strings, matching what UpdateDischargeSummaryRequest expects. */
export function toUpdateRequest(values: DischargeSummaryFormValues): UpdateDischargeSummaryRequest {
  const orNull = (value: string | undefined) => (value ? value : null);

  return {
    finalDiagnosis: orNull(values.finalDiagnosis),

    chiefComplaints: orNull(values.chiefComplaints),
    historyOfPresentingIllness: orNull(values.historyOfPresentingIllness),
    pastMedicalHistory: orNull(values.pastMedicalHistory),
    pastSurgicalHistory: orNull(values.pastSurgicalHistory),
    familyHistory: orNull(values.familyHistory),
    personalHistory: orNull(values.personalHistory),

    generalExamination: orNull(values.generalExamination),
    cvsFindings: orNull(values.cvsFindings),
    rsFindings: orNull(values.rsFindings),
    paFindings: orNull(values.paFindings),
    cnsFindings: orNull(values.cnsFindings),
    localExamination: orNull(values.localExamination),
    gait: orNull(values.gait),

    heightCm: values.heightCm ?? null,
    weightKg: values.weightKg ?? null,
    pulseRate: values.pulseRate ?? null,
    respiratoryRate: values.respiratoryRate ?? null,
    temperatureF: values.temperatureF ?? null,
    spO2Percent: values.spO2Percent ?? null,
    bloodPressure: orNull(values.bloodPressure),

    courseInHospital: orNull(values.courseInHospital),

    procedureName: orNull(values.procedureName),
    procedureDateTime: values.procedureDateTime ? new Date(values.procedureDateTime).toISOString() : null,
    primarySurgeon: orNull(values.primarySurgeon),
    assistantSurgeons: orNull(values.assistantSurgeons),
    anaesthetist: orNull(values.anaesthetist),
    anaesthesia: orNull(values.anaesthesia),
    surgicalPosition: orNull(values.surgicalPosition),
    intraOperativeFindings: orNull(values.intraOperativeFindings),
    operativeNotes: orNull(values.operativeNotes),

    diet: orNull(values.diet),
    woundCare: orNull(values.woundCare),
    activity: orNull(values.activity),
    physiotherapy: orNull(values.physiotherapy),
    reviewInstructions: orNull(values.reviewInstructions),
    emergencyInstructions: orNull(values.emergencyInstructions),
    conditionAtDischarge: orNull(values.conditionAtDischarge),

    medications: values.medications,
  };
}
