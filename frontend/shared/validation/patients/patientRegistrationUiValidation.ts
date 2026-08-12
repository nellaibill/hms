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
// A real phone number never has fewer than 10 digits in this system — the lookahead
// requires at least 10 digit characters anywhere in the string (formatting characters like
// "+91-98765-43210" don't count against that), which also subsumes the old "at least one
// digit" check (a symbol-only string like "----------" has zero digits and is rejected too).
// Kept in sync with backend's CreatePatientRequestValidator.PhonePattern/MinPhoneDigits.
const phonePattern = /^(?=(?:.*\d){10,})[0-9+\-() ]*$/;
// Spells out the allowed characters in prose rather than "digits/+/-/()/spaces" — that
// slash-separated form reads as if "/" itself were an allowed character, which it isn't.
const phonePatternMessage = 'Phone number must contain at least 10 digits, using only digits, spaces, and the characters + - ( ).';
const pincodePattern = /^[0-9]{6}$/;

// \p{L}/\p{M} (Unicode letter/mark categories) rather than A-Za-z so names in Indian
// scripts (Devanagari, Tamil, etc.) — not just ASCII — are accepted; still rejects digits
// and most symbols. Allows spaces/apostrophes/periods/hyphens for names like "Mary-Jane
// O'Brien" or "Dr. Rao". Kept in sync with backend's CreatePatientRequestValidator.NamePattern.
const namePattern = /^[\p{L}\p{M}][\p{L}\p{M}\s'.-]*$/u;
const namePatternMessage = 'Enter letters only.';

const phoneEntrySchema = z.object({
  number: z.string().trim().max(20).regex(phonePattern, phonePatternMessage),
  relation: z.enum(PHONE_RELATIONS),
});

const primaryPhoneSchema = z.object({
  number: z.string().trim().min(1, 'Primary phone is required').max(20).regex(phonePattern, phonePatternMessage),
  relation: z.enum(PHONE_RELATIONS),
});

const arrivalSourceSchema = z
  .object({
    category: z.enum(ARRIVAL_SOURCE_CATEGORIES),
    doctorReferral: z
      .object({
        doctorName: z.string().trim().max(150).optional().or(z.literal('')),
        // A Masters department id (from the same DepartmentSelect used for the main
        // Registration Details Department field), not free text — kept optional since a
        // referring doctor's department isn't always known/relevant. Not yet persisted to
        // the backend; see arrivalSource's top-of-file note in
        // PatientRegistrationCreatePage.tsx (Phase 2, same as the rest of arrivalSource).
        departmentId: z.string().trim().optional().or(z.literal('')),
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
  contactNumber: z.string().trim().max(20).regex(phonePattern, phonePatternMessage).optional().or(z.literal('')),
});

// Exported (not just used internally) so a future "record a new visit" form can reuse this
// exact schema instead of duplicating it — a follow-up visit's registration details are the
// same shape as a first-time registration's, so they should share one validator.
export const registrationDetailsUiSchema = z
  .object({
    encounterType: z.enum(ENCOUNTER_TYPES),
    departmentId: z.string().trim().min(1, 'Department is required'),
    consultantId: z.string().trim().min(1, 'Consultant is required'),
    appointmentTypeId: z.string().trim().optional().or(z.literal('')),
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
  firstName: z.string().trim().min(1, 'First name is required').max(100).regex(namePattern, namePatternMessage),
  lastName: z.string().trim().min(1, 'Last name is required').max(100).regex(namePattern, namePatternMessage),
  dateOfBirth: z
    .string()
    .min(1, 'Date of birth is required')
    .refine((value) => new Date(value) <= new Date(), 'Date of birth cannot be in the future')
    // Kept in sync with backend's CreatePatientRequestValidator.MinDateOfBirth (130 years) —
    // generous enough to never reject a real patient while catching an obvious data-entry
    // slip like typing "1023" instead of "2023".
    .refine((value) => {
      const minDate = new Date();
      minDate.setFullYear(minDate.getFullYear() - 130);
      return new Date(value) >= minDate;
    }, 'Date of birth is too far in the past — please check the year.'),
  gender: z.enum(PATIENT_GENDERS),
  // Required — not optional — so the field can't be silently skipped end-to-end; the
  // dropdown always defaults to and includes 'Unknown' as an explicit, deliberate choice
  // for "not known/not recorded" rather than leaving the field genuinely unset.
  bloodGroup: z.enum(BLOOD_GROUPS, { message: 'Blood group is required — select Unknown if it isn\'t known.' }),

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
  emergencyContactName: z.string().trim().min(1, 'Emergency contact name is required').max(150).regex(namePattern, namePatternMessage),
  emergencyContactPhone: z
    .string()
    .trim()
    .min(1, 'Emergency contact phone is required')
    .max(20)
    .regex(phonePattern, phonePatternMessage),

  hasKnownAllergy: z.boolean(),
  allergyCategory: z.enum(ALLERGY_CATEGORIES).optional().or(z.literal('')),
  allergySpecify: z.string().max(200).optional().or(z.literal('')),
  allergySeverity: z.enum(ALLERGY_SEVERITIES).optional().or(z.literal('')),
};

// The backend composes AllergyType as `${category}: ${specify}` (see bridging.ts's
// toAllergyType) and caps the *composed* string at 200 chars — capping allergySpecify alone
// at 200 isn't enough, since the category label adds its own overhead (e.g. "Environmental: "
// plus a 200-char specify composes to 215 chars and would be rejected server-side after the
// form looked valid). Kept in sync with backend/.../CreatePatientRequestValidator.cs's
// `RuleFor(x => x.AllergyType)...MaximumLength(200)`.
const MAX_ALLERGY_TYPE_LENGTH = 200;

const allergyRefinement = (
  data: { hasKnownAllergy: boolean; allergyCategory?: string; allergySpecify?: string; allergySeverity?: string },
  ctx: z.RefinementCtx,
) => {
  if (data.hasKnownAllergy && !data.allergyCategory) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['allergyCategory'], message: 'Allergy category is required' });
  }
  if (data.hasKnownAllergy && !data.allergySeverity) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['allergySeverity'], message: 'Allergy severity is required' });
  }
  if (data.hasKnownAllergy) {
    const composedLength = [data.allergyCategory, data.allergySpecify].filter(Boolean).join(': ').length;
    if (composedLength > MAX_ALLERGY_TYPE_LENGTH) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['allergySpecify'],
        message: `Allergy details are too long — please shorten (combined with the category, it must fit within ${MAX_ALLERGY_TYPE_LENGTH} characters).`,
      });
    }
  }
};

// Mirrors the backend's CreatePatientRequestValidator.CalculateAge (whole-year age, adjusted
// for whether the birthday has occurred yet this year) so both layers agree on the same age
// for a given date of birth.
function calculateAge(dateOfBirth: string, asOf: Date): number {
  const dob = new Date(dateOfBirth);
  let age = asOf.getFullYear() - dob.getFullYear();
  const beforeBirthdayThisYear =
    asOf.getMonth() < dob.getMonth() || (asOf.getMonth() === dob.getMonth() && asOf.getDate() < dob.getDate());
  if (beforeBirthdayThisYear) {
    age--;
  }
  return age;
}

// Kept in sync with backend's CreatePatientRequestValidator.IsTitleConsistentWithAge —
// Title is an age category, not a free-text honorific, so it must stay consistent with the
// patient's actual age. Deliberately not coupled to Gender (a separate, more sensitive call).
const titleAgeRefinement = (data: { title: string; dateOfBirth: string }, ctx: z.RefinementCtx) => {
  if (!data.dateOfBirth) {
    return;
  }
  const age = calculateAge(data.dateOfBirth, new Date());
  const isConsistent = (() => {
    switch (data.title) {
      case 'Baby':
        return age < 2;
      case 'Master':
      case 'Miss':
        return age < 18;
      case 'Mr':
      case 'Mrs':
      case 'Ms':
      case 'Dr':
        return age >= 18;
      default:
        return true;
    }
  })();
  if (!isConsistent) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      path: ['title'],
      message: "Title does not match the patient's age (Baby: under 2, Master/Miss: under 18, Mr/Mrs/Ms/Dr: 18 or older).",
    });
  }
};

// Kept in sync with backend's CreatePatientRequestValidator.IsTitleConsistentWithGender —
// Mr/Master are conventionally masculine, Mrs/Ms/Miss conventionally feminine; Dr and Baby
// are gender-neutral and never flagged. Transgender/NA (this UI's richer gender list, which
// bridges to the backend's single "Other" value — see bridging.ts's toBackendGender) are
// treated the same way Gender.Other is on the backend: never flagged against any title,
// since there's no universal convention for how they should pair with a gendered title.
const titleGenderRefinement = (data: { title: string; gender: string }, ctx: z.RefinementCtx) => {
  if (data.gender === 'Transgender' || data.gender === 'NA') {
    return;
  }
  const isConsistent = (() => {
    switch (data.title) {
      case 'Mr':
      case 'Master':
        return data.gender === 'Male';
      case 'Mrs':
      case 'Ms':
      case 'Miss':
        return data.gender === 'Female';
      default:
        return true;
    }
  })();
  if (!isConsistent) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      path: ['title'],
      message: "Title does not match the patient's gender (Mr/Master: Male, Mrs/Ms/Miss: Female).",
    });
  }
};

export const patientRegistrationUiSchema = z
  .object({
    ...demographicsUiSchema,
    arrivalSource: arrivalSourceSchema,
    registration: registrationDetailsUiSchema,
  })
  .superRefine(allergyRefinement)
  .superRefine(titleAgeRefinement)
  .superRefine(titleGenderRefinement);

export const patientEditUiSchema = z
  .object(demographicsUiSchema)
  .superRefine(allergyRefinement)
  .superRefine(titleAgeRefinement)
  .superRefine(titleGenderRefinement);

export type PatientRegistrationUiFormValues = z.infer<typeof patientRegistrationUiSchema>;
export type PatientEditUiFormValues = z.infer<typeof patientEditUiSchema>;
