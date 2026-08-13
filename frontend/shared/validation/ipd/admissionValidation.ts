import { z } from 'zod';
import { CHARGE_TYPES, DISCHARGE_TYPES, IPD_ADMISSION_TYPES } from '../../enums/ipd';

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
