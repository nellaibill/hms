import { z } from 'zod';
import { CHARGE_TYPES, DISCHARGE_TYPES, DOCTOR_ORDER_TYPES, IPD_ADMISSION_TYPES, NURSING_SHIFTS } from '../../enums/ipd';

// datetime-local inputs let a user type an out-of-range year (e.g. "222222") that the
// browser still reports as a non-empty value — guard against that reaching new Date(...).toISOString().
function isValidDateTimeLocal(value: string): boolean {
  const date = new Date(value);
  return !Number.isNaN(date.getTime()) && date.getFullYear() >= 1900 && date.getFullYear() <= 2999;
}

/**
 * Mirrors HMS.Modules.IPD.Application.Validators.CreateAdmissionRequestValidator exactly
 * (client-side convenience only, the backend remains authoritative —
 * docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
export const createAdmissionSchema = z.object({
  patientId: z.string().trim().min(1, 'Patient is required'),
  departmentId: z.string().trim().min(1, 'Department is required'),
  consultantId: z.string().trim().min(1, 'Consultant is required'),
  wardId: z.string().trim().min(1, 'Ward is required'),
  bedId: z.string().trim().min(1, 'Bed is required'),
  admissionDateTime: z
    .string()
    .trim()
    .optional()
    .or(z.literal(''))
    .refine((val) => !val || isValidDateTimeLocal(val), { message: 'Enter a valid admission date and time' }),
  admissionType: z.enum(IPD_ADMISSION_TYPES, { message: 'Admission type is required' }),
  reasonForAdmission: z.string().trim().min(1, 'Reason for admission is required').max(500),
});

export type AdmissionFormValues = z.infer<typeof createAdmissionSchema>;

/** Mirrors HMS.Modules.IPD.Application.Validators.TransferBedRequestValidator. */
export const transferBedSchema = z.object({
  newWardId: z.string().trim().min(1, 'Ward is required'),
  newBedId: z.string().trim().min(1, 'Bed is required'),
  transferReason: z.string().trim().max(500).optional().or(z.literal('')),
});

export type TransferBedFormValues = z.infer<typeof transferBedSchema>;

/** Mirrors HMS.Modules.IPD.Application.Validators.DischargeAdmissionRequestValidator. */
export const dischargeAdmissionSchema = z.object({
  dischargeDateTime: z
    .string()
    .trim()
    .min(1, 'Discharge date/time is required')
    .refine(isValidDateTimeLocal, { message: 'Enter a valid discharge date and time' }),
  dischargeType: z.enum(DISCHARGE_TYPES, { message: 'Discharge type is required' }),
  finalDiagnosis: z.string().trim().max(1000).optional().or(z.literal('')),
  dischargeNotes: z.string().trim().max(2000).optional().or(z.literal('')),
  followUpAdvice: z.string().trim().max(2000).optional().or(z.literal('')),
});

export type DischargeAdmissionFormValues = z.infer<typeof dischargeAdmissionSchema>;

/** Mirrors HMS.Modules.IPD.Application.Validators.CreateAdmissionChargeRequestValidator. */
export const createAdmissionChargeSchema = z.object({
  chargeType: z.enum(CHARGE_TYPES, { message: 'Charge type is required' }),
  amount: z.coerce.number().positive('Amount must be greater than 0'),
  remarks: z.string().trim().max(500).optional().or(z.literal('')),
});

export type AdmissionChargeFormValues = z.infer<typeof createAdmissionChargeSchema>;

// Helper for an optional numeric input: an empty string (unfilled field) becomes undefined
// rather than being coerced to 0 — z.coerce.number() turns "" into 0 via JS's Number(""),
// so a plain `.optional().or(z.literal(''))` union only falls through to undefined when 0
// itself fails the range check (e.g. Temperature), silently keeping 0 for every field whose
// range includes 0 (Pulse, SpO2, Weight, Pain, Glucose, etc.) — preprocessing "" to undefined
// *before* coercion avoids that entirely, for every field regardless of its range.
function optionalNumber(min: number, max: number, message: string) {
  return z.preprocess(
    (val) => (val === '' || val === undefined || val === null ? undefined : val),
    z.coerce.number().refine((val) => val >= min && val <= max, message).optional(),
  );
}

/** Mirrors HMS.Modules.IPD.Application.Validators.CreateVitalsReadingRequestValidator. */
export const createVitalsReadingSchema = z.object({
  recordedAt: z
    .string()
    .trim()
    .min(1, 'Recorded date/time is required')
    .refine(isValidDateTimeLocal, { message: 'Enter a valid date and time' }),
  temperatureF: optionalNumber(70, 115, 'Temperature must be between 70 and 115°F'),
  pulseRate: optionalNumber(0, 300, 'Pulse must be between 0 and 300'),
  respiratoryRate: optionalNumber(0, 150, 'Respiratory rate must be between 0 and 150'),
  bloodPressureSystolic: optionalNumber(40, 300, 'Systolic BP must be between 40 and 300'),
  bloodPressureDiastolic: optionalNumber(20, 200, 'Diastolic BP must be between 20 and 200'),
  spO2Percent: optionalNumber(0, 100, 'SpO2 must be between 0 and 100'),
  weightKg: optionalNumber(0, 500, 'Weight must be between 0 and 500 kg'),
  heightCm: optionalNumber(0, 300, 'Height must be between 0 and 300 cm'),
  painScore: optionalNumber(0, 10, 'Pain score must be between 0 and 10'),
  bloodGlucoseMgDl: optionalNumber(0, 1000, 'Blood glucose must be between 0 and 1000 mg/dL'),
  notes: z.string().trim().max(1000).optional().or(z.literal('')),
});

export type VitalsReadingFormValues = z.infer<typeof createVitalsReadingSchema>;

/** Mirrors HMS.Modules.IPD.Application.Validators.CreateProgressNoteRequestValidator. */
export const createProgressNoteSchema = z.object({
  noteDateTime: z
    .string()
    .trim()
    .min(1, 'Note date/time is required')
    .refine(isValidDateTimeLocal, { message: 'Enter a valid date and time' }),
  clinicalCondition: z.string().trim().max(2000).optional().or(z.literal('')),
  progress: z.string().trim().max(2000).optional().or(z.literal('')),
  diagnosis: z.string().trim().max(2000).optional().or(z.literal('')),
  assessment: z.string().trim().max(2000).optional().or(z.literal('')),
  plan: z.string().trim().max(2000).optional().or(z.literal('')),
  instructions: z.string().trim().max(2000).optional().or(z.literal('')),
});

export type ProgressNoteFormValues = z.infer<typeof createProgressNoteSchema>;

/** Mirrors HMS.Modules.IPD.Application.Validators.CreateNursingAssessmentRequestValidator. */
export const createNursingAssessmentSchema = z.object({
  assessedAt: z
    .string()
    .trim()
    .min(1, 'Assessed date/time is required')
    .refine(isValidDateTimeLocal, { message: 'Enter a valid date and time' }),
  generalCondition: z.string().trim().max(500).optional().or(z.literal('')),
  consciousnessLevel: z.string().trim().max(200).optional().or(z.literal('')),
  mobility: z.string().trim().max(500).optional().or(z.literal('')),
  nutritionStatus: z.string().trim().max(500).optional().or(z.literal('')),
  fallRisk: z.string().trim().max(200).optional().or(z.literal('')),
  pressureSoreRisk: z.string().trim().max(200).optional().or(z.literal('')),
  skinCondition: z.string().trim().max(500).optional().or(z.literal('')),
  painScore: optionalNumber(0, 10, 'Pain score must be between 0 and 10'),
  notes: z.string().trim().max(1000).optional().or(z.literal('')),
});

export type NursingAssessmentFormValues = z.infer<typeof createNursingAssessmentSchema>;

/** Mirrors HMS.Modules.IPD.Application.Validators.CreateNursingNoteRequestValidator. */
export const createNursingNoteSchema = z.object({
  noteDateTime: z
    .string()
    .trim()
    .min(1, 'Note date/time is required')
    .refine(isValidDateTimeLocal, { message: 'Enter a valid date and time' }),
  shift: z.enum(NURSING_SHIFTS, { message: 'Shift is required' }),
  observation: z.string().trim().max(2000).optional().or(z.literal('')),
  intervention: z.string().trim().max(2000).optional().or(z.literal('')),
  patientResponse: z.string().trim().max(2000).optional().or(z.literal('')),
  remarks: z.string().trim().max(2000).optional().or(z.literal('')),
});

export type NursingNoteFormValues = z.infer<typeof createNursingNoteSchema>;

/** Mirrors HMS.Modules.IPD.Application.Validators.CreateDoctorOrderRequestValidator. */
export const createDoctorOrderSchema = z.object({
  orderType: z.enum(DOCTOR_ORDER_TYPES, { message: 'Order type is required' }),
  description: z.string().trim().min(1, 'Description is required').max(1000),
  instructions: z.string().trim().max(2000).optional().or(z.literal('')),
  orderedAt: z
    .string()
    .trim()
    .min(1, 'Ordered date/time is required')
    .refine(isValidDateTimeLocal, { message: 'Enter a valid date and time' }),
});

export type DoctorOrderFormValues = z.infer<typeof createDoctorOrderSchema>;

/** Mirrors HMS.Modules.IPD.Application.Validators.CreateMedicationOrderRequestValidator. */
export const createMedicationOrderSchema = z
  .object({
    drugName: z.string().trim().min(1, 'Drug name is required').max(200),
    dose: z.string().trim().min(1, 'Dose is required').max(100),
    route: z.string().trim().min(1, 'Route is required').max(100),
    frequency: z.string().trim().min(1, 'Frequency is required').max(100),
    startDate: z
      .string()
      .trim()
      .min(1, 'Start date is required')
      .refine(isValidDateTimeLocal, { message: 'Enter a valid date and time' }),
    endDate: z
      .string()
      .trim()
      .optional()
      .or(z.literal(''))
      .refine((val) => !val || isValidDateTimeLocal(val), { message: 'Enter a valid date and time' }),
    instructions: z.string().trim().max(2000).optional().or(z.literal('')),
    orderedAt: z
      .string()
      .trim()
      .min(1, 'Ordered date/time is required')
      .refine(isValidDateTimeLocal, { message: 'Enter a valid date and time' }),
  })
  .refine((data) => !data.endDate || new Date(data.endDate) >= new Date(data.startDate), {
    message: 'End date must be on or after the start date',
    path: ['endDate'],
  });

export type MedicationOrderFormValues = z.infer<typeof createMedicationOrderSchema>;

/** Mirrors HMS.Modules.IPD.Application.Validators.CreateMedicationAdministrationRequestValidator. */
export const createMedicationAdministrationSchema = z.object({
  scheduledTime: z
    .string()
    .trim()
    .min(1, 'Scheduled time is required')
    .refine(isValidDateTimeLocal, { message: 'Enter a valid date and time' }),
  wasGiven: z.enum(['given', 'not-given'], { message: 'Select whether the dose was given' }),
  administeredAt: z
    .string()
    .trim()
    .optional()
    .or(z.literal(''))
    .refine((val) => !val || isValidDateTimeLocal(val), { message: 'Enter a valid date and time' }),
  reason: z.string().trim().max(500).optional().or(z.literal('')),
  remarks: z.string().trim().max(1000).optional().or(z.literal('')),
});

export type MedicationAdministrationFormValues = z.infer<typeof createMedicationAdministrationSchema>;
