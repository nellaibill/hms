import type { DosageForm, DrugSchedule, Product, ProductProfileFormValues, StorageCondition } from '@hms/shared';

export const DRUG_SCHEDULE_LABELS: Record<DrugSchedule, string> = {
  None: 'Not scheduled',
  G: 'Schedule G',
  H: 'Schedule H',
  H1: 'Schedule H1',
  X: 'Schedule X',
  Narcotic: 'Narcotic (NDPS)',
};

export const STORAGE_CONDITION_LABELS: Record<StorageCondition, string> = {
  RoomTemperature: 'Room temperature',
  CoolPlace: 'Cool place (8–25 °C)',
  Refrigerated: 'Refrigerated (2–8 °C)',
  Frozen: 'Frozen',
};

/** The drug-profile fields as the create and update requests both carry them — shared so the
 * two pages can't drift apart on how a blank form field becomes "not set". */
export function toDrugProfileRequest(values: ProductProfileFormValues) {
  return {
    dosageForm: (values.dosageForm || null) as DosageForm | null,
    strength: values.strength || undefined,
    unitsPerPack: values.unitsPerPack,
    schedule: values.schedule,
    storageCondition: values.storageCondition,
    rackLocation: values.rackLocation || undefined,
    gstRatePercent: values.gstRatePercent ?? null,
  };
}

/** The reverse: a saved product's drug profile as form values. */
export function toDrugProfileFormValues(product: Product) {
  return {
    dosageForm: product.dosageForm ?? ('' as const),
    strength: product.strength ?? '',
    unitsPerPack: product.unitsPerPack,
    schedule: product.schedule,
    storageCondition: product.storageCondition,
    rackLocation: product.rackLocation ?? '',
    gstRatePercent: product.gstRatePercent ?? undefined,
  };
}
