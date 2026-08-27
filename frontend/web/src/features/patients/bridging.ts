import type {
  AllergyInput,
  AllergySeverity,
  AllergyType,
  CreatePatientVisitRequest,
  EmergencyContactInput,
  Gender,
  ModeOfArrivalSource,
  PatientGenderUi,
  PatientRegistrationUiFormValues,
  RecordVisitUiFormValues,
  VisitConsultation,
  VisitType,
} from '@hms/shared';

/**
 * Shared UI-form <-> backend-DTO structural mappers for the Patients feature. Patient
 * Information/Contact Information/Medical Information now map onto the real
 * CreatePatientRequest/UpdatePatientRequest — see PatientRegistrationCreatePage.tsx's
 * toRequest() and PatientEditPage.tsx's toRequest()/toDefaultValues().
 */

// The backend's Gender enum matches PATIENT_GENDERS exactly (Male/Female/Transgender/NA —
// see enums/patients.ts), so this is a lossless 1:1 identity mapping in both directions.
export function toBackendGender(gender: PatientGenderUi): Gender {
  return gender;
}

export function fromBackendGender(gender: Gender): PatientGenderUi {
  return gender;
}

interface UiAllergyRow {
  allergyCategory?: string;
  allergySpecify?: string;
  allergySeverity?: string;
}

/** One UI allergy row (primary or an additionalAllergies entry) -> AllergyRequest. Only
 * called for rows that actually have a category+severity — the additional rows are optional
 * per-field, so a half-filled "Add another Allergy" row that was never completed is simply
 * skipped by the caller rather than sent as a malformed request. */
export function toAllergyRequest(row: UiAllergyRow): AllergyInput {
  return {
    allergyType: row.allergyCategory as AllergyType,
    specify: row.allergySpecify || undefined,
    severity: row.allergySeverity as AllergySeverity,
  };
}

interface UiEmergencyContactRow {
  relationship: string;
  name: string;
  phone: string;
}

/** One UI emergency-contact row (primary or an additionalEmergencyContacts entry) -> EmergencyContactRequest — a direct structural mapping now that both sides use the same Relationship enum. */
export function toEmergencyContactRequest(row: UiEmergencyContactRow): EmergencyContactInput {
  return {
    relationship: row.relationship as EmergencyContactInput['relationship'],
    name: row.name,
    phone: row.phone,
  };
}

export interface ModeOfArrivalRequestFields {
  modeOfArrivalSource: ModeOfArrivalSource;
  modeOfArrivalChannel?: string;
  modeOfArrivalSpecify?: string;
}

/**
 * Maps the UI's richer arrivalSource shape onto the backend's flatter
 * {ModeOfArrivalSource, ModeOfArrivalChannel, ModeOfArrivalSpecify}. ModeOfArrivalChannel/
 * ModeOfArrivalSpecify are plain free-text columns on the backend with no source-specific
 * restriction (CreatePatientRequestValidator only makes Channel *required* for the two Ad
 * sources, and Specify *required* when Channel is literally "Other" — neither rule forbids
 * either field for the other sources), so every category's detail maps through:
 * - Online/Offline Advertisement: channel + specify (only when channel is "Other").
 * - Patient/Relative Referral: the referral source (SameDepartment/Other) as channel, its
 *   free-text details as specify (only when source is "Other") — same "specify only for
 *   Other" shape as the Ad sources.
 * - Doctor Referral: the department name as channel — there's no separate "details" field on
 *   this branch of the UI, so specify stays empty.
 */
export function toModeOfArrivalRequest(arrivalSource: PatientRegistrationUiFormValues['arrivalSource']): ModeOfArrivalRequestFields {
  const category = arrivalSource.category as ModeOfArrivalSource;
  if (arrivalSource.category === 'OnlineAdvertisement' && arrivalSource.onlineAd) {
    return {
      modeOfArrivalSource: category,
      modeOfArrivalChannel: arrivalSource.onlineAd.channel,
      modeOfArrivalSpecify: arrivalSource.onlineAd.channel === 'Other' ? arrivalSource.onlineAd.details || undefined : undefined,
    };
  }
  if (arrivalSource.category === 'OfflineAdvertisement' && arrivalSource.offlineAd) {
    return {
      modeOfArrivalSource: category,
      modeOfArrivalChannel: arrivalSource.offlineAd.channel,
      modeOfArrivalSpecify: arrivalSource.offlineAd.channel === 'Other' ? arrivalSource.offlineAd.details || undefined : undefined,
    };
  }
  if (arrivalSource.category === 'PatientOrRelativeReferral' && arrivalSource.patientRelativeReferral) {
    return {
      modeOfArrivalSource: category,
      modeOfArrivalChannel: arrivalSource.patientRelativeReferral.source,
      modeOfArrivalSpecify:
        arrivalSource.patientRelativeReferral.source === 'Other' ? arrivalSource.patientRelativeReferral.details || undefined : undefined,
    };
  }
  if (arrivalSource.category === 'DoctorReferral' && arrivalSource.doctorReferral?.department) {
    return {
      modeOfArrivalSource: category,
      modeOfArrivalChannel: arrivalSource.doctorReferral.department,
    };
  }
  return { modeOfArrivalSource: category };
}

/** Registration Details tab (or the standalone "Add Visit" page's RecordVisitUiFormValues,
 * which shares this exact same core shape) -> CreatePatientVisitRequest. The primary
 * Department/Consultant/Consultation Type fields plus every additionalConsultants row that
 * has both a department and a consultant become one consultation line each, all sharing the
 * visit created from this one request (see PatientVisitService.CreateAsync) — a row opened
 * via "Add another Consultant" but never filled in is silently dropped rather than sent as a
 * malformed line. */
export function toCreatePatientVisitRequest(registration: RecordVisitUiFormValues): CreatePatientVisitRequest {
  const consultations: VisitConsultation[] = [
    {
      departmentId: registration.departmentId,
      consultantId: registration.consultantId,
      consultationTypeId: registration.consultationTypeId || undefined,
    },
    ...registration.additionalConsultants
      .filter((row) => row.departmentId && row.consultantId)
      .map((row) => ({
        departmentId: row.departmentId!,
        consultantId: row.consultantId!,
        consultationTypeId: row.consultationTypeId || undefined,
      })),
  ];

  return {
    visitType: registration.encounterType as VisitType,
    appointmentTypeId: registration.appointmentTypeId || undefined,
    consultations,
  };
}
