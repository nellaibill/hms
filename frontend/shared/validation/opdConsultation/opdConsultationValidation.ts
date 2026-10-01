import { z } from 'zod';
import { OPD_DIAGNOSIS_TYPES, OPD_INVESTIGATION_DEPARTMENTS, OPD_INVESTIGATION_PRIORITIES } from '../../dtos/opdConsultation/opdConsultation';
import { optionalBloodPressureSchema } from '../bloodPressure';

/**
 * Mirrors HMS.Modules.OpdConsultation.Application.Validators.SaveOpdConsultationRequestValidator
 * — client-side convenience only, the backend remains authoritative (docs/ApiStandards.md §7,
 * docs/FrontendArchitecture.md §9). No field here is required: Save Draft has no requirements
 * at all, and Complete's three required fields (see completeOpdConsultationSchema below) are a
 * stricter refinement of this same shape, not a separate one — matching
 * OpdConsultationService.CompleteAsync's own manual required-field check rather than a second
 * FluentValidation validator for the identical request shape.
 */
const optionalTrimmedString = (max: number) => z.string().trim().max(max).optional().or(z.literal(''));

/** Empty-string, undefined, or NaN (from a number input's raw text value on a blank field) all
 * mean "not entered" for an optional numeric vitals field — mirrors DischargeSummary's own
 * optionalBoundedNumber. */
function optionalBoundedNumber(min: number, max: number) {
  const message = `Must be between ${min} and ${max}`;
  return z.preprocess(
    (value) => (value === '' || value === undefined || value === null || (typeof value === 'number' && Number.isNaN(value)) ? undefined : value),
    z.coerce.number().min(min, message).max(max, message).optional(),
  );
}

/** A vital Complete can't go without. A blank number input arrives as '' (or NaN), which
 * z.coerce.number() would quietly turn into 0 and pass a min(0) check — so blank is mapped to
 * undefined first and reported as missing, and the value must be above 0 like the backend's
 * GreaterThan(0) rule. */
function requiredPositiveNumber(max: number, requiredMessage: string) {
  const rangeMessage = `Must be greater than 0 and at most ${max}`;
  return z.preprocess(
    (value) => (value === '' || value === undefined || value === null || (typeof value === 'number' && Number.isNaN(value)) ? undefined : value),
    z.coerce.number({ required_error: requiredMessage, invalid_type_error: requiredMessage }).gt(0, rangeMessage).max(max, rangeMessage),
  );
}

export const opdConsultationDiagnosisSchema = z.object({
  diagnosisId: z.string().trim().min(1, 'Select a diagnosis'),
  type: z.enum(OPD_DIAGNOSIS_TYPES),
  /** Display-only (the server resolves it on every response) — kept on the line so a diagnosis
   * added from the picker shows its name before the next save round-trip. Ignored server-side. */
  diagnosisName: z.string().optional().nullable(),
  icdCode: z.string().optional().nullable(),
});

export const opdConsultationInvestigationSchema = z.object({
  name: z.string().trim().min(1, 'Investigation name is required').max(200),
  department: z.enum(OPD_INVESTIGATION_DEPARTMENTS),
  priority: z.enum(OPD_INVESTIGATION_PRIORITIES),
  /** Catalog DiagnosticService id when picked from the list (OPD-01) — null for free text. */
  serviceId: z.string().optional().nullable(),
});

/** Mirrors the backend's prescription rules (OPD-02): only the drug name is required. */
export const opdConsultationPrescriptionSchema = z.object({
  drugName: z.string().trim().min(1, 'Drug name is required').max(200),
  dose: optionalTrimmedString(100),
  route: optionalTrimmedString(50),
  frequency: optionalTrimmedString(100),
  durationDays: z.preprocess(
    (value) => (value === '' || value === undefined || value === null || (typeof value === 'number' && Number.isNaN(value)) ? undefined : value),
    z.coerce.number().int('Whole days only').min(1, 'Must be between 1 and 365').max(365, 'Must be between 1 and 365').optional(),
  ),
  instructions: optionalTrimmedString(500),
});

export const saveOpdConsultationSchema = z.object({
  heightCm: optionalBoundedNumber(0, 300),
  weightKg: optionalBoundedNumber(0, 500),
  pulseRate: optionalBoundedNumber(0, 300),
  bloodPressure: optionalBloodPressureSchema,
  temperatureF: optionalBoundedNumber(70, 115),
  spO2Percent: optionalBoundedNumber(0, 100),

  presentingComplaints: optionalTrimmedString(2000),
  clinicalHistory: optionalTrimmedString(4000),
  examinationFindings: optionalTrimmedString(4000),

  diagnoses: z.array(opdConsultationDiagnosisSchema),
  investigations: z.array(opdConsultationInvestigationSchema),
  prescriptions: z.array(opdConsultationPrescriptionSchema).max(30, 'At most 30 medicines per consultation'),

  planOfManagement: optionalTrimmedString(4000),

  reviewDate: z.string().trim().optional().or(z.literal('')),
  followUpInstructions: optionalTrimmedString(2000),

  emergencyReviewInstructions: optionalTrimmedString(2000),

  referralDepartmentId: z.string().trim().optional().or(z.literal('')),
  referralConsultantId: z.string().trim().optional().or(z.literal('')),
  referralReason: optionalTrimmedString(1000),
});

export type OpdConsultationFormValues = z.infer<typeof saveOpdConsultationSchema>;

/** Same shape as saveOpdConsultationSchema, but PresentingComplaints/HeightCm/WeightKg become
 * required — matches the form's own `*` markers and OpdConsultationService.CompleteAsync's
 * server-side check. Used only to validate before calling Complete; Save Draft always uses the
 * lenient schema above regardless of which fields are filled in. */
export const completeOpdConsultationSchema = saveOpdConsultationSchema.extend({
  presentingComplaints: z.string().trim().min(1, 'Presenting complaints are required to complete a consultation.').max(2000),
  heightCm: requiredPositiveNumber(300, 'Height is required to complete a consultation.'),
  weightKg: requiredPositiveNumber(500, 'Weight is required to complete a consultation.'),
});
