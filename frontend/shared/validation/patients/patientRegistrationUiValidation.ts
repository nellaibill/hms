import { z } from 'zod';
import {
  ALLERGY_CATEGORIES,
  ALLERGY_SEVERITIES,
  ARRIVAL_SOURCE_CATEGORIES,
  BLOOD_GROUPS,
  ENCOUNTER_TYPES_UI,
  ID_PROOF_TYPES,
  type IdProofType,
  MARITAL_STATUSES,
  OFFLINE_AD_CHANNELS,
  ONLINE_AD_CHANNELS,
  PATIENT_GENDERS,
  PATIENT_RELATIVE_REFERRAL_SOURCES,
  REFERRAL_COLUMN_CATEGORIES,
  RELATIONSHIPS,
  TITLES,
} from '../../enums';

/**
 * UI-first schema matching the source documents exactly (LH Software.docx + the Patient
 * Mode of Arrival Form). Patient Information/Contact Information/Medical Information now map
 * onto the real CreatePatientRequest (see PatientRegistrationCreatePage.tsx's toRequest());
 * Registration Details/Billing are still UI-only — the Patients backend has no
 * encounter/visit concept yet, see that same file's doc comment.
 */
// Exactly 10 digits, no country code, no formatting characters — kept in sync with backend's
// CreatePatientRequestValidator.PhonePattern (^[0-9]{10}$), which now actually rejects
// anything else, so the UI must match exactly rather than just being "at least as strict".
const phonePattern = /^[0-9]{10}$/;
const phonePatternMessage = 'Phone number must be exactly 10 digits.';
const pincodePattern = /^[0-9]{6}$/;

// \p{L}/\p{M} (Unicode letter/mark categories) rather than A-Za-z so names in Indian
// scripts (Devanagari, Tamil, etc.) — not just ASCII — are accepted; still rejects digits
// and most symbols. Allows spaces/apostrophes/periods/hyphens for names like "Mary-Jane
// O'Brien" or "Dr. Rao". Kept in sync with backend's CreatePatientRequestValidator.NamePattern.
const namePattern = /^[\p{L}\p{M}][\p{L}\p{M}\s'.-]*$/u;
const namePatternMessage = 'Enter letters only.';

// Kept in sync with the backend's CreatePatientRequestValidator — Aadhaar/Passport/VoterId
// each have one fixed, nationally-standardized format, checkable outright. DrivingLicense
// genuinely varies too much (state-specific RTO codes, several formats still in circulation)
// for a real format check, so it only gets a sanity check (alphanumeric, reasonable length) —
// same reasoning as the backend. Other is a free-text catch-all by definition — no format.
// Returns null when the format is fine (or the type has no format to check) — callers add
// their own "required" check first, since that's contextual (Create gates it separately from
// Edit's always-required field).
export function idProofNumberFormatError(idProofType: IdProofType, trimmedNumber: string): string | null {
  switch (idProofType) {
    case 'Aadhaar':
      return /^[0-9]{12}$/.test(trimmedNumber) ? null : 'Aadhaar number must be exactly 12 digits.';
    case 'Passport':
      return /^[A-Za-z][0-9]{7}$/.test(trimmedNumber)
        ? null
        : 'Passport number must be 1 letter followed by 7 digits (e.g. A1234567).';
    case 'VoterId':
      return /^[A-Za-z]{3}[0-9]{7}$/.test(trimmedNumber)
        ? null
        : 'Voter ID number must be 3 letters followed by 7 digits (e.g. ABC1234567).';
    case 'DrivingLicense':
      return /^[A-Za-z0-9\s-]{10,20}$/.test(trimmedNumber) ? null : 'Driving License number must be 10–20 letters/digits.';
    default:
      return null;
  }
}

const primaryPhoneSchema = z.object({
  number: z.string().trim().min(1, 'Primary phone is required').max(20).regex(phonePattern, phonePatternMessage),
});

// A second/third emergency contact added via "Add Emergency Contact" — the first one stays
// its own always-present, always-required set of fields (emergencyContactRelationship/Name/
// Phone below); this is only for entries beyond that first one, same shape as
// additionalConsultants' "the primary one is a fixed field, extras are an array" split.
const emergencyContactEntrySchema = z.object({
  relationship: z.enum(RELATIONSHIPS),
  name: z.string().trim().min(1, 'Name is required').max(150).regex(namePattern, namePatternMessage),
  phone: z.string().trim().min(1, 'Phone is required').max(20).regex(phonePattern, phonePatternMessage),
});

// UI-only demo affordance ("Add another Allergy") — mirrors additionalConsultantSchema's own
// optional/unvalidated shape (frontend/shared/validation/patients/patientRegistrationUiValidation.ts's
// registrationDetailsUiSchema). Deliberately not bridged into CreatePatientRequest: the backend
// only has one AllergyType/AllergySeverity pair per registration today (composed from
// allergyCategory/allergySpecify/allergySeverity above), so entries here are for the on-screen
// experience only and are dropped, not silently mis-saved, at submit time — same reasoning as
// additionalConsultants.
const additionalAllergySchema = z
  .object({
    allergyCategory: z.enum(ALLERGY_CATEGORIES).optional().or(z.literal('')),
    allergySpecify: z.string().trim().max(200).optional().or(z.literal('')),
    allergySeverity: z.enum(ALLERGY_SEVERITIES).optional().or(z.literal('')),
  })
  // A row with *some* field filled in but not enough to actually save (Type/Severity are what
  // toAllergyRequest requires) was previously accepted by this schema and then silently
  // dropped at submit time (PatientRegistrationCreatePage's toRequest filters out incomplete
  // rows) — the receptionist's entry vanished with no error shown. An untouched row (the
  // common case before "Add another Allergy" is even clicked) stays valid.
  .superRefine((row, ctx) => {
    const touched = Boolean(row.allergyCategory || row.allergySpecify || row.allergySeverity);
    if (!touched) {
      return;
    }
    if (!row.allergyCategory) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['allergyCategory'], message: 'Allergy type is required for this row.' });
    }
    if (!row.allergySeverity) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['allergySeverity'], message: 'Allergy severity is required for this row.' });
    }
  });

const arrivalSourceSchema = z
  .object({
    category: z.enum(ARRIVAL_SOURCE_CATEGORIES),
    doctorReferral: z
      .object({
        department: z.string().trim().max(100).optional().or(z.literal('')),
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
    if (data.category === 'DoctorReferral') {
      if (!data.doctorReferral?.department) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['doctorReferral', 'department'], message: 'Enter the referring department.' });
      }
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

// "Add another Consultant" row — each field stays optional/unvalidated here (a half-filled
// row you never finished isn't a validation error), but this IS bridged into
// CreatePatientVisitRequest now (see bridging.ts's toCreatePatientVisitRequest, called from
// PatientRegistrationCreatePage's handleSubmit): any row with both departmentId and
// consultantId set becomes one consultation line on the visit; a row missing either is
// silently dropped rather than sent as a malformed line.
const additionalConsultantSchema = z.object({
  departmentId: z.string().trim().optional().or(z.literal('')),
  consultantId: z.string().trim().optional().or(z.literal('')),
  consultationTypeId: z.string().trim().optional().or(z.literal('')),
});

// Exported (not just used internally) so a future "record a new visit" form can reuse this
// exact schema instead of duplicating it — a follow-up visit's registration details are the
// same shape as a first-time registration's, so they should share one validator.
export const registrationDetailsUiSchema = z
  .object({
    encounterType: z.enum(ENCOUNTER_TYPES_UI),
    departmentId: z.string().trim().min(1, 'Department is required'),
    consultantId: z.string().trim().min(1, 'Consultant is required'),
    additionalConsultants: z.array(additionalConsultantSchema).max(3, 'Up to three additional consultants').default([]),
    appointmentTypeId: z.string().trim().optional().or(z.literal('')),
    consultationTypeId: z.string().trim().optional().or(z.literal('')),
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

export type RegistrationDetailsUiFormValues = z.infer<typeof registrationDetailsUiSchema>;

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
  maritalStatus: z.enum(MARITAL_STATUSES, { message: 'Marital status is required.' }),

  addressLine1: z.string().trim().min(1, 'Address is required').max(200),
  addressLine2: z.string().max(200).optional().or(z.literal('')),
  addressLine3: z.string().max(200).optional().or(z.literal('')),
  district: z.string().trim().min(1, 'District is required').max(100),
  state: z.string().trim().min(1, 'State is required').max(100),
  pincode: z.string().regex(pincodePattern, 'Pincode must be 6 digits'),

  primaryPhone: primaryPhoneSchema,
  secondaryPhone: z.string().trim().max(20).regex(phonePattern, phonePatternMessage).optional().or(z.literal('')),
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
  additionalEmergencyContacts: z.array(emergencyContactEntrySchema).max(2, 'Up to two additional emergency contacts').default([]),

  hasKnownAllergy: z.boolean(),
  allergyCategory: z.enum(ALLERGY_CATEGORIES).optional().or(z.literal('')),
  allergySpecify: z.string().max(200).optional().or(z.literal('')),
  allergySeverity: z.enum(ALLERGY_SEVERITIES).optional().or(z.literal('')),
  additionalAllergies: z.array(additionalAllergySchema).max(3, 'Up to three additional allergies').default([]),
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
  // Brackets match the Title dropdown's own displayed guidance exactly (see titleLabel.ts:
  // "Baby — up to 1 year", "Master/Miss — 1–18 years") — Master/Miss requires age >= 1 too,
  // not just < 18, so a newborn can't be silently registered as Master/Miss instead of Baby.
  const isConsistent = (() => {
    switch (data.title) {
      case 'Baby':
        return age <= 1;
      case 'Master':
      case 'Miss':
        return age >= 1 && age < 18;
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
      message: "Title does not match the patient's age (Baby: up to 1 year, Master/Miss: 1–18 years, Mr/Mrs/Ms/Dr: 18 or older).",
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

// Kept in sync with backend's CreatePatientRequestValidator.IsMaritalStatusConsistentWithAge —
// a minor's marital status isn't a real-world determination yet: under 18 must be 'NA';
// 18-or-older must give a real answer (Married or Unmarried), not 'NA'. Same 18 threshold
// Title already uses for Mr/Mrs/Ms/Dr.
const maritalStatusAgeRefinement = (data: { maritalStatus: string; dateOfBirth: string }, ctx: z.RefinementCtx) => {
  if (!data.dateOfBirth) {
    return;
  }
  const isMinor = calculateAge(data.dateOfBirth, new Date()) < 18;
  const isConsistent = isMinor ? data.maritalStatus === 'NA' : data.maritalStatus === 'Married' || data.maritalStatus === 'Unmarried';
  if (!isConsistent) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      path: ['maritalStatus'],
      message: isMinor
        ? "Patients under 18 must have marital status 'N/A'."
        : 'Marital status must be Married or Unmarried for patients 18 or older.',
    });
  }
};

// Kept in sync with backend's CreatePatientRequestValidator (the SecondaryPhone.NotEqual and
// EmergencyContacts.Must rules) — a "secondary" number identical to the primary isn't a second
// contact method, and an emergency contact is supposed to be someone else to call when the
// patient can't be reached, so either reusing the patient's own primary phone is almost
// certainly a data-entry mistake, not a deliberate choice.
const phoneDuplicationRefinement = (
  data: {
    primaryPhone: { number: string };
    secondaryPhone?: string;
    emergencyContactPhone: string;
    additionalEmergencyContacts: Array<{ phone: string }>;
  },
  ctx: z.RefinementCtx,
) => {
  const primary = data.primaryPhone.number;
  if (!primary) {
    return;
  }
  if (data.secondaryPhone && data.secondaryPhone === primary) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      path: ['secondaryPhone'],
      message: 'Secondary phone must be different from the primary phone.',
    });
  }
  if (data.emergencyContactPhone && data.emergencyContactPhone === primary) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      path: ['emergencyContactPhone'],
      message: "An emergency contact's phone number must be different from the patient's own primary phone.",
    });
  }
  data.additionalEmergencyContacts.forEach((contact, index) => {
    if (contact.phone && contact.phone === primary) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['additionalEmergencyContacts', index, 'phone'],
        message: "An emergency contact's phone number must be different from the patient's own primary phone.",
      });
    }
  });
};

// Patient Information + Contact Information + Medical Information only — everything the
// backend's CreatePatientRequest actually needs, and nothing from Registration Details/
// Billing (which aren't wired to the backend yet, see PatientRegistrationCreatePage's
// toRequest()). Exported so "Save and proceed to Registration" can re-parse getValues()
// through this instead of the full patientRegistrationUiSchema, which would also demand
// Registration Details' required Department/Consultant be filled in — fields that step
// deliberately hasn't been reached yet at that point in the wizard.
export const patientRegistrationCoreUiSchema = z
  .object({
    ...demographicsUiSchema,
    arrivalSource: arrivalSourceSchema,
  })
  .superRefine(allergyRefinement)
  .superRefine(titleAgeRefinement)
  .superRefine(titleGenderRefinement)
  .superRefine(maritalStatusAgeRefinement)
  .superRefine(phoneDuplicationRefinement);

export const patientRegistrationUiSchema = z
  .object({
    ...demographicsUiSchema,
    arrivalSource: arrivalSourceSchema,
    registration: registrationDetailsUiSchema,
  })
  .superRefine(allergyRefinement)
  .superRefine(titleAgeRefinement)
  .superRefine(titleGenderRefinement)
  .superRefine(maritalStatusAgeRefinement)
  .superRefine(phoneDuplicationRefinement);

// ID proof type/number live only on the Edit schema, not demographicsUiSchema — Create keeps
// them on its separate `documents` staging object (see PatientRegistrationForm.tsx) since a
// file has to be staged alongside them until the patient exists; Edit has no such constraint,
// so they can be normal RHF-validated fields here. Always required (unlike Create's more
// deferred gating) since an existing patient's record already has some value for both.
const idProofNumberRefinement = (data: { idProofType: IdProofType; idProofNumber: string }, ctx: z.RefinementCtx) => {
  const trimmed = data.idProofNumber.trim();
  if (!trimmed) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['idProofNumber'], message: 'ID proof number is required.' });
    return;
  }
  const formatError = idProofNumberFormatError(data.idProofType, trimmed);
  if (formatError) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['idProofNumber'], message: formatError });
  }
};

// Edit's Allergy Details section is a real list backed by the backend's add-one/remove-one
// endpoints (see useAddPatientAllergyMutation/useRemovePatientAllergyMutation), not a single
// "primary + additional" set of form fields the way Create's is — there's no "edit the
// current one in place" or "batch several into one save" on the backend for an existing
// patient. So hasKnownAllergy/allergyCategory/allergySpecify/allergySeverity/
// additionalAllergies are deliberately excluded here (they used to be included but never
// actually read by PatientEditPage's toRequest — a dead, silently-discarded set of fields).
function omit<T extends object, K extends keyof T>(shape: T, keys: readonly K[]): Omit<T, K> {
  const result = { ...shape };
  for (const key of keys) delete result[key];
  return result;
}
const editDemographicsUiSchema = omit(demographicsUiSchema, [
  'hasKnownAllergy',
  'allergyCategory',
  'allergySpecify',
  'allergySeverity',
  'additionalAllergies',
] as const);

export const patientEditUiSchema = z
  .object({
    ...editDemographicsUiSchema,
    idProofType: z.enum(ID_PROOF_TYPES),
    idProofNumber: z.string().max(30),
  })
  .superRefine(titleAgeRefinement)
  .superRefine(titleGenderRefinement)
  .superRefine(maritalStatusAgeRefinement)
  .superRefine(phoneDuplicationRefinement)
  .superRefine(idProofNumberRefinement);

export type PatientRegistrationUiFormValues = z.infer<typeof patientRegistrationUiSchema>;
export type PatientRegistrationCoreUiFormValues = z.infer<typeof patientRegistrationCoreUiSchema>;
export type PatientEditUiFormValues = z.infer<typeof patientEditUiSchema>;
