import { z } from 'zod';
import {
  ALLERGY_CATEGORIES,
  ALLERGY_SEVERITIES,
  ARRIVAL_SOURCE_CATEGORIES,
  BLOOD_GROUPS,
  ENCOUNTER_TYPES,
  OFFLINE_AD_CHANNELS,
  ONLINE_AD_CHANNELS,
  PATIENT_GENDERS,
  PATIENT_RELATIVE_REFERRAL_SOURCES,
  PHONE_RELATIONS,
  REFERRAL_COLUMN_CATEGORIES,
  RELATIONSHIPS,
  TITLES,
} from '../../enums';

/**
 * UI-first schema matching the source documents exactly (LH Software.docx + the Patient
 * Mode of Arrival Form) — distinct from ./patientValidation.ts, which mirrors the
 * *current* backend Contracts. See frontend/web/src/pages/patients/*.tsx's toRequest()
 * for how this bridges to the existing API shape pending the backend Phase 2 update.
 */
const phonePattern = /^[0-9+\-() ]*$/;
const pincodePattern = /^[0-9]{6}$/;

const phoneEntrySchema = z.object({
  number: z.string().trim().max(20).regex(phonePattern, 'Enter a valid phone number'),
  relation: z.enum(PHONE_RELATIONS),
});

const primaryPhoneSchema = z.object({
  number: z.string().trim().min(1, 'Primary phone is required').max(20).regex(phonePattern, 'Enter a valid phone number'),
  relation: z.enum(PHONE_RELATIONS),
});

const arrivalSourceSchema = z
  .object({
    category: z.enum(ARRIVAL_SOURCE_CATEGORIES),
    doctorReferral: z
      .object({
        doctorName: z.string().trim().max(150).optional().or(z.literal('')),
        department: z.string().trim().max(100).optional().or(z.literal('')),
        hospital: z.string().trim().max(150).optional().or(z.literal('')),
      })
      .optional(),
    patientRelativeReferral: z
      .object({
        source: z.enum(PATIENT_RELATIVE_REFERRAL_SOURCES),
        details: z.string().trim().max(200).optional().or(z.literal('')),
      })
      .optional(),
    onlineAd: z
      .object({
        channel: z.enum(ONLINE_AD_CHANNELS),
        details: z.string().trim().max(200).optional().or(z.literal('')),
      })
      .optional(),
    offlineAd: z
      .object({
        channel: z.enum(OFFLINE_AD_CHANNELS),
        details: z.string().trim().max(200).optional().or(z.literal('')),
      })
      .optional(),
  })
  .superRefine((data, ctx) => {
    if (data.category === 'DoctorReferral' && !data.doctorReferral?.doctorName) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['doctorReferral', 'doctorName'], message: 'Referring doctor name is required' });
    }
    if (data.category === 'PatientOrRelativeReferral') {
      if (!data.patientRelativeReferral?.source) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['patientRelativeReferral', 'source'], message: 'Select a referral source' });
      } else if (data.patientRelativeReferral.source === 'Other' && !data.patientRelativeReferral.details) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['patientRelativeReferral', 'details'], message: 'Please specify' });
      }
    }
    if (data.category === 'OnlineAdvertisement') {
      if (!data.onlineAd?.channel) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['onlineAd', 'channel'], message: 'Select a channel' });
      } else if (data.onlineAd.channel === 'Other' && !data.onlineAd.details) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['onlineAd', 'details'], message: 'Please specify' });
      }
    }
    if (data.category === 'OfflineAdvertisement') {
      if (!data.offlineAd?.channel) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['offlineAd', 'channel'], message: 'Select a channel' });
      } else if (data.offlineAd.channel === 'Other' && !data.offlineAd.details) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['offlineAd', 'details'], message: 'Please specify' });
      }
    }
  });

const referralColumnSchema = z.object({
  category: z.enum(REFERRAL_COLUMN_CATEGORIES),
  details: z.string().trim().max(200).optional().or(z.literal('')),
  contactNumber: z.string().trim().max(20).regex(phonePattern, 'Enter a valid phone number').optional().or(z.literal('')),
});

const registrationDetailsUiSchema = z
  .object({
    encounterType: z.enum(ENCOUNTER_TYPES),
    department: z.string().trim().min(1, 'Department is required').max(200),
    consultant: z.string().trim().min(1, 'Consultant is required').max(200),
    appointmentType: z.string().trim().max(100).optional().or(z.literal('')),
    admissionType: z.enum(['MLC', 'NMLC']).optional().or(z.literal('')),
    referral: referralColumnSchema.optional(),
    category: z.string().trim().max(100).optional().or(z.literal('')),
  })
  .superRefine((data, ctx) => {
    if ((data.encounterType === 'IP' || data.encounterType === 'Emergency') && !data.admissionType) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['admissionType'],
        message: 'Admission type (MLC/NMLC) is required for IP and Emergency encounters',
      });
    }
    if (data.referral && !data.referral.category) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['referral', 'category'], message: 'Select a referral category' });
    }
  });

const demographicsUiSchema = {
  title: z.enum(TITLES),
  firstName: z.string().trim().min(1, 'First name is required').max(100),
  lastName: z.string().trim().min(1, 'Last name is required').max(100),
  dateOfBirth: z
    .string()
    .min(1, 'Date of birth is required')
    .refine((value) => new Date(value) <= new Date(), 'Date of birth cannot be in the future'),
  gender: z.enum(PATIENT_GENDERS),
  bloodGroup: z.enum(BLOOD_GROUPS).optional().or(z.literal('')),

  addressLine1: z.string().trim().min(1, 'Address is required').max(200),
  addressLine2: z.string().max(200).optional().or(z.literal('')),
  addressLine3: z.string().max(200).optional().or(z.literal('')),
  district: z.string().trim().min(1, 'District is required').max(100),
  state: z.string().trim().min(1, 'State is required').max(100),
  pincode: z.string().regex(pincodePattern, 'Pincode must be 6 digits'),

  primaryPhone: primaryPhoneSchema,
  additionalPhones: z.array(phoneEntrySchema).max(2, 'Up to two additional numbers').default([]),
  email: z.string().email('Enter a valid email address').max(256).optional().or(z.literal('')),
  profession: z.string().max(100).optional().or(z.literal('')),

  emergencyContactRelationship: z.enum(RELATIONSHIPS),
  emergencyContactName: z.string().trim().min(1, 'Emergency contact name is required').max(150),
  emergencyContactPhone: z
    .string()
    .trim()
    .min(1, 'Emergency contact phone is required')
    .max(20)
    .regex(phonePattern, 'Enter a valid phone number'),

  hasKnownAllergy: z.boolean(),
  allergyCategory: z.enum(ALLERGY_CATEGORIES).optional().or(z.literal('')),
  allergySpecify: z.string().max(200).optional().or(z.literal('')),
  allergySeverity: z.enum(ALLERGY_SEVERITIES).optional().or(z.literal('')),
};

const allergyRefinement = (
  data: { hasKnownAllergy: boolean; allergyCategory?: string; allergySeverity?: string },
  ctx: z.RefinementCtx,
) => {
  if (data.hasKnownAllergy && !data.allergyCategory) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['allergyCategory'], message: 'Allergy category is required' });
  }
  if (data.hasKnownAllergy && !data.allergySeverity) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['allergySeverity'], message: 'Allergy severity is required' });
  }
};

export const patientRegistrationUiSchema = z
  .object({
    ...demographicsUiSchema,
    arrivalSource: arrivalSourceSchema,
    registration: registrationDetailsUiSchema,
  })
  .superRefine(allergyRefinement);

export const patientEditUiSchema = z.object(demographicsUiSchema).superRefine(allergyRefinement);

export type PatientRegistrationUiFormValues = z.infer<typeof patientRegistrationUiSchema>;
export type PatientEditUiFormValues = z.infer<typeof patientEditUiSchema>;
