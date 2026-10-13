/** Mirrors HMS.Modules.Products.Contracts.ProductEnums — serialized as strings (JsonStringEnumConverter). */
export const DOSAGE_FORMS = [
  'Tablet',
  'Capsule',
  'Syrup',
  'Suspension',
  'Injection',
  'Infusion',
  'Drops',
  'Ointment',
  'Cream',
  'Gel',
  'Inhaler',
  'Powder',
  'Suppository',
  'Patch',
  'Other',
] as const;
export type DosageForm = (typeof DOSAGE_FORMS)[number];

/** The schedule a drug falls under in India's Drugs and Cosmetics Rules; `Narcotic` is the NDPS Act. */
export const DRUG_SCHEDULES = ['None', 'G', 'H', 'H1', 'X', 'Narcotic'] as const;
export type DrugSchedule = (typeof DRUG_SCHEDULES)[number];

export const STORAGE_CONDITIONS = ['RoomTemperature', 'CoolPlace', 'Refrigerated', 'Frozen'] as const;
export type StorageCondition = (typeof STORAGE_CONDITIONS)[number];
