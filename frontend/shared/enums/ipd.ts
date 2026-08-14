/** Mirrors HMS.Modules.IPD.Contracts.IPDEnums — serialized as strings (JsonStringEnumConverter). */
export const WARD_TYPES = ['General', 'SemiPrivate', 'Private', 'ICU'] as const;
export type WardType = (typeof WARD_TYPES)[number];

export const BED_STATUSES = ['Available', 'Occupied', 'Maintenance'] as const;
export type BedStatus = (typeof BED_STATUSES)[number];

export const BED_TYPES = ['Standard', 'Electric', 'ICU', 'SemiICU', 'Deluxe'] as const;
export type BedType = (typeof BED_TYPES)[number];

// Named IpdAdmissionType (not AdmissionType) — HMS.Modules.Patients.Contracts.AdmissionType
// (MLC/NMLC) already owns that name in this barrel; the two are unrelated concepts that
// happen to share a base name across modules.
export const IPD_ADMISSION_TYPES = ['Emergency', 'Elective', 'Transfer'] as const;
export type IpdAdmissionType = (typeof IPD_ADMISSION_TYPES)[number];

export const ADMISSION_STATUSES = ['Admitted', 'Discharged'] as const;
export type AdmissionStatus = (typeof ADMISSION_STATUSES)[number];

export const DISCHARGE_TYPES = ['Normal', 'AgainstMedicalAdvice', 'Referred'] as const;
export type DischargeType = (typeof DISCHARGE_TYPES)[number];

export const CHARGE_TYPES = ['AdmissionCharge', 'BedCharge', 'NursingCharge'] as const;
export type ChargeType = (typeof CHARGE_TYPES)[number];
